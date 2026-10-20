using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ScenarioConstructor.Journal
{
    public sealed class ScenarioEvent
    {
        public const string StepResolved = "step.resolved";
        public const string AttemptRejected = "attempt.rejected";
        public const string StepActivated = "step.activated";
        public const string StepProgressed = "step.progressed";

        public DateTimeOffset Timestamp { get; }
        public string StepId { get; }
        public string EventType { get; }
        public StepOutcome Outcome { get; }
        public int? AttemptNumber { get; }
        public IReadOnlyDictionary<string, string> Details { get; }

        public ScenarioEvent(DateTimeOffset timestamp, string stepId, string eventType,
            StepOutcome outcome = StepOutcome.None, int? attemptNumber = null,
            IDictionary<string, string> details = null)
        {
            if (string.IsNullOrWhiteSpace(stepId))
                throw new ArgumentException("A step identifier is required.", nameof(stepId));
            if (string.IsNullOrWhiteSpace(eventType))
                throw new ArgumentException("An event type is required.", nameof(eventType));
            if (!Enum.IsDefined(typeof(StepOutcome), outcome))
                throw new ArgumentOutOfRangeException(nameof(outcome));
            if (attemptNumber.HasValue && attemptNumber.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(attemptNumber));

            Timestamp = timestamp;
            StepId = stepId;
            EventType = eventType;
            Outcome = outcome;
            AttemptNumber = attemptNumber;
            Details = new ReadOnlyDictionary<string, string>(details == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(details));
        }
    }
}
