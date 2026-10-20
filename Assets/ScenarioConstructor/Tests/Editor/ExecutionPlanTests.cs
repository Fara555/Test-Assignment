using System;
using NUnit.Framework;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Execution;

namespace ScenarioConstructor.Tests
{
    public sealed class ExecutionPlanTests
    {
        private static readonly StepDefinition[] Steps =
        {
            new StepDefinition("a", "First step", new OrderedActionsRule("perform")),
            new StepDefinition("b", "Second step", new OrderedActionsRule("perform"))
        };

        [Test]
        public void MissingStepInActivationPlanIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ExecutionPlan("scenario", Steps,
                new[] { new StepSet("group", "a", "b") }, new[] { new StepSet("batch", "a") }));
        }

        [Test]
        public void StepCannotBelongToMultipleGroups()
        {
            Assert.Throws<ArgumentException>(() => new ExecutionPlan("scenario", Steps,
                new[] { new StepSet("one", "a", "b"), new StepSet("two", "b") },
                new[] { new StepSet("batch", "a", "b") }));
        }

        [Test]
        public void ActivationBatchCannotSpanSequentialExamGroups()
        {
            Assert.Throws<ArgumentException>(() => new ExecutionPlan("scenario", Steps,
                new[] { new StepSet("one", "a"), new StepSet("two", "b") },
                new[] { new StepSet("batch", "a", "b") }));
        }

        [Test]
        public void ActivationOrderCannotReturnToEarlierGroup()
        {
            Assert.Throws<ArgumentException>(() => new ExecutionPlan("scenario", Steps,
                new[] { new StepSet("one", "a"), new StepSet("two", "b") },
                new[] { new StepSet("later", "b"), new StepSet("earlier", "a") }));
        }

        [Test]
        public void TrainingRejectsMultipleGroups()
        {
            var groups = new[] { new StepSet("one", "a"), new StepSet("two", "b") };
            var plan = new ExecutionPlan("scenario", Steps, groups, groups);
            Assert.Throws<ArgumentException>(() => new ScenarioRunner(plan, new TrainingExecutionPolicy()));
        }

        [Test]
        public void ExamThresholdMustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExamExecutionPolicy(0));
        }

        [Test]
        public void PlanAndActionSequenceCopyAuthoringCollections()
        {
            var actions = new[] { "original" };
            var rule = new OrderedActionsRule(actions);
            actions[0] = "changed";
            Assert.That(rule.Accepts("original"), Is.True);
            Assert.That(rule.Accepts("changed"), Is.False);

            var ids = new[] { "a", "b" };
            var set = new StepSet("group", ids);
            ids[0] = "changed";
            Assert.That(set.StepIds[0], Is.EqualTo("a"));

            var definitions = (StepDefinition[])Steps.Clone();
            var plan = new ExecutionPlan("scenario", definitions, new[] { set }, new[] { set });
            definitions[0] = null;
            Assert.That(plan.Steps[0], Is.Not.Null);
        }
    }
}
