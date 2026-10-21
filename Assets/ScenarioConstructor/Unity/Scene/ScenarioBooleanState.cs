using UnityEngine;

namespace ScenarioConstructor.Unity.Scene
{
    [DisallowMultipleComponent]
    public sealed class ScenarioBooleanState : MonoBehaviour
    {
        [SerializeField] private bool value;

        public bool Value => value;
        public void SetValue(bool nextValue) => value = nextValue;
        public void SetTrue() => value = true;
        public void SetFalse() => value = false;
        public void Toggle() => value = !value;
    }
}
