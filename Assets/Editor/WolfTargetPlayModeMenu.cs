#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WolfTargetPlayModeMenu
{
    [MenuItem("Tools/Wolf Target Look/Enter Play Mode")]
    public static void EnterPlayMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorSceneManager.SaveOpenScenes();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Wolf Target Look/Exit Play Mode")]
    public static void ExitPlayMode()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.isPlaying = false;
    }
}
#endif
