using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridRenderer))]
public class GridRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Preview Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Debug helper: render GridManager's auto-generated underground grid in Edit Mode (without entering Play).",
            MessageType.Info);

        GridRenderer renderer = (GridRenderer)target;

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Generate Grid In Editor"))
            {
                Undo.RegisterFullObjectHierarchyUndo(renderer.gameObject, "Generate Grid Preview");
                renderer.GenerateGridInEditor();
            }

            if (GUILayout.Button("Clear Generated Grid"))
            {
                Undo.RegisterFullObjectHierarchyUndo(renderer.gameObject, "Clear Grid Preview");
                renderer.ClearGeneratedGrid();
            }
        }
    }
}
