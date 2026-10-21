using UnityEngine;

namespace ScenarioConstructor.Unity.Scene
{
    [DisallowMultipleComponent]
    public sealed class CollisionActionSource : MonoBehaviour
    {
        [SerializeField] private ScenarioActionRelay action;
        [SerializeField] private ColliderFilter filter = new ColliderFilter();

        private void OnCollisionEnter(Collision collision)
        {
            if (isActiveAndEnabled && action != null && filter.Accepts(collision.collider))
                action.Raise();
        }
    }
}
