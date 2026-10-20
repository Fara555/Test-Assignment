using System;
using System.Collections.Generic;

namespace ScenarioConstructor.Conditions
{
    public sealed class OrderedActionsRule : IStepRule
    {
        private readonly string[] actions;
        private readonly HashSet<string> acceptedActions;

        public OrderedActionsRule(params string[] actionIds)
        {
            if (actionIds == null)
                throw new ArgumentNullException(nameof(actionIds));
            if (actionIds.Length == 0)
                throw new ArgumentException("At least one action is required.", nameof(actionIds));

            actions = (string[])actionIds.Clone();
            acceptedActions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in actions)
            {
                if (string.IsNullOrWhiteSpace(action))
                    throw new ArgumentException("Action identifiers cannot be empty.", nameof(actionIds));
                acceptedActions.Add(action);
            }
        }

        public bool Accepts(string actionId) => acceptedActions.Contains(actionId);

        public IStepCondition CreateCondition() => new Condition(actions, acceptedActions);

        private sealed class Condition : IStepCondition
        {
            private readonly string[] actions;
            private readonly HashSet<string> acceptedActions;
            private int nextAction;

            public Condition(string[] actions, HashSet<string> acceptedActions)
            {
                this.actions = actions;
                this.acceptedActions = acceptedActions;
            }

            public ConditionResult Evaluate(StepSignal signal)
            {
                if (!acceptedActions.Contains(signal.ActionId) || nextAction == actions.Length)
                    return ConditionResult.Ignored();
                if (signal.ActionId != actions[nextAction])
                    return ConditionResult.Rejected($"Expected action '{actions[nextAction]}' before '{signal.ActionId}'.");

                nextAction++;
                return nextAction == actions.Length ? ConditionResult.Completed() : ConditionResult.Progressed();
            }
        }
    }
}
