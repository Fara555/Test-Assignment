#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Unity.Authoring;
using ScenarioConstructor.Unity.Scene;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ScenarioConstructor.Tests
{
    public sealed class ScenarioIntegrationTests
    {
        private GameObject root;
        private ScenarioSession session;
        private ScenarioStepAsset step;
        private ScenarioActionRelay relay;
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root);
            foreach (var asset in assets)
                Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [UnityTest]
        public IEnumerator ButtonListenerIsRemovedWhileDisabledAndDoesNotAccumulate()
        {
            CreateFixture(true);
            var buttonObject = Child("Button", typeof(RectTransform), typeof(Button), typeof(ButtonActionSource));
            var source = buttonObject.GetComponent<ButtonActionSource>();
            Reference(source, "action", relay);
            StartFixture();
            yield return null;

            source.enabled = false;
            buttonObject.GetComponent<Button>().onClick.Invoke();
            Assert.That(session.GetProgress("step").Violations, Is.Zero);
            for (var i = 0; i < 3; i++)
            {
                source.enabled = true;
                source.enabled = false;
            }
            source.enabled = true;
            buttonObject.GetComponent<Button>().onClick.Invoke();
            Assert.That(session.GetProgress("step").Violations, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UnityEventCanCompleteStepThroughRelay()
        {
            CreateFixture();
            StartFixture();
            yield return null;
            var source = new UnityEvent();
            source.AddListener(relay.Raise);
            source.Invoke();
            Assert.That(session.IsFinished, Is.True);
            Assert.That(session.LastCompletedJournal, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SceneFlagIsReadAtActionTimeAndRetryCanSucceed()
        {
            CreateFixture(true);
            var state = Child("Power").AddComponent<ScenarioBooleanState>();
            var serialized = new SerializedObject(relay);
            var flags = serialized.FindProperty("stateFlags");
            flags.arraySize = 1;
            flags.GetArrayElementAtIndex(0).FindPropertyRelative("flagId").stringValue = "powerOn";
            flags.GetArrayElementAtIndex(0).FindPropertyRelative("source").objectReferenceValue = state;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            StartFixture();
            yield return null;
            relay.Raise();
            Assert.That(session.GetProgress("step").AttemptNumber, Is.EqualTo(2));
            state.SetTrue();
            relay.Raise();
            Assert.That(session.IsFinished, Is.True);
            var analyzer = Asset<TrainingAnalyzerAsset>();
            Assert.That(session.AnalyzeLastCompletedRun(analyzer).Score, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator CompletedRunCanBeReanalyzedAfterRestartAndSessionDisable()
        {
            CreateFixture();
            StartFixture();
            yield return null;
            relay.Raise();
            var completed = session.LastCompletedJournal;
            session.Restart();
            Assert.That(session.RunId, Is.Not.EqualTo(completed.RunId));
            session.enabled = false;
            relay.Raise();
            Assert.That(session.GetProgress("step").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(session.AnalyzeLastCompletedRun(Asset<TrainingAnalyzerAsset>()).Score, Is.EqualTo(10));
            Assert.That(session.AnalyzeLastCompletedRun(Asset<ExamAnalyzerAsset>()).Score, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator ReenabledSessionPublishesOneStateChangePerAction()
        {
            CreateFixture();
            StartFixture();
            yield return null;
            for (var i = 0; i < 3; i++)
            {
                session.enabled = false;
                session.enabled = true;
            }
            var notifications = 0;
            session.StateChanged += () => notifications++;
            relay.Raise();
            Assert.That(notifications, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisabledRelayDoesNotSubmitUnityEvents()
        {
            CreateFixture();
            StartFixture();
            yield return null;
            relay.enabled = false;
            relay.Raise();
            Assert.That(session.IsFinished, Is.False);
        }

        [UnityTest]
        public IEnumerator TriggerAcceptsActualPhysicsEntry()
        {
            CreateFixture();
            var trigger = Child("Trigger", typeof(BoxCollider), typeof(TriggerActionSource));
            trigger.GetComponent<BoxCollider>().isTrigger = true;
            Reference(trigger.GetComponent<TriggerActionSource>(), "action", relay);
            var actor = Actor();
            StartFixture();
            yield return new WaitForFixedUpdate();
            actor.position = Vector3.zero;
            yield return WaitForCompletion();
            Assert.That(session.IsFinished, Is.True);
        }

        [UnityTest]
        public IEnumerator CollisionAcceptsActualPhysicsContact()
        {
            CreateFixture();
            var wall = Child("Wall", typeof(BoxCollider), typeof(CollisionActionSource));
            Reference(wall.GetComponent<CollisionActionSource>(), "action", relay);
            var actor = Actor();
            StartFixture();
            yield return new WaitForFixedUpdate();
            actor.position = new Vector3(-0.8f, 0, 0);
            yield return WaitForCompletion();
            Assert.That(session.IsFinished, Is.True);
        }

        [UnityTest]
        public IEnumerator TriggerIgnoresFilteredAndDisabledSources()
        {
            CreateFixture();
            var trigger = Child("Trigger", typeof(BoxCollider), typeof(TriggerActionSource));
            trigger.GetComponent<BoxCollider>().isTrigger = true;
            var source = trigger.GetComponent<TriggerActionSource>();
            Reference(source, "action", relay);
            var settings = new SerializedObject(source);
            settings.FindProperty("filter").FindPropertyRelative("acceptedLayers").intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var actor = Actor();
            StartFixture();
            yield return new WaitForFixedUpdate();
            actor.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(session.IsFinished, Is.False);
            source.enabled = false;
            settings.Update();
            settings.FindProperty("filter").FindPropertyRelative("acceptedLayers").intValue = -1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            actor.position = new Vector3(-4, 0, 0);
            yield return new WaitForFixedUpdate();
            actor.position = Vector3.zero;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(session.IsFinished, Is.False);
        }

        private void CreateFixture(bool gated = false)
        {
            root = new GameObject("Scenario integration fixture");
            root.SetActive(false);
            session = root.AddComponent<ScenarioSession>();
            step = Asset<ScenarioStepAsset>();
            var stepSettings = new SerializedObject(step);
            stepSettings.FindProperty("stepId").stringValue = "step";
            stepSettings.FindProperty("title").stringValue = "Test step";
            stepSettings.FindProperty("rule").objectReferenceValue = gated
                ? (StepRuleAsset)Asset<StateGatedActionAsset>() : Asset<OrderedActionsAsset>();
            stepSettings.ApplyModifiedPropertiesWithoutUndo();
            var profile = Asset<ScenarioProfile>();
            var settings = new SerializedObject(profile);
            settings.FindProperty("steps").arraySize = 1;
            settings.FindProperty("steps").GetArrayElementAtIndex(0).objectReferenceValue = step;
            foreach (var field in new[] { "groups", "activationBatches" })
            {
                var sets = settings.FindProperty(field);
                sets.arraySize = 1;
                var set = sets.GetArrayElementAtIndex(0);
                set.FindPropertyRelative("id").stringValue = field;
                set.FindPropertyRelative("steps").arraySize = 1;
                set.FindPropertyRelative("steps").GetArrayElementAtIndex(0).objectReferenceValue = step;
            }
            settings.FindProperty("execution").objectReferenceValue = Asset<TrainingPolicyAsset>();
            settings.FindProperty("logger").objectReferenceValue = Asset<MemoryLoggerAsset>();
            settings.FindProperty("analyzer").objectReferenceValue = Asset<TrainingAnalyzerAsset>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            Reference(session, "profile", profile);
            relay = Child("Action").AddComponent<ScenarioActionRelay>();
            Reference(relay, "session", session);
            Reference(relay, "step", step);
        }

        private void StartFixture()
        {
            root.SetActive(true);
            session.Begin();
        }

        private Rigidbody Actor()
        {
            var actor = Child("Actor", typeof(SphereCollider), typeof(Rigidbody));
            actor.transform.position = new Vector3(-4, 0, 0);
            var body = actor.GetComponent<Rigidbody>();
            body.useGravity = false;
            return body;
        }

        private IEnumerator WaitForCompletion()
        {
            for (var i = 0; i < 20 && !session.IsFinished; i++)
                yield return new WaitForFixedUpdate();
        }

        private GameObject Child(string name, params System.Type[] components)
        {
            var child = new GameObject(name, components);
            child.transform.SetParent(root.transform, false);
            return child;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            assets.Add(asset);
            return asset;
        }

        private static void Reference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
