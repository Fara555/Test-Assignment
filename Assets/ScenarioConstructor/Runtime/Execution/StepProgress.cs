using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Execution
{
    public enum StepStatus
    {
        Pending,
        Active,
        Completed,
        Failed,
        Skipped
    }

    public readonly struct StepProgress
    {
        public string StepId { get; }
        public StepStatus Status { get; }
        public int AttemptNumber { get; }
        public int Violations { get; }

        public StepProgress(string stepId, StepStatus status, int attemptNumber, int violations)
        {
            StepId = stepId;
            Status = status;
            AttemptNumber = attemptNumber;
            Violations = violations;
        }
    }
}
