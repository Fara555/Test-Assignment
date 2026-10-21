using System;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using ScenarioConstructor.Unity.Authoring;

namespace ScenarioConstructor.Tests
{
    public sealed class ReplacementExecutionAsset : ScenarioExecutionAsset
    {
        public override void Validate(ExecutionPlan plan) { }

        public override IScenarioExecution CreateExecution(ExecutionPlan plan, Func<string, IScenarioLogger> loggerFactory)
        {
            return new ReplacementExecution(new ScenarioRunner(plan, new ExamExecutionPolicy(1), loggerFactory));
        }

        private sealed class ReplacementExecution : IScenarioExecution
        {
            private readonly ScenarioRunner inner;

            public ReplacementExecution(ScenarioRunner inner) => this.inner = inner;
            public string RunId => inner.RunId;
            public bool IsFinished => inner.IsFinished;
            public string CurrentGroupId => inner.CurrentGroupId;
            public ScenarioJournal PreviousJournal => inner.PreviousJournal;
            public event Action StateChanged
            {
                add => inner.StateChanged += value;
                remove => inner.StateChanged -= value;
            }
            public StepProgress GetProgress(string stepId) => inner.GetProgress(stepId);
            public ScenarioJournal CaptureJournal() => inner.CaptureJournal();
            public ConditionResult Submit(string stepId, StepSignal signal) => inner.Submit(stepId, signal);
            public void Restart() => inner.Restart();
        }
    }
}
