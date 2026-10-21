using System;
using System.Collections.Generic;
using ScenarioConstructor.Execution;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [Serializable]
    public sealed class StepSetConfiguration
    {
        [SerializeField] private string id;
        [SerializeField] private List<ScenarioStepAsset> steps = new List<ScenarioStepAsset>();

        public StepSet Build(ISet<ScenarioStepAsset> roster)
        {
            var ids = new List<string>();
            foreach (var step in steps)
            {
                if (step == null || !roster.Contains(step))
                    throw new InvalidOperationException($"Set '{id}' must reference steps from this profile's roster.");
                ids.Add(step.StepId);
            }
            return new StepSet(id, ids.ToArray());
        }
    }
}
