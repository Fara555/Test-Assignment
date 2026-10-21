using System;
using System.Collections.Generic;
using System.Linq;
using ScenarioConstructor.Unity.Authoring;
using ScenarioConstructor.Unity.Demo;
using ScenarioConstructor.Unity.Scene;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ScenarioConstructor.Editor
{
    public static class DemoSceneBuilder
    {
        public const string ScenePath = "Assets/ScenarioConstructor/Demo/ScenarioDemo.unity";
        private const string AssetFolder = "Assets/ScenarioConstructor/Demo";
        private const string Examples = "Assets/ScenarioConstructor/Examples/Inspection";

        [MenuItem("Scenario Constructor/Open Demo Scene", priority = 0)]
        public static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                BuildDemo();
            else
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void BuildDemo()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("The demonstration scene already exists; it will not be overwritten.");
            if (AssetDatabase.IsValidFolder(AssetFolder))
                throw new InvalidOperationException("The Demo folder already exists; existing assets will not be overwritten.");
            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder("Assets/ScenarioConstructor", "Demo");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ScenarioDemo";
            var training = AssetDatabase.LoadAssetAtPath<ScenarioProfile>($"{Examples}/Training.asset");
            var exam = AssetDatabase.LoadAssetAtPath<ScenarioProfile>($"{Examples}/Exam.asset");
            if (training == null || exam == null)
                throw new InvalidOperationException("The supplied Inspection example profiles are required.");

            var session = new GameObject("Scenario Session").AddComponent<ScenarioSession>();
            Configure(session, settings =>
            {
                settings.FindProperty("profile").objectReferenceValue = training;
                settings.FindProperty("startAutomatically").boolValue = false;
            });
            var dashboard = new GameObject("Demo Dashboard").AddComponent<DemoDashboard>();
            Assign(dashboard, ("training", training), ("exam", exam));
            var power = new GameObject("Power State").AddComponent<ScenarioBooleanState>();
            var rig = new GameObject("Physics Rig").AddComponent<DemoPhysicsRig>();
            var relays = new ScenarioActionRelay[12];
            for (var i = 0; i < relays.Length; i++)
            {
                if (i != 1)
                    relays[i] = Relay(session, training.Steps[i], "perform");
            }
            Configure(relays[3], settings =>
            {
                var flags = settings.FindProperty("stateFlags");
                flags.arraySize = 1;
                flags.GetArrayElementAtIndex(0).FindPropertyRelative("flagId").stringValue = "powerOn";
                flags.GetArrayElementAtIndex(0).FindPropertyRelative("source").objectReferenceValue = power;
            });
            var enterA = Relay(session, training.Steps[1], "enter-a");
            var enterB = Relay(session, training.Steps[1], "enter-b");
            var texture = BuildBoard(rig, enterA, enterB, relays[2]);
            var emitter = new GameObject("Custom Event Source").AddComponent<DemoEventEmitter>();
            UnityEventTools.AddPersistentListener(emitter.OnRaised, relays[4].Raise);

            var eventSystem = new GameObject("Event System", typeof(EventSystem));
            eventSystem.SetActive(false);
            ConfigureInput(eventSystem.AddComponent<InputSystemUIInputModule>());
            eventSystem.SetActive(true);
            var displayCamera = new GameObject("Display Camera").AddComponent<Camera>();
            displayCamera.tag = "MainCamera";
            displayCamera.cullingMask = 0;
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = new Color(0.035f, 0.06f, 0.10f);
            var canvasObject = new GameObject("Demo Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var backdrop = canvasObject.AddComponent<Image>();
            backdrop.color = new Color(0.035f, 0.06f, 0.10f);
            backdrop.raycastTarget = false;
            var layout = Rect(canvasObject.transform, "Layout", 0, 0, 1600, 900);
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(0.5f, 0.5f);
            layout.anchoredPosition = Vector2.zero;

            Label(layout, "SCENARIO LAB", 24, 24, 900, 42, 32, true);
            Label(layout, "Reusable steps. Independent execution, logging and analysis.", 26, 72, 1200, 25, 17);
            Panel(layout, "Tasks Panel", 24, 110, 340, 770);
            Panel(layout, "Controls Panel", 380, 110, 640, 770);
            Panel(layout, "Results Panel", 1036, 110, 540, 770);

            Label(layout, "TASKS", 42, 128, 300, 26, 19, true);
            Label(layout, "Blue: active. Amber: skipped.\nPending actions may count as errors.", 42, 169, 304, 48, 15);
            var rowContent = Scroll(layout, "Task List", 40, 226, 308, 628, true);
            var rowTemplate = CreateRow(rowContent);
            rowTemplate.gameObject.SetActive(false);

            var trainingButton = Button(layout, "Start Training", "Training", 398, 130, 190, 40);
            Bind(trainingButton, dashboard.StartTraining);
            eventSystem.GetComponent<EventSystem>().firstSelectedGameObject = trainingButton.gameObject;
            Bind(Button(layout, "Start Exam", "Exam", 600, 130, 190, 40), dashboard.StartExam);
            Bind(Button(layout, "Restart Run", "Restart", 802, 130, 190, 40), dashboard.RestartRun);
            var runLabel = Label(layout, "TRAINING", 398, 182, 594, 28, 18, true);
            var actionViews = new List<DemoActionButton>();
            var actionNames = new[]
            {
                "Confirm start", "Enter A", "Enter B", "Touch wall", "Toggle power", "Confirm power",
                "Custom event", "Check instrument", "Confirm readings", "Close inspection", "Channel A", "Channel B",
                "Sign report", "Finish procedure"
            };
            var targets = new[] { 0, 1, 1, 2, -1, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            for (var i = 0; i < actionNames.Length; i++)
            {
                var button = Button(layout, actionNames[i], actionNames[i], 398 + i % 3 * 202, 224 + i / 3 * 52, 190, 44);
                if (i == 1) Bind(button, rig.EnterA);
                else if (i == 2) Bind(button, rig.EnterB);
                else if (i == 3) Bind(button, rig.TouchWall);
                else if (i == 4) Bind(button, dashboard.TogglePower);
                else if (i == 6) Bind(button, emitter.Raise);
                else
                {
                    var source = button.gameObject.AddComponent<ButtonActionSource>();
                    Assign(source, ("action", relays[targets[i]]));
                }
                if (targets[i] < 0)
                    continue;
                var view = button.gameObject.AddComponent<DemoActionButton>();
                Configure(view, settings =>
                {
                    settings.FindProperty("step").objectReferenceValue = training.Steps[targets[i]];
                    settings.FindProperty("usesPhysics").boolValue = i == 1 || i == 2 || i == 3;
                });
                actionViews.Add(view);
            }
            var powerLabel = Label(layout, "Power: OFF", 810, 445, 180, 30, 19, true);
            var message = Label(layout, "Choose an active task.", 398, 486, 594, 58, 16);
            Label(layout, "PHYSICS BOARD", 398, 554, 594, 25, 17, true);
            var board = Rect(layout, "Physics View", 398, 582, 594, 194).gameObject.AddComponent<RawImage>();
            board.texture = texture;
            board.raycastTarget = false;
            Label(layout, "Enter A / B moves the blue actor into a real Trigger.\nTouch wall creates a Collision. Wrong order is a violation.", 398, 794, 594, 62, 16);

            Label(layout, "RESULT & JOURNAL", 1056, 128, 500, 26, 19, true);
            var result = Label(layout, "No completed run", 1056, 166, 500, 48, 32, true);
            var detail = JournalText(Scroll(layout, "Result Details", 1056, 221, 500, 60, false), 15);
            detail.text = "Finish the scenario to calculate a result.";
            var analyzerLabel = Label(layout, "Analyzer: Training Analysis", 1056, 290, 500, 24, 16);
            Bind(Button(layout, "Switch Analyzer", "Switch analyzer", 1056, 324, 240, 36), dashboard.SelectNextAnalyzer);
            var analyze = Button(layout, "Reevaluate", "Re-evaluate", 1308, 324, 248, 36);
            Bind(analyze, dashboard.AnalyzeCompletedRun);
            Bind(Button(layout, "Journal Source", "Current / last completed journal", 1056, 374, 500, 34), dashboard.ToggleJournalSource);
            var journalLabel = Label(layout, "CURRENT RUN", 1056, 420, 500, 23, 14, true);
            var journal = JournalText(Scroll(layout, "Journal", 1056, 453, 500, 398, false), 15);
            Label(layout, "Scroll or drag to inspect the full journal.", 1056, 856, 500, 18, 13);

            Assign(dashboard, ("session", session), ("training", training), ("exam", exam), ("physicsRig", rig),
                ("power", power), ("rowTemplate", rowTemplate), ("rowParent", rowContent), ("runLabel", runLabel),
                ("message", message), ("powerLabel", powerLabel), ("resultLabel", result), ("resultDetail", detail),
                ("analyzerLabel", analyzerLabel), ("journalLabel", journalLabel), ("journalText", journal), ("analyzeButton", analyze));
            Configure(dashboard, settings =>
            {
                var choices = settings.FindProperty("analyzers");
                choices.arraySize = 2;
                choices.GetArrayElementAtIndex(0).objectReferenceValue = training.Analyzer;
                choices.GetArrayElementAtIndex(1).objectReferenceValue = exam.Analyzer;
                var actions = settings.FindProperty("actions");
                actions.arraySize = actionViews.Count;
                for (var i = 0; i < actionViews.Count; i++)
                    actions.GetArrayElementAtIndex(i).objectReferenceValue = actionViews[i];
            });
            var savedSettings = new SerializedObject(dashboard);
            if (savedSettings.FindProperty("training").objectReferenceValue == null
                || savedSettings.FindProperty("exam").objectReferenceValue == null)
                throw new InvalidOperationException("The demo profiles must remain valid before saving.");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("The demonstration scene could not be saved.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(entry => entry.path != ScenePath)).ToArray();
        }

        private static RenderTexture BuildBoard(DemoPhysicsRig rig, ScenarioActionRelay enterA,
            ScenarioActionRelay enterB, ScenarioActionRelay wallAction)
        {
            var groundMaterial = Material("Board", new Color(0.10f, 0.16f, 0.22f));
            var zoneMaterial = Material("Zones", new Color(0.15f, 0.40f, 0.45f));
            var actorMaterial = Material("Actor", new Color(0.25f, 0.75f, 1f));
            var wallMaterial = Material("Wall", new Color(0.85f, 0.52f, 0.25f));
            Primitive(rig.transform, "Board", PrimitiveType.Cube, new Vector3(0, -0.15f, 0), new Vector3(7.5f, 0.2f, 3.2f), groundMaterial);
            var a = Primitive(rig.transform, "Zone A", PrimitiveType.Cube, new Vector3(-2, 0.04f, 0.5f), new Vector3(1.3f, 0.08f, 1.3f), zoneMaterial);
            var b = Primitive(rig.transform, "Zone B", PrimitiveType.Cube, new Vector3(0, 0.04f, 0.5f), new Vector3(1.3f, 0.08f, 1.3f), zoneMaterial);
            foreach (var zone in new[] { a, b })
            {
                var collider = zone.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = new Vector3(0, 6, 0);
                collider.size = new Vector3(1, 14, 1);
            }
            var wall = Primitive(rig.transform, "Inspection Wall", PrimitiveType.Cube, new Vector3(2.8f, 0.5f, 0.5f), new Vector3(0.8f, 1, 1.3f), wallMaterial);
            var actor = Primitive(rig.transform, "Actor", PrimitiveType.Sphere, new Vector3(0, 0.5f, -1.2f), Vector3.one * 0.7f, actorMaterial);
            var body = actor.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var sourceA = a.AddComponent<TriggerActionSource>();
            var sourceB = b.AddComponent<TriggerActionSource>();
            var sourceWall = wall.AddComponent<CollisionActionSource>();
            foreach (var pair in new[] { (Source: (Component)sourceA, Relay: enterA), (Source: (Component)sourceB, Relay: enterB), (Source: (Component)sourceWall, Relay: wallAction) })
            {
                Configure(pair.Source, settings =>
                {
                    settings.FindProperty("action").objectReferenceValue = pair.Relay;
                    settings.FindProperty("filter").FindPropertyRelative("requiredCollider").objectReferenceValue = actor.GetComponent<Collider>();
                });
            }
            Assign(rig, ("actor", body), ("home", Marker(rig.transform, "Home", new Vector3(0, 0.5f, -1.2f))),
                ("zoneA", Marker(rig.transform, "A Destination", new Vector3(-2, 0.5f, 0.5f))),
                ("zoneB", Marker(rig.transform, "B Destination", new Vector3(0, 0.5f, 0.5f))),
                ("wallContact", Marker(rig.transform, "Wall Destination", new Vector3(2.1f, 0.5f, 0.5f))));
            BoardLabel(rig.transform, "A", new Vector3(-2, 0.12f, 1.0f));
            BoardLabel(rig.transform, "B", new Vector3(0, 0.12f, 1.0f));
            BoardLabel(rig.transform, "WALL", new Vector3(2.8f, 1.05f, 0.5f));
            var texture = new RenderTexture(960, 320, 24) { name = "Physics Board" };
            AssetDatabase.CreateAsset(texture, $"{AssetFolder}/PhysicsBoard.renderTexture");
            var camera = new GameObject("Board Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 8, 0);
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.orthographic = true;
            camera.orthographicSize = 1.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.085f, 0.13f);
            camera.targetTexture = texture;
            return texture;
        }

        private static ScenarioActionRelay Relay(ScenarioSession session, ScenarioStepAsset step, string actionId)
        {
            var relay = new GameObject($"{step.StepId} - {actionId}").AddComponent<ScenarioActionRelay>();
            Configure(relay, settings =>
            {
                settings.FindProperty("session").objectReferenceValue = session;
                settings.FindProperty("step").objectReferenceValue = step;
                settings.FindProperty("actionId").stringValue = actionId;
            });
            return relay;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static void Panel(Transform parent, string name, float x, float y, float width, float height)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = new Color(0.065f, 0.10f, 0.16f);
            image.raycastTarget = false;
        }

        private static Text Label(Transform parent, string value, float x, float y, float width, float height, int size, bool bold = false)
        {
            var text = Rect(parent, value.Split('\n')[0], x, y, width, height).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = new Color(0.88f, 0.93f, 0.98f);
            text.text = value;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button Button(Transform parent, string name, string label, float x, float y, float width, float height)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.14f, 0.23f, 0.34f);
            colors.highlightedColor = new Color(0.23f, 0.39f, 0.52f);
            colors.pressedColor = new Color(0.10f, 0.18f, 0.28f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.085f, 0.12f, 0.18f);
            button.colors = colors;
            var text = Label(rect, label, 4, 0, width - 8, height, 17);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static RectTransform Scroll(Transform parent, string name, float x, float y, float width, float height, bool rows)
        {
            var viewport = Rect(parent, name, x, y, width, height);
            viewport.gameObject.AddComponent<Image>().color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect(viewport, "Content", 0, 0, width, 0);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = Vector2.zero;
            if (rows)
            {
                var group = content.gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = 4;
                group.childControlHeight = true;
                group.childForceExpandHeight = false;
                group.childForceExpandWidth = true;
            }
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            return content;
        }

        private static Text JournalText(RectTransform content, int size)
        {
            var text = content.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = new Color(0.73f, 0.82f, 0.91f);
            text.supportRichText = false;
            text.raycastTarget = true;
            return text;
        }

        private static DemoStepRow CreateRow(Transform parent)
        {
            var rect = Rect(parent, "Step Row Template", 0, 0, 308, 46);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            var title = Label(rect, "Step", 8, 5, 292, 19, 15, true);
            var detail = Label(rect, "Pending", 8, 26, 292, 17, 13);
            var row = rect.gameObject.AddComponent<DemoStepRow>();
            Assign(row, ("title", title), ("detail", detail), ("background", image));
            return row;
        }

        private static Material Material(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, $"{AssetFolder}/{name}.mat");
            return material;
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.position = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static Transform Marker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = position;
            return marker;
        }

        private static void BoardLabel(Transform parent, string value, Vector3 position)
        {
            var text = new GameObject(value).AddComponent<TextMesh>();
            text.transform.SetParent(parent, false);
            text.transform.position = position;
            text.transform.rotation = Quaternion.Euler(90, 0, 0);
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.characterSize = 0.14f;
            text.fontSize = 32;
            text.color = Color.white;
        }

        private static void Bind(Button button, UnityAction action) => UnityEventTools.AddPersistentListener(button.onClick, action);

        private static void ConfigureInput(InputSystemUIInputModule module)
        {
            const string path = "Assets/InputSystem_Actions.inputactions";
            module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (module.actionsAsset == null)
                throw new InvalidOperationException("The project's Input System action asset is required.");
            var references = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>()
                .Where(reference => reference.action.actionMap.name == "UI")
                .GroupBy(reference => reference.action.name)
                .ToDictionary(group => group.Key, group => group.First());
            module.point = references["Point"];
            module.leftClick = references["Click"];
            module.rightClick = references["RightClick"];
            module.middleClick = references["MiddleClick"];
            module.scrollWheel = references["ScrollWheel"];
            module.move = references["Navigate"];
            module.submit = references["Submit"];
            module.cancel = references["Cancel"];
        }

        private static void Assign(UnityEngine.Object target, params (string Field, UnityEngine.Object Value)[] values)
        {
            Configure(target, settings =>
            {
                foreach (var pair in values)
                    settings.FindProperty(pair.Field).objectReferenceValue = pair.Value;
            });
        }

        private static void Configure(UnityEngine.Object target, Action<SerializedObject> configure)
        {
            var settings = new SerializedObject(target);
            configure(settings);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
