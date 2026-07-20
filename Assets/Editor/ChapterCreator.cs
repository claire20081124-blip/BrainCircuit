using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RunLight.UI;

namespace RunLight.Editor
{
    /// <summary>
    /// 一鍵建立 Chapter1 / Chapter2 場景並加入 Build Settings。
    /// 選單位置：腦迴路 ▸ 建立章節場景
    /// </summary>
    public static class ChapterCreator
    {
        private const string Chapter1Path = "Assets/Scenes/Chapter1.unity";
        private const string Chapter2Path = "Assets/Scenes/Chapter2.unity";

        [MenuItem("腦迴路/建立章節場景（Chapter1 + Chapter2）")]
        public static void CreateChapters()
        {
            var dir = "Assets/Scenes";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            CreateChapterScene(Chapter1Path, nextScene: "Chapter2");
            CreateChapterScene(Chapter2Path, nextScene: "SampleScene");

            // 加入 Build Settings（排在 Prologue 後面）
            AddToBuildSettings(Chapter1Path, Chapter2Path);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "建立完成",
                "Chapter1 和 Chapter2 已建立並加入 Build Settings。\n\n" +
                "流程：Prologue → Chapter1 → Chapter2 → SampleScene\n\n" +
                "在各場景的 PrologueController → Inspector 設定影片與對話內容。",
                "OK");
        }

        private static void CreateChapterScene(string path, string nextScene)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);

            var go = new GameObject("PrologueController");
            var prologue = go.AddComponent<Prologue>();

            // 設定下一個場景名稱
            var so = new SerializedObject(prologue);
            var prop = so.FindProperty("nextSceneName");
            if (prop != null)
            {
                prop.stringValue = nextScene;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[腦迴路] 場景已建立：{path}（下一關：{nextScene}）");
        }

        private static void AddToBuildSettings(string ch1, string ch2)
        {
            var list = EditorBuildSettings.scenes.ToList();

            foreach (var path in new[] { ch1, ch2 })
            {
                if (list.All(s => s.path != path))
                    list.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        [MenuItem("腦迴路/建立章節場景（Chapter1 + Chapter2）", validate = true)]
        public static bool Validate() => true;
    }
}
