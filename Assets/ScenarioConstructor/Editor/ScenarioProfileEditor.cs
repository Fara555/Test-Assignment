using ScenarioConstructor.Unity.Authoring;
using UnityEditor;
using UnityEngine;

namespace ScenarioConstructor.Editor
{
    [CustomEditor(typeof(ScenarioProfile)), CanEditMultipleObjects]
    internal sealed class ScenarioProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            foreach (var selected in targets)
            {
                var profile = (ScenarioProfile)selected;
                var valid = profile.TryValidate(out var diagnostic);
                EditorGUILayout.HelpBox($"{profile.name}: {diagnostic}", valid ? MessageType.Info : MessageType.Error);
            }
        }
    }
}
