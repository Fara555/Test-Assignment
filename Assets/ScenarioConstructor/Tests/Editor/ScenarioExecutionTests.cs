using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ScenarioConstructor.Analysis;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Tests
{
    public sealed class ScenarioExecutionTests
    {
        [Test]
        public void ConcurrentStepsCompleteInAnyOrderBeforeNextBatchOpens()
        {
            var runner = Training(new[] { Step("a"), Step("b"), Step("c") },
                Set("first", "a", "b"), Set("next", "c"));

            Assert.That(runner.GetProgress("a").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Active));
            Perform(runner, "b");
            Assert.That(runner.GetProgress("c").Status, Is.EqualTo(StepStatus.Pending));
            Perform(runner, "a");
            Assert.That(runner.GetProgress("c").Status, Is.EqualTo(StepStatus.Active));
            Perform(runner, "c");
            Assert.That(runner.IsFinished, Is.True);
        }

        [Test]
        public void AssignmentMixedActivationExampleProgressesThroughAllTwelveSteps()
        {
            var definitions = Enumerable.Range(1, 12).Select(i => Step(i.ToString())).ToArray();
            var runner = Training(definitions, Set("first", "1", "2", "3", "4"), Set("five", "5"),
                Set("six", "6"), Set("seven", "7"), Set("eight", "8"),
                Set("pair-a", "9", "10"), Set("pair-b", "11", "12"));

            foreach (var id in new[] { "4", "2", "1" })
                Perform(runner, id);
            Assert.That(runner.GetProgress("5").Status, Is.EqualTo(StepStatus.Pending));
            Perform(runner, "3");
            foreach (var id in new[] { "5", "6", "7", "8" })
            {
                Assert.That(runner.GetProgress(id).Status, Is.EqualTo(StepStatus.Active));
                Perform(runner, id);
            }
            Perform(runner, "10");
            Assert.That(runner.GetProgress("11").Status, Is.EqualTo(StepStatus.Pending));
            Perform(runner, "9");
            Perform(runner, "12");
            Perform(runner, "11");

            Assert.That(runner.IsFinished, Is.True);
            Assert.That(new TrainingAnalyzer().Analyze(runner.CaptureJournal()).Score, Is.EqualTo(120));
        }

        [Test]
        public void TrainingRetriesProduceReferenceScoreFromExecutedActions()
        {
            var runner = Training(new[] { Gated("a"), Gated("b"), Gated("c") }, Set("all", "a", "b", "c"));
            PerformGated(runner, "a", true);
            PerformGated(runner, "b", false);
            PerformGated(runner, "b", true);
            PerformGated(runner, "c", false);
            PerformGated(runner, "c", false);
            PerformGated(runner, "c", true);

            var result = new TrainingAnalyzer().Analyze(runner.CaptureJournal());
            Assert.That(result.Score, Is.EqualTo(15));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(runner.GetProgress("c").AttemptNumber, Is.EqualTo(3));
        }

        [Test]
        public void TrainingViolationLeavesOtherActiveStepsAndTheirProgressIntact()
        {
            var runner = Training(new[] { Gated("a"), Ordered("b") }, Set("all", "a", "b"));
            runner.Submit("b", new StepSignal("enter-a"));
            PerformGated(runner, "a", false);
            Assert.That(runner.GetProgress("a").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Active));
            runner.Submit("b", new StepSignal("enter-b"));
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Completed));
            Assert.That(runner.GetProgress("b").AttemptNumber, Is.EqualTo(1));
        }

        [Test]
        public void TrainingNeverSkipsGroupAfterRepeatedViolations()
        {
            var runner = Training(new[] { Gated("a"), Step("b") }, Set("all", "a", "b"));
            for (var i = 0; i < 5; i++)
                PerformGated(runner, "a", false);

            Assert.That(runner.IsFinished, Is.False);
            Assert.That(runner.GetProgress("a").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(runner.GetProgress("a").Violations, Is.EqualTo(5));
        }

        [Test]
        public void OrderedConditionRejectsWrongOrderAndResetsOnlyFailedAttempt()
        {
            var runner = Training(new[] { Ordered("a") }, Set("all", "a"));
            var rejection = runner.Submit("a", new StepSignal("enter-b"));
            Assert.That(rejection.Status, Is.EqualTo(ConditionStatus.Rejected));
            Assert.That(rejection.Reason, Does.Contain("enter-a"));
            Assert.That(runner.Submit("a", new StepSignal("enter-a")).Status, Is.EqualTo(ConditionStatus.Progressed));
            Assert.That(runner.Submit("a", new StepSignal("enter-b")).Status, Is.EqualTo(ConditionStatus.Completed));
            Assert.That(new TrainingAnalyzer().Analyze(runner.CaptureJournal()).Score, Is.EqualTo(5));
        }

        [Test]
        public void ContextualPartialProgressDoesNotConsumeAnotherAttempt()
        {
            var runner = Training(new[] { Ordered("a") }, Set("all", "a"));
            runner.Submit("a", new StepSignal("enter-a"));
            runner.Submit("a", new StepSignal("enter-b"));
            Assert.That(runner.GetProgress("a").AttemptNumber, Is.EqualTo(1));
            Assert.That(new TrainingAnalyzer().Analyze(runner.CaptureJournal()).Score, Is.EqualTo(10));
        }

        [Test]
        public void FailureAfterPartialProgressRequiresWholeSequenceAgain()
        {
            var runner = Training(new[] { Ordered("a") }, Set("all", "a"));
            runner.Submit("a", new StepSignal("enter-a"));
            runner.Submit("a", new StepSignal("enter-a"));
            Assert.That(runner.Submit("a", new StepSignal("enter-b")).Status, Is.EqualTo(ConditionStatus.Rejected));
            runner.Submit("a", new StepSignal("enter-a"));
            runner.Submit("a", new StepSignal("enter-b"));
            Assert.That(runner.GetProgress("a").AttemptNumber, Is.EqualTo(3));
        }

        [Test]
        public void MissingContextFlagIsReportedAsViolationWithReason()
        {
            var runner = Training(new[] { Gated("a") }, Set("all", "a"));
            var result = Perform(runner, "a");
            Assert.That(result.Status, Is.EqualTo(ConditionStatus.Rejected));
            Assert.That(result.Reason, Does.Contain("not supplied"));
            Assert.That(runner.CaptureJournal().Events.Last().Details["reason"], Does.Contain("not supplied"));
        }

        [Test]
        public void GateCanRequireFalseRatherThanTrue()
        {
            var step = new StepDefinition("a", "Require power off", new StateGatedActionRule("perform", "ready", false));
            var runner = Training(new[] { step }, Set("all", "a"));
            PerformGated(runner, "a", false);
            Assert.That(runner.IsFinished, Is.True);
        }

        [Test]
        public void PendingActionIsViolationByDefaultAndCountsTowardTrainingAttempts()
        {
            var runner = Training(new[] { Step("a"), Step("b") }, Set("first", "a"), Set("next", "b"));
            Assert.That(Perform(runner, "b").Status, Is.EqualTo(ConditionStatus.Rejected));
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Pending));
            Perform(runner, "a");
            Perform(runner, "b");
            Assert.That(new TrainingAnalyzer().Analyze(runner.CaptureJournal()).Score, Is.EqualTo(15));
        }

        [Test]
        public void DisabledPendingStepsIgnoreActionsWithoutConsumingAttempts()
        {
            var plan = Plan(new[] { Step("a"), Step("b") }, new[] { Set("group", "a", "b") },
                new[] { Set("first", "a"), Set("next", "b") }, PendingActionBehavior.Ignore);
            var runner = new ScenarioRunner(plan, new TrainingExecutionPolicy());
            var before = runner.CaptureJournal().Events.Count;
            Assert.That(Perform(runner, "b").Status, Is.EqualTo(ConditionStatus.Ignored));
            Assert.That(runner.CaptureJournal().Events.Count, Is.EqualTo(before));
            Assert.That(runner.GetProgress("b").AttemptNumber, Is.EqualTo(1));
            Assert.That(runner.GetProgress("b").Violations, Is.Zero);
        }

        [Test]
        public void UnrelatedActionDoesNotCountAsOutOfOrderViolation()
        {
            var runner = Training(new[] { Step("a"), Step("b") }, Set("first", "a"), Set("next", "b"));
            Assert.That(runner.Submit("b", new StepSignal("unrelated")).Status, Is.EqualTo(ConditionStatus.Ignored));
            Assert.That(runner.GetProgress("b").Violations, Is.Zero);
        }

        [Test]
        public void TerminalStepIgnoresRepeatedActions()
        {
            var runner = Training(new[] { Step("a"), Step("b") }, Set("all", "a", "b"));
            Perform(runner, "a");
            var count = runner.CaptureJournal().Events.Count;
            Assert.That(Perform(runner, "a").Status, Is.EqualTo(ConditionStatus.Ignored));
            Assert.That(runner.CaptureJournal().Events.Count, Is.EqualTo(count));
        }

        [Test]
        public void ExamThresholdProducesReferenceStatusesAndSeventyPoints()
        {
            var plan = Plan(new[] { Gated("a"), Gated("b"), Step("c"), Step("d"), Step("e") },
                new[] { Set("first", "a", "b", "c", "d"), Set("second", "e") },
                new[] { Set("first-batch", "a", "b", "c", "d"), Set("second-batch", "e") });
            var runner = new ScenarioRunner(plan, new ExamExecutionPolicy());
            PerformGated(runner, "a", false);
            Assert.That(runner.GetProgress("c").Status, Is.EqualTo(StepStatus.Active));
            PerformGated(runner, "b", false);

            Assert.That(runner.GetProgress("a").Status, Is.EqualTo(StepStatus.Failed));
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Failed));
            Assert.That(runner.GetProgress("c").Status, Is.EqualTo(StepStatus.Skipped));
            Assert.That(runner.GetProgress("d").Status, Is.EqualTo(StepStatus.Skipped));
            Assert.That(runner.CurrentGroupId, Is.EqualTo("second"));
            Perform(runner, "e");
            var result = new ExamAnalyzer().Analyze(runner.CaptureJournal());
            Assert.That(result.Score, Is.EqualTo(70));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void ExamFailureUnlocksNextSequentialStepWithoutRetry()
        {
            var plan = Plan(new[] { Gated("a"), Step("b") }, new[] { Set("group", "a", "b") },
                new[] { Set("first", "a"), Set("next", "b") });
            var runner = new ScenarioRunner(plan, new ExamExecutionPolicy());
            PerformGated(runner, "a", false);
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(PerformGated(runner, "a", true).Status, Is.EqualTo(ConditionStatus.Ignored));
            Perform(runner, "b");
            Assert.That(new ExamAnalyzer().Analyze(runner.CaptureJournal()).Score, Is.EqualTo(90));
        }

        [Test]
        public void CustomExamThresholdSkipsPendingAsWellAsActiveSteps()
        {
            var plan = Plan(new[] { Gated("a"), Step("b"), Step("c") }, new[] { Set("group", "a", "b", "c") },
                new[] { Set("first", "a"), Set("next", "b", "c") });
            var runner = new ScenarioRunner(plan, new ExamExecutionPolicy(1));
            PerformGated(runner, "a", false);
            Assert.That(runner.IsFinished, Is.True);
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Skipped));
            Assert.That(runner.GetProgress("c").Status, Is.EqualTo(StepStatus.Skipped));
        }

        [Test]
        public void ExamGroupBudgetsRemainIndependentAcrossThreeGroups()
        {
            var definitions = new[] { Gated("a"), Step("b"), Gated("c"), Step("d"), Step("e") };
            var sets = new[] { Set("one", "a", "b"), Set("two", "c", "d"), Set("three", "e") };
            var runner = new ScenarioRunner(Plan(definitions, sets, sets), new ExamExecutionPolicy());
            PerformGated(runner, "a", false);
            Perform(runner, "b");
            PerformGated(runner, "c", false);
            Assert.That(runner.GetProgress("d").Status, Is.EqualTo(StepStatus.Active));
            Perform(runner, "d");
            Assert.That(runner.CurrentGroupId, Is.EqualTo("three"));
            Perform(runner, "e");
            Assert.That(runner.IsFinished, Is.True);
            Assert.That(runner.GetGroupViolations("one"), Is.EqualTo(1));
            Assert.That(runner.GetGroupViolations("two"), Is.EqualTo(1));
            Assert.That(runner.GetGroupViolations("three"), Is.Zero);
        }

        [Test]
        public void FutureGroupViolationBelongsToTargetGroupAndThresholdAppliesOnEntry()
        {
            var sets = new[] { Set("one", "a"), Set("two", "b", "c"), Set("three", "d") };
            var runner = new ScenarioRunner(Plan(new[] { Step("a"), Step("b"), Step("c"), Step("d") }, sets, sets),
                new ExamExecutionPolicy(1));
            Perform(runner, "b");
            Assert.That(runner.CurrentGroupId, Is.EqualTo("one"));
            Assert.That(runner.GetGroupViolations("one"), Is.Zero);
            Assert.That(runner.GetGroupViolations("two"), Is.EqualTo(1));
            Perform(runner, "a");
            Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Failed));
            Assert.That(runner.GetProgress("c").Status, Is.EqualTo(StepStatus.Skipped));
            Assert.That(runner.CurrentGroupId, Is.EqualTo("three"));
        }

        [Test]
        public void SameStepDefinitionsSupportDifferentModesAndActivationPlans()
        {
            var definitions = new[] { Step("a"), Step("b") };
            var training = Training(definitions, Set("all", "a", "b"));
            var exam = new ScenarioRunner(Plan(definitions, new[] { Set("group", "a", "b") },
                new[] { Set("first", "b"), Set("second", "a") }), new ExamExecutionPolicy());

            Assert.That(training.GetProgress("a").Status, Is.EqualTo(StepStatus.Active));
            Assert.That(exam.GetProgress("a").Status, Is.EqualTo(StepStatus.Pending));
            Perform(training, "a");
            Assert.That(exam.GetProgress("a").Status, Is.EqualTo(StepStatus.Pending));
        }

        [Test]
        public void RunsUsingSameOrderedRuleDoNotSharePartialProgress()
        {
            var definitions = new[] { Ordered("a") };
            var first = Training(definitions, Set("all", "a"));
            var second = Training(definitions, Set("all", "a"));
            first.Submit("a", new StepSignal("enter-a"));
            Assert.That(second.Submit("a", new StepSignal("enter-b")).Status, Is.EqualTo(ConditionStatus.Rejected));
            Assert.That(first.Submit("a", new StepSignal("enter-b")).Status, Is.EqualTo(ConditionStatus.Completed));
        }

        [Test]
        public void RestartResetsProgressAndRetainsPreviousImmutableJournal()
        {
            var runner = Training(new[] { Ordered("a") }, Set("all", "a"));
            runner.Submit("a", new StepSignal("enter-a"));
            var previousRun = runner.RunId;
            var oldSnapshot = runner.CaptureJournal();
            runner.Restart();

            Assert.That(runner.RunId, Is.Not.EqualTo(previousRun));
            Assert.That(runner.PreviousJournal.RunId, Is.EqualTo(previousRun));
            Assert.That(runner.PreviousJournal.Events.Count, Is.EqualTo(oldSnapshot.Events.Count));
            Assert.That(runner.GetProgress("a").AttemptNumber, Is.EqualTo(1));
            Assert.That(runner.GetProgress("a").Violations, Is.Zero);
            Assert.That(runner.Submit("a", new StepSignal("enter-b")).Status, Is.EqualTo(ConditionStatus.Rejected));
            Assert.That(oldSnapshot.Events.Count, Is.EqualTo(2));
        }

        [Test]
        public void CompletedRunCanRestartAndFinishAgain()
        {
            var runner = Training(new[] { Step("a") }, Set("all", "a"));
            Perform(runner, "a");
            runner.Restart();
            Assert.That(runner.IsFinished, Is.False);
            Perform(runner, "a");
            Assert.That(runner.IsFinished, Is.True);
            Assert.That(new TrainingAnalyzer().Analyze(runner.PreviousJournal).Score, Is.EqualTo(10));
        }

        [Test]
        public void RecordingUsesInjectedClockAndIncludesViolationContext()
        {
            var timestamp = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
            var plan = Plan(new[] { Gated("a") }, new[] { Set("group", "a") }, new[] { Set("batch", "a") });
            var runner = new ScenarioRunner(plan, new TrainingExecutionPolicy(), clock: () => timestamp);
            PerformGated(runner, "a", false);
            var entry = runner.CaptureJournal().Events.Last();
            Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
            Assert.That(entry.EventType, Is.EqualTo(ScenarioEvent.AttemptRejected));
            Assert.That(entry.AttemptNumber, Is.EqualTo(1));
            Assert.That(entry.Details["groupId"], Is.EqualTo("group"));
            Assert.That(entry.Details["actionId"], Is.EqualTo("perform"));
            Assert.That(entry.Details["reason"], Is.Not.Empty);
        }

        [Test]
        public void StateNotificationObservesFullyAdvancedState()
        {
            var runner = Training(new[] { Step("a"), Step("b") }, Set("first", "a"), Set("next", "b"));
            var notifications = 0;
            runner.StateChanged += () =>
            {
                notifications++;
                Assert.That(runner.GetProgress("b").Status, Is.EqualTo(StepStatus.Active));
            };
            Perform(runner, "a");
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void ProgressSnapshotsDoNotChangeAfterExecution()
        {
            var runner = Training(new[] { Step("a") }, Set("all", "a"));
            var progress = runner.GetProgress("a");
            Perform(runner, "a");
            Assert.That(progress.Status, Is.EqualTo(StepStatus.Active));
            Assert.That(runner.GetProgress("a").Status, Is.EqualTo(StepStatus.Completed));
        }

        [Test]
        public void UnknownStepAndNullSignalAreRejectedAtBoundary()
        {
            var runner = Training(new[] { Step("a") }, Set("all", "a"));
            Assert.Throws<ArgumentException>(() => Perform(runner, "unknown"));
            Assert.Throws<ArgumentNullException>(() => runner.Submit("a", null));
        }

        [Test]
        public void IncompatibleLoggerIsRejectedBeforeRunStarts()
        {
            var plan = Plan(new[] { Step("a") }, new[] { Set("group", "a") }, new[] { Set("batch", "a") });
            Assert.Throws<InvalidOperationException>(() => new ScenarioRunner(plan, new TrainingExecutionPolicy(),
                id => new InMemoryScenarioLogger("wrong-scenario", id, new[] { "a" })));
        }

        private static StepDefinition Step(string id) => new StepDefinition(id, $"Step {id}", new OrderedActionsRule("perform"));
        private static StepDefinition Ordered(string id) => new StepDefinition(id, $"Sequence {id}", new OrderedActionsRule("enter-a", "enter-b"));
        private static StepDefinition Gated(string id) => new StepDefinition(id, $"Gated {id}", new StateGatedActionRule("perform", "ready"));
        private static StepSet Set(string id, params string[] steps) => new StepSet(id, steps);

        private static ExecutionPlan Plan(StepDefinition[] steps, StepSet[] groups, StepSet[] batches,
            PendingActionBehavior behavior = PendingActionBehavior.RecordViolation)
        {
            return new ExecutionPlan("scenario", steps, groups, batches, behavior);
        }

        private static ScenarioRunner Training(StepDefinition[] steps, params StepSet[] batches)
        {
            return new ScenarioRunner(Plan(steps, new[] { Set("training", steps.Select(s => s.Id).ToArray()) }, batches),
                new TrainingExecutionPolicy());
        }

        private static ConditionResult Perform(ScenarioRunner runner, string id) => runner.Submit(id, new StepSignal("perform"));

        private static ConditionResult PerformGated(ScenarioRunner runner, string id, bool ready)
        {
            return runner.Submit(id, new StepSignal("perform", new Dictionary<string, bool> { { "ready", ready } }));
        }
    }
}
