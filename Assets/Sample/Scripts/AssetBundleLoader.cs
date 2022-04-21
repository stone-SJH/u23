using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssetBundleLoader : MonoBehaviour
{
    // Start is called before the first frame update
    IEnumerator Start()
    {
        var bundleLoadRequest = AssetBundle.LoadFromFileAsync(Application.dataPath + "/abs/adam");
        yield return bundleLoadRequest;

        var myLoadedAssetBundle = bundleLoadRequest.assetBundle;
        if (myLoadedAssetBundle == null)
        {
            Debug.Log("Failed to load AssetBundle!");
            yield break;
        }

        var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("adamHead");
        yield return assetLoadRequest;

        GameObject prefab = assetLoadRequest.asset as GameObject;
        Instantiate(prefab);

        myLoadedAssetBundle.Unload(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
