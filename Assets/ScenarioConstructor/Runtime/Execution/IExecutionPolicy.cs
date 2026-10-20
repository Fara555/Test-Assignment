using System;

namespace ScenarioConstructor.Execution
{
    public interface IExecutionPolicy
    {
        bool RetryOnViolation { get; }
        int? GroupViolationLimit { get; }
        void Validate(ExecutionPlan plan);
    }

    public sealed class TrainingExecutionPolicy : IExecutionPolicy
    {
        public bool RetryOnViolation => true;
        public int? GroupViolationLimit => null;

        public void Validate(ExecutionPlan plan)
        {
            if (plan.Groups.Count != 1)
                throw new ArgumentException("Training requires one shared group.", nameof(plan));
        }
    }

    public sealed class ExamExecutionPolicy : IExecutionPolicy
    {
        public bool RetryOnViolation => false;
        public int? GroupViolationLimit { get; }

        public ExamExecutionPolicy(int groupViolationLimit = 2)
        {
            if (groupViolationLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(groupViolationLimit));
            GroupViolationLimit = groupViolationLimit;
        }

        public void Validate(ExecutionPlan plan) { }
    }
}
