using System;
using System.Collections.Generic;
using ScenarioConstructor.Analysis;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using ScenarioConstructor.Unity.Authoring;
using UnityEngine;
using UnityEngine.Events;

namespace ScenarioConstructor.Unity.Scene
{
    [DisallowMultipleComponent]
    public sealed class ScenarioSession : MonoBehaviour
    {
        [SerializeField] private ScenarioProfile profile;
        [SerializeField] private bool startAutomatically = true;
        [SerializeField] private UnityEvent<string> onViolation = new UnityEvent<string>();
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        private ScenarioRunner runner;
        private Dictionary<ScenarioStepAsset, string> stepBindings;
        private bool completionPublished;

        public ScenarioProfile Profile => profile;
        public bool HasRun => runner != null;
        public bool IsFinished => runner != null && runner.IsFinished;
        public string RunId => runner?.RunId;
        public ScenarioJournal LastCompletedJournal { get; private set; }
        public event Action StateChanged;

        private void Start()
        {
            if (startAutomatically && runner == null)
                Begin();
        }

        private void OnEnable()
        {
            if (runner != null)
                runner.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (runner != null)
                runner.StateChanged -= HandleStateChanged;
        }

        public void Begin() => Begin(profile);

        public void Begin(ScenarioProfile selectedProfile)
        {
            if (!isActiveAndEnabled)
                return;
            try
            {
                if (selectedProfile == null)
                    throw new InvalidOperationException("Select a scenario profile before starting.");
                var next = selectedProfile.CreateRunner();
                var nextBindings = new Dictionary<ScenarioStepAsset, string>();
                foreach (var step in selectedProfile.Steps)
                    nextBindings.Add(step, step.StepId);
                if (runner != null)
                    runner.StateChanged -= HandleStateChanged;
                runner = next;
                stepBindings = nextBindings;
                profile = selectedProfile;
                completionPublished = false;
                runner.StateChanged += HandleStateChanged;
                HandleStateChanged();
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                Debug.LogError($"Cannot start scenario: {exception.Message}", this);
            }
        }

        public void Restart()
        {
            if (!isActiveAndEnabled)
                return;
            if (runner == null)
            {
                Begin();
                return;
            }
            completionPublished = false;
            runner.Restart();
        }

        public ConditionResult Submit(ScenarioStepAsset step, StepSignal signal)
        {
            if (!isActiveAndEnabled || runner == null)
                return ConditionResult.Ignored();
            if (step == null || !stepBindings.TryGetValue(step, out var stepId))
                throw new ArgumentException("The action must target a step asset in the active profile.", nameof(step));
            var result = runner.Submit(stepId, signal);
            if (result.Status == ConditionStatus.Rejected)
                onViolation.Invoke(result.Reason);
            return result;
        }

        public StepProgress GetProgress(string stepId)
        {
            if (runner == null)
                throw new InvalidOperationException("Start a scenario before reading progress.");
            return runner.GetProgress(stepId);
        }

        public ScenarioJournal CaptureJournal()
        {
            if (runner == null)
                throw new InvalidOperationException("Start a scenario before capturing its journal.");
            return runner.CaptureJournal();
        }

        public AnalysisResult AnalyzeLastCompletedRun(ScenarioAnalyzerAsset selectedAnalyzer)
        {
            if (LastCompletedJournal == null)
                throw new InvalidOperationException("Complete a run before analyzing its final result.");
            if (selectedAnalyzer == null)
                throw new ArgumentNullException(nameof(selectedAnalyzer));
            var analyzer = selectedAnalyzer.CreateAnalyzer()
                ?? throw new InvalidOperationException("The analyzer asset did not create an analyzer.");
            return analyzer.Analyze(LastCompletedJournal);
        }

        private void HandleStateChanged()
        {
            var completedNow = runner.IsFinished && !completionPublished;
            if (completedNow)
            {
                completionPublished = true;
                LastCompletedJournal = runner.CaptureJournal();
            }
            StateChanged?.Invoke();
            if (completedNow)
                onCompleted.Invoke();
        }
    }
}
