using ScenarioConstructor.Execution;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    public abstract class ExecutionPolicyAsset : ScriptableObject
    {
        public abstract IExecutionPolicy CreatePolicy();
    }
}
