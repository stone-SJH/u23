using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GLTF.Schema
{
    public class UnityMobileDiffuseExtension : IExtension
    {
        public TextureInfo MainTex;

        public UnityMobileDiffuseExtension(TextureInfo mainTex)
        {
            MainTex = mainTex;
        }

        public IExtension Clone(GLTFRoot gltfRoot)
        {
            return new UnityMobileDiffuseExtension(new TextureInfo(MainTex, gltfRoot));
        }

        public JProperty Serialize()
        {
            Debug.Log(MainTex.Index.Id);
            JProperty jProperty = new JProperty(UnityMobileDiffuseExtensionFactory.EXTENSION_NAME,
                new JObject(
                    new JProperty(UnityMobileDiffuseExtensionFactory.MAIN_TEXTURE,
                        new JObject(
                            new JProperty(TextureInfo.INDEX, MainTex.Index.Id)
                        )
                    )
                )
            );
            return jProperty;
        }
    }
}