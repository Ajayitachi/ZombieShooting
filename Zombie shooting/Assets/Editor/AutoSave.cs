using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoSave
{
    private static double saveInterval = 300; // 5 minutes (in seconds)
    private static double lastSaveTime;

    static AutoSave()
    {
        EditorApplication.update += OnEditorUpdate;
        lastSaveTime = EditorApplication.timeSinceStartup;
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.timeSinceStartup - lastSaveTime > saveInterval)
        {
            SaveScene();
            lastSaveTime = EditorApplication.timeSinceStartup;
        }
    }

    private static void SaveScene()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Auto-saving scene at " + System.DateTime.Now);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isDirty) // save only if changed
            {
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets(); // save project assets too
        }
    }
}
