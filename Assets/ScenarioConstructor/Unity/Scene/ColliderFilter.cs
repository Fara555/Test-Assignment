using System;
using UnityEngine;

namespace ScenarioConstructor.Unity.Scene
{
    [Serializable]
    public sealed class ColliderFilter
    {
        [SerializeField] private LayerMask acceptedLayers = ~0;
        [SerializeField, Tooltip("Optional exact collider. Leave empty to accept any collider on the selected layers.")]
        private Collider requiredCollider;

        public bool Accepts(Collider other)
        {
            return other != null && (acceptedLayers.value & (1 << other.gameObject.layer)) != 0
                && (requiredCollider == null || other == requiredCollider);
        }
    }
}
