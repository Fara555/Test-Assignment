using System;
using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    public abstract class ScenarioExecutionAsset : ScriptableObject
    {
        public abstract void Validate(ExecutionPlan plan);
        public abstract IScenarioExecution CreateExecution(ExecutionPlan plan, Func<string, IScenarioLogger> loggerFactory);
    }
}
