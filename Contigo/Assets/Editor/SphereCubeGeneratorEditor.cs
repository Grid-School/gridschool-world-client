using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SphereCubeGenerator))]
public class SphereCubeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        // Get the target script
        SphereCubeGenerator generator = (SphereCubeGenerator)target;

        // Add Generate Cubes button
        if (GUILayout.Button("Generate Cubes"))
        {
            generator.GenerateCubes();
            // Mark the scene as dirty to ensure changes are saved
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        // Add Clear Cubes button
        if (GUILayout.Button("Clear Cubes"))
        {
            generator.ClearCubes();
            // Mark the scene as dirty
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
    }
}