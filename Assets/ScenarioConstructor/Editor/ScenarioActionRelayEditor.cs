using ScenarioConstructor.Unity.Scene;
using UnityEditor;

namespace ScenarioConstructor.Editor
{
    [CustomEditor(typeof(ScenarioActionRelay)), CanEditMultipleObjects]
    internal sealed class ScenarioActionRelayEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            foreach (var selected in targets)
            {
                var valid = ((ScenarioActionRelay)selected).TryValidate(out var diagnostic);
                if (!valid)
                    EditorGUILayout.HelpBox(diagnostic, MessageType.Error);
            }
        }
    }
}
