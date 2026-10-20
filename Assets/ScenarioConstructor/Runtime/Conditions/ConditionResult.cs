using System;

namespace ScenarioConstructor.Conditions
{
    public enum ConditionStatus
    {
        Ignored,
        Progressed,
        Completed,
        Rejected
    }

    public readonly struct ConditionResult
    {
        public ConditionStatus Status { get; }
        public string Reason { get; }

        private ConditionResult(ConditionStatus status, string reason)
        {
            Status = status;
            Reason = reason;
        }

        public static ConditionResult Ignored() => new ConditionResult(ConditionStatus.Ignored, null);
        public static ConditionResult Progressed() => new ConditionResult(ConditionStatus.Progressed, null);
        public static ConditionResult Completed() => new ConditionResult(ConditionStatus.Completed, null);

        public static ConditionResult Rejected(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A rejection reason is required.", nameof(reason));
            return new ConditionResult(ConditionStatus.Rejected, reason);
        }
    }
}
