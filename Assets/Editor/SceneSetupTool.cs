using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneSetupTool
{
    [MenuItem("PitchRush/Update Build Settings")]
    public static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[2];
        newScenes[0] = new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true);
        newScenes[1] = new EditorBuildSettingsScene("Assets/Scenes/Gameplay.unity", true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log("Build settings updated.");
    }
}
