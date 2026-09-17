using UnityEditor;
using UnityEngine;

public class FlipNormals : EditorWindow
{
    [MenuItem("Tools/Flip Normals (選取物件)")]
    static void Open() => GetWindow<FlipNormals>("Flip Normals");

    void OnGUI()
    {
        GUILayout.Label("先在 Hierarchy 選好房間物件，再按執行", EditorStyles.wordWrappedLabel);
        GUILayout.Space(8);

        if (GUILayout.Button("翻轉法線", GUILayout.Height(36)))
            Apply();
    }

    static void Apply()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;

                // 翻轉所有法線方向
                var normals = mesh.normals;
                for (int i = 0; i < normals.Length; i++)
                    normals[i] = -normals[i];
                mesh.normals = normals;

                // 翻轉三角形繞行順序（讓面朝向也反過來）
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var tris = mesh.GetTriangles(s);
                    for (int i = 0; i < tris.Length; i += 3)
                        (tris[i], tris[i + 2]) = (tris[i + 2], tris[i]);
                    mesh.SetTriangles(tris, s);
                }

                count++;
            }
        }

        EditorUtility.DisplayDialog("完成", $"已翻轉 {count} 個 Mesh 的法線", "OK");
    }
}
