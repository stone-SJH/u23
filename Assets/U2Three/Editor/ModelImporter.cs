using System.Collections;
using System.Collections.Generic;
using GLTFImporter;
using UnityEditor;
using UnityEngine;

public class ModelImporter : EditorWindow
{
    [MenuItem("Tools/Export Models for ThreeJS")]
    static void Init()
    {
        ModelImporter window = (ModelImporter) EditorWindow.GetWindow(typeof(ModelImporter));
    }

    void OnGUI()
    {
        if (GUILayout.Button("import"))
        {
            GameObject model = Importer.LoadFromFile(Application.dataPath + "/Sample/third-party/adamHead/adamHead.gltf");
            GameObject.Instantiate(model);
        }
    }
}
