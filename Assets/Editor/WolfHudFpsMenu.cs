#if UNITY_EDITOR
using HelloWorldRoom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WolfHudFpsMenu
{
    [MenuItem("Tools/Wolf Target Look/Enable FPS HUD")]
    public static void EnableFpsHud()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            Debug.LogWarning("[WolfHudFpsMenu] Exit Play Mode, then run Enable FPS HUD again.");
            return;
        }

        int hudCount = 0;
        foreach (WolfHud hud in Object.FindObjectsByType<WolfHud>(FindObjectsInactive.Include))
        {
            SerializedObject serializedHud = new SerializedObject(hud);
            SerializedProperty showFps = serializedHud.FindProperty("showFps");
            SerializedProperty fpsRefreshInterval = serializedHud.FindProperty("fpsRefreshInterval");
            SerializedProperty lowFpsThreshold = serializedHud.FindProperty("lowFpsThreshold");

            if (showFps != null)
            {
                showFps.boolValue = true;
            }

            if (fpsRefreshInterval != null && fpsRefreshInterval.floatValue <= 0f)
            {
                fpsRefreshInterval.floatValue = 0.25f;
            }

            if (lowFpsThreshold != null && lowFpsThreshold.intValue <= 0)
            {
                lowFpsThreshold.intValue = 30;
            }

            serializedHud.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
            hudCount++;
        }

        if (hudCount > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"[WolfHudFpsMenu] FPS HUD enabled on {hudCount} WolfHud component(s).");
    }
}
#endif
