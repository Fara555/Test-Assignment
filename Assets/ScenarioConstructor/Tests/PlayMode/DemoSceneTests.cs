#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ScenarioConstructor.Analysis;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using ScenarioConstructor.Unity.Demo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ScenarioConstructor.Tests
{
    public sealed class DemoSceneTests
    {
        private DemoDashboard dashboard;

        [UnitySetUp]
        public IEnumerator LoadDemo()
        {
            yield return SceneManager.LoadSceneAsync("ScenarioDemo");
            yield return null;
            dashboard = Object.FindFirstObjectByType<DemoDashboard>();
            Assert.That(dashboard, Is.Not.Null);
            Assert.That(dashboard.Session.HasRun, Is.True);
            Assert.That(EventSystem.current.currentInputModule, Is.TypeOf<InputSystemUIInputModule>());
            Assert.That(SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).All(script => script != null), Is.True);
        }

        [UnityTest]
        public IEnumerator PointerInputActivatesTheVisibleStartAction()
        {
            var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            var previousRunInBackground = Application.runInBackground;
            var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .Single(candidate => candidate.name == "Confirm start");
                var rect = button.GetComponent<RectTransform>();
                Canvas.ForceUpdateCanvases();
                var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
                Assert.That(hits.Any(hit => hit.gameObject == button.gameObject), Is.True,
                    $"The visible action must be raycastable at {position}; hits: {string.Join(", ", hits.Select(hit => hit.gameObject.name))}");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.Update();
                yield return null;
                Assert.That(dashboard.Session.GetProgress("S01").Status, Is.EqualTo(StepStatus.Completed));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                Application.runInBackground = previousRunInBackground;
            }
        }

        [UnityTest]
        public IEnumerator TrainingControlsProduceReferenceSubtotalAndSupportOfflineReanalysis()
        {
            Press("Confirm start");
            Press("Enter B");
            yield return WaitForRig();
            Press("Enter A");
            yield return WaitForRig();
            Press("Enter B");
            yield return WaitForRig();
            Press("Confirm power");
            Press("Confirm power");
            Press("Toggle power");
            Press("Confirm power");
            Press("Touch wall");
            yield return WaitForRig();
            FinishRemaining();

            Assert.That(dashboard.Session.IsFinished, Is.True);
            Assert.That(dashboard.LastAnalysis.Score, Is.EqualTo(105));
            var journal = dashboard.Session.LastCompletedJournal;
            var ids = new[] { "S01", "S02", "S04" };
            var subset = new ScenarioJournal(journal.ScenarioId, journal.RunId, ids,
                journal.Events.Where(entry => ids.Contains(entry.StepId)));
            Assert.That(new TrainingAnalyzer().Analyze(subset).Score, Is.EqualTo(15));
            Press("Switch Analyzer");
            Press("Reevaluate");
            Assert.That(dashboard.LastAnalysis.Score, Is.EqualTo(100));
            Assert.That(dashboard.Session.LastCompletedJournal, Is.SameAs(journal));
            Press("Restart Run");
            Assert.That(dashboard.Session.IsFinished, Is.False);
            Assert.That(dashboard.Session.RunId, Is.Not.EqualTo(journal.RunId));
            Assert.That(dashboard.Session.LastCompletedJournal, Is.SameAs(journal));
        }

        [UnityTest]
        public IEnumerator ExamControlsProduceTwoFailuresTwoSkipsAndSeventyPoints()
        {
            Press("Start Exam");
            Press("Enter B");
            yield return WaitForRig();
            Press("Confirm power");
            Assert.That(dashboard.Session.GetProgress("S01").Status, Is.EqualTo(StepStatus.Skipped));
            Assert.That(dashboard.Session.GetProgress("S02").Status, Is.EqualTo(StepStatus.Failed));
            Assert.That(dashboard.Session.GetProgress("S03").Status, Is.EqualTo(StepStatus.Skipped));
            Assert.That(dashboard.Session.GetProgress("S04").Status, Is.EqualTo(StepStatus.Failed));
            FinishRemaining();
            Assert.That(dashboard.Session.IsFinished, Is.True);
            Assert.That(dashboard.LastAnalysis.Score, Is.EqualTo(70));
            Assert.That(dashboard.LastAnalysis.Warnings, Is.Empty);
            Press("Journal Source");
            Press("Reevaluate");
            Assert.That(dashboard.LastAnalysis.Score, Is.EqualTo(70));
        }

        [UnityTest]
        public IEnumerator RestartCancelsPendingPhysicsMovement()
        {
            Press("Enter A");
            Assert.That(dashboard.PhysicsRig.IsBusy, Is.True);
            Press("Restart Run");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(dashboard.PhysicsRig.IsBusy, Is.False);
            Assert.That(dashboard.Session.GetProgress("S02").AttemptNumber, Is.EqualTo(1));
            Assert.That(dashboard.Session.CaptureJournal().Events.All(entry => entry.EventType == ScenarioEvent.StepActivated), Is.True);
        }

        private void FinishRemaining()
        {
            foreach (var name in new[] { "Custom event", "Check instrument", "Confirm readings", "Close inspection",
                "Channel B", "Channel A", "Finish procedure", "Sign report" })
                Press(name);
        }

        private IEnumerator WaitForRig()
        {
            for (var i = 0; i < 30 && dashboard.PhysicsRig.IsBusy; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(dashboard.PhysicsRig.IsBusy, Is.False);
        }

        private static void Press(string name)
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Single(candidate => candidate.name == name);
            Assert.That(button.interactable, Is.True, $"Button '{name}' should be available.");
            button.onClick.Invoke();
        }
    }
}
#endif
