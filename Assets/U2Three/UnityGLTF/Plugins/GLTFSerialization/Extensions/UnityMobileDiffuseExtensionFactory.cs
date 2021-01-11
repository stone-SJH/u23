using GLTF.Extensions;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
    public class UnityMobileDiffuseExtensionFactory : ExtensionFactory
    {
        public const string EXTENSION_NAME = "Unity_materials_mobileDiffuse";
        public const string MAIN_TEXTURE = "mainTex";

        public UnityMobileDiffuseExtensionFactory()
        {
            ExtensionName = EXTENSION_NAME;
        }

        public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            TextureInfo mainTexInfo = new TextureInfo();

            if (extensionToken != null)
            {
#if DEBUG
                // Broken on il2cpp. Don't ship debug DLLs there.
                System.Diagnostics.Debug.WriteLine(extensionToken.Value.ToString());
                System.Diagnostics.Debug.WriteLine(extensionToken.Value.Type);
#endif
                mainTexInfo = extensionToken.Value[MAIN_TEXTURE].DeserializeAsTexture(root);
            }

            return new UnityMobileDiffuseExtension(mainTexInfo);
        }
    }
}