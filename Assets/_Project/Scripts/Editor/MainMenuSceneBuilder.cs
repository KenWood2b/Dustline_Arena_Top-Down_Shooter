using System.Collections.Generic;
using DustlineArena.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustlineArena.Editor
{
    public static class MainMenuSceneBuilder
    {
        private const string MenuScenePath = "Assets/_Project/Scenes/Dustline_MainMenu.unity";

        [MenuItem("Dustline Arena/Build Main Menu")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.038f, 1f);
            cameraObject.tag = "MainCamera";

            new GameObject("Main_Menu", typeof(MainMenuController));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            EnsureMenuFirstInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: main menu scene built.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static void EnsureMenuFirstInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuScenePath, true)
            };

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == MenuScenePath)
                {
                    continue;
                }

                scenes.Add(existing[i]);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
