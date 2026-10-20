using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ScenarioConstructor.Conditions
{
    public sealed class StepSignal
    {
        public string ActionId { get; }
        public IReadOnlyDictionary<string, bool> Flags { get; }

        public StepSignal(string actionId, IDictionary<string, bool> flags = null)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action identifier is required.", nameof(actionId));

            ActionId = actionId;
            Flags = new ReadOnlyDictionary<string, bool>(flags == null
                ? new Dictionary<string, bool>(StringComparer.Ordinal)
                : new Dictionary<string, bool>(flags, StringComparer.Ordinal));
        }
    }
}
