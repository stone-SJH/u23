using System.Collections;
using System.Collections.Generic;
using GLTFImporter;
using UnityEditor;
using UnityEngine;

public class AssetBundleBuilder : EditorWindow
{
    [MenuItem("Tools/Build AssetBundle")]
    static void Init()
    {
        AssetBundleBuilder window = (AssetBundleBuilder) EditorWindow.GetWindow(typeof(AssetBundleBuilder));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Build"))
        {
            BuildPipeline.BuildAssetBundles(Application.dataPath + "/abs", BuildAssetBundleOptions.None,
                BuildTarget.StandaloneOSX);
        }
    }
}
