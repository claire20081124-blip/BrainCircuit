using UnityEditor;
using UnityEngine;
using System.IO;

public class FlipNormals : EditorWindow
{
    [MenuItem("Tools/Flip Normals (選取物件)")]
    static void Open() => GetWindow<FlipNormals>("Flip Normals");

    void OnGUI()
    {
        GUILayout.Label("先在 Hierarchy 選好房間物件，再按執行\n改好的 Mesh 會存成 .asset 檔，重開不會跑掉", EditorStyles.wordWrappedLabel);
        GUILayout.Space(8);

        if (GUILayout.Button("翻轉法線並儲存", GUILayout.Height(36)))
            Apply();
    }

    static void Apply()
    {
        string saveFolder = "Assets/_Art/建模/FlippedMeshes";
        if (!AssetDatabase.IsValidFolder(saveFolder))
            AssetDatabase.CreateFolder("Assets/_Art/建模", "FlippedMeshes");

        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                var original = mf.sharedMesh;
                if (original == null) continue;

                // 複製一份，避免改到原始 FBX mesh
                var mesh = Instantiate(original);
                mesh.name = original.name + "_flipped";

                // 翻轉法線
                var normals = mesh.normals;
                for (int i = 0; i < normals.Length; i++)
                    normals[i] = -normals[i];
                mesh.normals = normals;

                // 翻轉三角形繞行順序
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var tris = mesh.GetTriangles(s);
                    for (int i = 0; i < tris.Length; i += 3)
                        (tris[i], tris[i + 2]) = (tris[i + 2], tris[i]);
                    mesh.SetTriangles(tris, s);
                }

                // 存成 .asset 檔
                string assetPath = $"{saveFolder}/{mesh.name}.asset";
                AssetDatabase.CreateAsset(mesh, assetPath);

                // 把 MeshFilter 指向新的 asset
                mf.sharedMesh = mesh;
                EditorUtility.SetDirty(mf);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已翻轉並儲存 {count} 個 Mesh\n存在 Assets/_Art/建模/FlippedMeshes/", "OK");
    }
}
