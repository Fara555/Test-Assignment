using System;
using System.Collections.Generic;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Execution
{
    public sealed class ScenarioRunner
    {
        private readonly ExecutionPlan plan;
        private readonly IExecutionPolicy policy;
        private readonly Func<string, IScenarioLogger> loggerFactory;
        private readonly Func<DateTimeOffset> clock;
        private readonly List<string> stepIds = new List<string>();
        private readonly Dictionary<string, int> groupByStep = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<StepSet>[] batchesByGroup;
        private Dictionary<string, RuntimeStep> steps;
        private IScenarioLogger logger;
        private int[] groupViolations;
        private int currentGroup;

        public string RunId { get; private set; }
        public bool IsFinished => currentGroup == plan.Groups.Count;
        public string CurrentGroupId => IsFinished ? null : plan.Groups[currentGroup].Id;
        public ScenarioJournal PreviousJournal { get; private set; }
        public event Action StateChanged;

        public ScenarioRunner(ExecutionPlan plan, IExecutionPolicy policy,
            Func<string, IScenarioLogger> loggerFactory = null, Func<DateTimeOffset> clock = null)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            policy.Validate(plan);
            if (policy.GroupViolationLimit.HasValue && policy.GroupViolationLimit.Value < 1)
                throw new ArgumentException("A violation limit must be positive.", nameof(policy));

            this.clock = clock ?? (() => DateTimeOffset.UtcNow);
            foreach (var step in plan.Steps)
                stepIds.Add(step.Id);
            this.loggerFactory = loggerFactory ?? (id => new InMemoryScenarioLogger(plan.ScenarioId, id, stepIds));

            batchesByGroup = new List<StepSet>[plan.Groups.Count];
            for (var i = 0; i < plan.Groups.Count; i++)
            {
                batchesByGroup[i] = new List<StepSet>();
                foreach (var id in plan.Groups[i].StepIds)
                    groupByStep.Add(id, i);
            }
            foreach (var batch in plan.ActivationBatches)
                batchesByGroup[groupByStep[batch.StepIds[0]]].Add(batch);

            Restart();
        }

        public StepProgress GetProgress(string stepId)
        {
            var step = FindStep(stepId);
            return new StepProgress(step.Definition.Id, step.Status, step.AttemptNumber, step.Violations);
        }

        public int GetGroupViolations(string groupId)
        {
            for (var i = 0; i < plan.Groups.Count; i++)
            {
                if (plan.Groups[i].Id == groupId)
                    return groupViolations[i];
            }
            throw new ArgumentException($"Unknown group '{groupId}'.", nameof(groupId));
        }

        public ScenarioJournal CaptureJournal() => logger.Capture();

        public ConditionResult Submit(string stepId, StepSignal signal)
        {
            if (signal == null)
                throw new ArgumentNullException(nameof(signal));
            var step = FindStep(stepId);
            if (IsTerminal(step.Status) || !step.Definition.Rule.Accepts(signal.ActionId))
                return ConditionResult.Ignored();
            if (step.Status == StepStatus.Pending && plan.PendingActions == PendingActionBehavior.Ignore)
                return ConditionResult.Ignored();

            var result = step.Status == StepStatus.Pending
                ? ConditionResult.Rejected("This step is not available yet.")
                : step.Condition.Evaluate(signal);

            switch (result.Status)
            {
                case ConditionStatus.Ignored:
                    return result;
                case ConditionStatus.Progressed:
                    Record(step, ScenarioEvent.StepProgressed, signal.ActionId);
                    break;
                case ConditionStatus.Completed:
                    Resolve(step, StepStatus.Completed, signal.ActionId);
                    break;
                case ConditionStatus.Rejected:
                    Reject(step, signal.ActionId, result.Reason);
                    break;
                default:
                    throw new InvalidOperationException("The step condition returned an unknown status.");
            }

            Advance();
            StateChanged?.Invoke();
            return result;
        }

        public void Restart()
        {
            var nextSteps = new Dictionary<string, RuntimeStep>(StringComparer.Ordinal);
            foreach (var definition in plan.Steps)
                nextSteps.Add(definition.Id, new RuntimeStep(definition));

            var nextRunId = Guid.NewGuid().ToString("N");
            var nextLogger = loggerFactory(nextRunId)
                ?? throw new InvalidOperationException("The logger factory returned null.");
            var header = nextLogger.Capture();
            if (header == null || header.RunId != nextRunId || header.ScenarioId != plan.ScenarioId
                || header.Events.Count != 0 || !new HashSet<string>(header.StepIds).SetEquals(stepIds))
                throw new InvalidOperationException("The logger must start empty with the current run and step roster.");

            PreviousJournal = logger?.Capture();
            steps = nextSteps;
            logger = nextLogger;
            RunId = nextRunId;
            groupViolations = new int[plan.Groups.Count];
            currentGroup = 0;
            Advance();
            StateChanged?.Invoke();
        }

        private void Reject(RuntimeStep step, string actionId, string reason)
        {
            step.Violations++;
            groupViolations[groupByStep[step.Definition.Id]]++;
            Record(step, ScenarioEvent.AttemptRejected, actionId, reason);
            if (policy.RetryOnViolation)
            {
                step.AttemptNumber++;
                step.ResetCondition();
            }
            else
            {
                Resolve(step, StepStatus.Failed, actionId, reason);
            }
        }

        private void Advance()
        {
            while (!IsFinished)
            {
                var group = plan.Groups[currentGroup];
                if (policy.GroupViolationLimit.HasValue
                    && groupViolations[currentGroup] >= policy.GroupViolationLimit.Value)
                {
                    foreach (var id in group.StepIds)
                    {
                        if (!IsTerminal(steps[id].Status))
                            Resolve(steps[id], StepStatus.Skipped, reason: "The group violation limit was reached.");
                    }
                }

                var groupComplete = true;
                foreach (var id in group.StepIds)
                    groupComplete &= IsTerminal(steps[id].Status);
                if (groupComplete)
                {
                    currentGroup++;
                    continue;
                }

                foreach (var batch in batchesByGroup[currentGroup])
                {
                    var batchComplete = true;
                    foreach (var id in batch.StepIds)
                        batchComplete &= IsTerminal(steps[id].Status);
                    if (batchComplete)
                        continue;

                    foreach (var id in batch.StepIds)
                    {
                        var step = steps[id];
                        if (step.Status != StepStatus.Pending)
                            continue;
                        step.Status = StepStatus.Active;
                        Record(step, ScenarioEvent.StepActivated);
                    }
                    return;
                }
            }
        }

        private void Resolve(RuntimeStep step, StepStatus status, string actionId = null, string reason = null)
        {
            step.Status = status;
            var outcome = status == StepStatus.Completed ? StepOutcome.Completed
                : status == StepStatus.Failed ? StepOutcome.Failed : StepOutcome.Skipped;
            Record(step, ScenarioEvent.StepResolved, actionId, reason, outcome);
        }

        private void Record(RuntimeStep step, string eventType, string actionId = null,
            string reason = null, StepOutcome outcome = StepOutcome.None)
        {
            var details = new Dictionary<string, string>
            {
                { "groupId", plan.Groups[groupByStep[step.Definition.Id]].Id }
            };
            if (actionId != null)
                details.Add("actionId", actionId);
            if (reason != null)
                details.Add("reason", reason);

            logger.Record(new ScenarioEvent(clock(), step.Definition.Id, eventType, outcome,
                outcome == StepOutcome.Skipped ? (int?)null : step.AttemptNumber, details));
        }

        private RuntimeStep FindStep(string stepId)
        {
            if (stepId == null || !steps.TryGetValue(stepId, out var step))
                throw new ArgumentException($"Unknown step '{stepId}'.", nameof(stepId));
            return step;
        }

        private static bool IsTerminal(StepStatus status)
        {
            return status == StepStatus.Completed || status == StepStatus.Failed || status == StepStatus.Skipped;
        }

        private sealed class RuntimeStep
        {
            public StepDefinition Definition { get; }
            public IStepCondition Condition { get; private set; }
            public StepStatus Status { get; set; }
            public int AttemptNumber { get; set; } = 1;
            public int Violations { get; set; }

            public RuntimeStep(StepDefinition definition)
            {
                Definition = definition;
                ResetCondition();
            }

            public void ResetCondition()
            {
                Condition = Definition.Rule.CreateCondition()
                    ?? throw new InvalidOperationException($"Step '{Definition.Id}' did not create a condition.");
            }
        }
    }
}
