using ScenarioConstructor.Conditions;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    public abstract class StepRuleAsset : ScriptableObject
    {
        public abstract IStepRule CreateRule();
    }
}
