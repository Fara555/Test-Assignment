using System;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Unity.Authoring
{
    public abstract class ExecutionPolicyAsset : ScenarioExecutionAsset
    {
        public abstract IExecutionPolicy CreatePolicy();

        public override void Validate(ExecutionPlan plan) => RequirePolicy().Validate(plan);

        public override IScenarioExecution CreateExecution(ExecutionPlan plan, Func<string, IScenarioLogger> loggerFactory)
        {
            return new ScenarioRunner(plan, RequirePolicy(), loggerFactory);
        }

        private IExecutionPolicy RequirePolicy()
        {
            return CreatePolicy() ?? throw new InvalidOperationException("The execution asset did not create a policy.");
        }
    }
}
