using System;

namespace ScenarioConstructor.Conditions
{
    public sealed class StateGatedActionRule : IStepRule
    {
        private readonly string actionId;
        private readonly string flagId;
        private readonly bool expectedValue;

        public StateGatedActionRule(string actionId, string flagId, bool expectedValue = true)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action identifier is required.", nameof(actionId));
            if (string.IsNullOrWhiteSpace(flagId))
                throw new ArgumentException("A state flag identifier is required.", nameof(flagId));
            this.actionId = actionId;
            this.flagId = flagId;
            this.expectedValue = expectedValue;
        }

        public bool Accepts(string candidate) => candidate == actionId;

        public IStepCondition CreateCondition() => new Condition(actionId, flagId, expectedValue);

        private sealed class Condition : IStepCondition
        {
            private readonly string actionId;
            private readonly string flagId;
            private readonly bool expectedValue;

            public Condition(string actionId, string flagId, bool expectedValue)
            {
                this.actionId = actionId;
                this.flagId = flagId;
                this.expectedValue = expectedValue;
            }

            public ConditionResult Evaluate(StepSignal signal)
            {
                if (signal.ActionId != actionId)
                    return ConditionResult.Ignored();
                if (!signal.Flags.TryGetValue(flagId, out var value))
                    return ConditionResult.Rejected($"Required state '{flagId}' was not supplied.");
                return value == expectedValue
                    ? ConditionResult.Completed()
                    : ConditionResult.Rejected($"State '{flagId}' must be {expectedValue}.");
            }
        }
    }
}
