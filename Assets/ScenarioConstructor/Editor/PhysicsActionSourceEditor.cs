using ScenarioConstructor.Unity.Scene;
using UnityEditor;
using UnityEngine;

namespace ScenarioConstructor.Editor
{
    [CustomEditor(typeof(TriggerActionSource)), CanEditMultipleObjects]
    internal sealed class TriggerActionSourceEditor : PhysicsActionSourceEditor
    {
        protected override bool RequiresTrigger => true;
    }

    [CustomEditor(typeof(CollisionActionSource)), CanEditMultipleObjects]
    internal sealed class CollisionActionSourceEditor : PhysicsActionSourceEditor
    {
        protected override bool RequiresTrigger => false;
    }

    internal abstract class PhysicsActionSourceEditor : UnityEditor.Editor
    {
        protected abstract bool RequiresTrigger { get; }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            foreach (var selected in targets)
            {
                var component = (Component)selected;
                var validCollider = false;
                foreach (var collider in component.GetComponents<Collider>())
                    validCollider |= collider.enabled && collider.isTrigger == RequiresTrigger;
                if (!validCollider)
                    EditorGUILayout.HelpBox(RequiresTrigger
                        ? "Add an enabled Collider on this object and enable Is Trigger."
                        : "Add an enabled Collider on this object and disable Is Trigger.", MessageType.Error);
            }
            EditorGUILayout.HelpBox("At least one participating physics object needs a Rigidbody. Use the filter to select accepted colliders or layers.", MessageType.Info);
        }
    }
}
