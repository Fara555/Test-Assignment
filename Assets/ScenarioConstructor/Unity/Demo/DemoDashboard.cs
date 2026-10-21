using System;
using System.Collections.Generic;
using System.Text;
using ScenarioConstructor.Analysis;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using ScenarioConstructor.Unity.Authoring;
using ScenarioConstructor.Unity.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioConstructor.Unity.Demo
{
    public sealed class DemoDashboard : MonoBehaviour
    {
        [SerializeField] private ScenarioSession session;
        [SerializeField] private ScenarioProfile training;
        [SerializeField] private ScenarioProfile exam;
        [SerializeField] private ScenarioAnalyzerAsset[] analyzers;
        [SerializeField] private DemoPhysicsRig physicsRig;
        [SerializeField] private ScenarioBooleanState power;
        [SerializeField] private DemoStepRow rowTemplate;
        [SerializeField] private Transform rowParent;
        [SerializeField] private DemoActionButton[] actions;
        [SerializeField] private Text runLabel;
        [SerializeField] private Text message;
        [SerializeField] private Text powerLabel;
        [SerializeField] private Text resultLabel;
        [SerializeField] private Text resultDetail;
        [SerializeField] private Text analyzerLabel;
        [SerializeField] private Text journalLabel;
        [SerializeField] private Text journalText;
        [SerializeField] private Button analyzeButton;
        private readonly List<DemoStepRow> rows = new List<DemoStepRow>();
        private ScenarioAnalyzerAsset selectedAnalyzer;
        private string analyzedRunId;
        private bool showCompletedJournal;

        public ScenarioSession Session => session;
        public DemoPhysicsRig PhysicsRig => physicsRig;
        public AnalysisResult LastAnalysis { get; private set; }

        private void OnEnable()
        {
            if (session == null || physicsRig == null)
                return;
            session.StateChanged += Refresh;
            session.Violation += ShowViolation;
            physicsRig.BusyChanged += Refresh;
        }

        private void OnDisable()
        {
            if (session == null || physicsRig == null)
                return;
            session.StateChanged -= Refresh;
            session.Violation -= ShowViolation;
            physicsRig.BusyChanged -= Refresh;
        }

        private void Start() => StartTraining();

        public void StartTraining() => Begin(training);
        public void StartExam() => Begin(exam);

        public void RestartRun()
        {
            physicsRig.ResetRig();
            power.SetFalse();
            message.text = "New attempt. Pending actions follow the profile's out-of-order policy.";
            showCompletedJournal = false;
            session.Restart();
        }

        public void TogglePower()
        {
            power.Toggle();
            Refresh();
        }

        public void SelectNextAnalyzer()
        {
            var index = Array.IndexOf(analyzers, selectedAnalyzer);
            selectedAnalyzer = analyzers[(index + 1) % analyzers.Length];
            analyzerLabel.text = $"Analyzer: {selectedAnalyzer.name}";
        }

        public void AnalyzeCompletedRun()
        {
            if (session.LastCompletedJournal == null)
                return;
            LastAnalysis = session.AnalyzeLastCompletedRun(selectedAnalyzer);
            analyzedRunId = session.LastCompletedJournal.RunId;
            resultLabel.text = $"{LastAnalysis.Score} / {LastAnalysis.MaximumScore}";
            resultDetail.text = $"Last completed run: {analyzedRunId.Substring(0, 8)}\n{selectedAnalyzer.name}"
                + (LastAnalysis.Warnings.Count == 0 ? "" : "\n" + string.Join("\n", LastAnalysis.Warnings));
        }

        public void ToggleJournalSource()
        {
            if (session.LastCompletedJournal != null)
                showCompletedJournal = !showCompletedJournal;
            RefreshJournal();
        }

        private void Begin(ScenarioProfile profile)
        {
            if (!profile.TryValidate(out var diagnostic))
            {
                message.text = diagnostic;
                return;
            }
            physicsRig.ResetRig();
            power.SetFalse();
            selectedAnalyzer = profile.Analyzer;
            analyzerLabel.text = $"Analyzer: {selectedAnalyzer.name}";
            showCompletedJournal = false;
            message.text = "Choose active tasks in any order. A and B belong to the same sequence.";
            foreach (var row in rows)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            rows.Clear();
            foreach (var step in profile.Steps)
            {
                var row = Instantiate(rowTemplate, rowParent);
                row.Bind(step);
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
            session.Begin(profile);
        }

        private void Refresh()
        {
            if (!session.HasRun)
                return;
            var resolved = 0;
            foreach (var row in rows)
            {
                var progress = session.GetProgress(row.Step.StepId);
                row.Refresh(progress);
                if (progress.Status != StepStatus.Active && progress.Status != StepStatus.Pending)
                    resolved++;
            }
            runLabel.text = $"{session.Profile.name.ToUpperInvariant()}   |   {resolved}/{rows.Count} resolved"
                + (session.IsFinished ? "   |   COMPLETE" : "");
            powerLabel.text = power.Value ? "Power: ON" : "Power: OFF";
            foreach (var action in actions)
                action.Refresh(session, physicsRig.IsBusy);
            analyzeButton.interactable = session.LastCompletedJournal != null;
            if (session.IsFinished && session.LastCompletedJournal.RunId != analyzedRunId)
                AnalyzeCompletedRun();
            RefreshJournal();
        }

        private void ShowViolation(string reason) => message.text = $"Last violation: {reason}";

        private void RefreshJournal()
        {
            if (!session.HasRun)
                return;
            var journal = showCompletedJournal && session.LastCompletedJournal != null
                ? session.LastCompletedJournal : session.CaptureJournal();
            journalLabel.text = $"{(showCompletedJournal ? "LAST COMPLETED" : "CURRENT RUN")}  {journal.RunId.Substring(0, 8)}  |  {journal.Events.Count} events";
            var text = new StringBuilder();
            foreach (var entry in journal.Events)
            {
                text.Append(entry.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"));
                text.Append("  ").Append(entry.StepId).Append("  ").AppendLine(entry.EventType);
                if (entry.Outcome != StepOutcome.None)
                    text.Append(entry.Outcome).Append("  ");
                if (entry.AttemptNumber.HasValue)
                    text.Append("Attempt ").Append(entry.AttemptNumber.Value).Append("  ");
                if (entry.Details.TryGetValue("reason", out var reason))
                    text.Append(reason);
                text.AppendLine().AppendLine();
            }
            journalText.text = text.ToString();
        }
    }
}
