using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BackgroundLayerRenderer))]
public class BackgroundLayerRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Background Draft Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This component is an editor helper for drafting background layout. It does not auto-generate at runtime.",
            MessageType.Info);

        BackgroundLayerRenderer renderer = (BackgroundLayerRenderer)target;

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Randomize Seed"))
            {
                Undo.RecordObject(renderer, "Randomize Background Seed");
                renderer.GenerateRandomSeed();
            }

            if (GUILayout.Button("Generate Draft In Editor"))
            {
                Undo.RegisterFullObjectHierarchyUndo(renderer.gameObject, "Generate Background Draft");
                renderer.GenerateDraftInEditor();
            }

            if (GUILayout.Button("Clear Generated Background"))
            {
                Undo.RegisterFullObjectHierarchyUndo(renderer.gameObject, "Clear Background Draft");
                renderer.ClearGeneratedBackground();
            }

            if (GUILayout.Button("Save Current Background As Prefab"))
            {
                bool saved = renderer.SaveCurrentBackgroundAsPrefab();
                if (saved)
                    AssetDatabase.Refresh();
            }
        }
    }
}
