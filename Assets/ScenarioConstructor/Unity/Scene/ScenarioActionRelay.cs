using System;
using System.Collections.Generic;
using ScenarioConstructor.Conditions;
using ScenarioConstructor.Unity.Authoring;
using UnityEngine;

namespace ScenarioConstructor.Unity.Scene
{
    [DisallowMultipleComponent]
    public sealed class ScenarioActionRelay : MonoBehaviour
    {
        [Serializable]
        private sealed class StateBinding
        {
            [SerializeField] private string flagId;
            [SerializeField] private ScenarioBooleanState source;

            public string FlagId => flagId;
            public ScenarioBooleanState Source => source;
        }

        [SerializeField] private ScenarioSession session;
        [SerializeField] private ScenarioStepAsset step;
        [SerializeField] private string actionId = "perform";
        [SerializeField] private List<StateBinding> stateFlags = new List<StateBinding>();

        public void Raise()
        {
            if (!isActiveAndEnabled || session == null || !session.isActiveAndEnabled || !session.HasRun)
                return;
            if (!TryValidate(out var diagnostic))
            {
                Debug.LogError(diagnostic, this);
                return;
            }

            var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var binding in stateFlags)
                flags.Add(binding.FlagId, binding.Source.Value);
            try
            {
                session.Submit(step, new StepSignal(actionId, flags));
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Cannot submit action: {exception.Message}", this);
            }
        }

        public bool TryValidate(out string diagnostic)
        {
            if (session == null || step == null || string.IsNullOrWhiteSpace(actionId))
            {
                diagnostic = "Select a session, a step, and a nonempty action identifier.";
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in stateFlags)
            {
                if (binding == null || binding.Source == null || string.IsNullOrWhiteSpace(binding.FlagId)
                    || !ids.Add(binding.FlagId))
                {
                    diagnostic = "State bindings need unique flag identifiers and assigned state sources.";
                    return false;
                }
            }
            diagnostic = "Action relay is valid.";
            return true;
        }
    }
}
