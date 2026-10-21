using System;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Execution
{
    public interface IScenarioExecution
    {
        string RunId { get; }
        bool IsFinished { get; }
        string CurrentGroupId { get; }
        ScenarioJournal PreviousJournal { get; }
        event Action StateChanged;
        StepProgress GetProgress(string stepId);
        ScenarioJournal CaptureJournal();
        ConditionResult Submit(string stepId, StepSignal signal);
        void Restart();
    }
}
