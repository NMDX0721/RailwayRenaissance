using UnityEngine;

namespace RailwayRenaissance.Cab
{
    [ExecuteAlways, RequireComponent(typeof(TextMesh))]
    public sealed class CabWorldLabel : MonoBehaviour
    {
        MaterialPropertyBlock properties;
        void OnEnable() { Font.textureRebuilt += AtlasChanged; Refresh(); }
        void OnDisable() { Font.textureRebuilt -= AtlasChanged; }
        void AtlasChanged(Font font) { if (GetComponent<TextMesh>().font == font) Refresh(); }
        void Refresh()
        {
            var text = GetComponent<TextMesh>();
            if (text.font == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            var renderer = GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex", text.font.material.mainTexture);
            renderer.SetPropertyBlock(properties);
        }
    }
}
