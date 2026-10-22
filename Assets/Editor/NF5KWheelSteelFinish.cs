using UnityEditor;
using UnityEngine;

// Separate dielectric oxide patches from the bare steel, without emission.
public static class NF5KWheelSteelFinish
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    public static void Apply()
    {
        Finish("Wheel_Machined_Tread",true);
        Finish("Wheel_Oxidized_Web",false);
    }
    static void Finish(string name,bool tread)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+name+".mat");
        if(material==null)return;
        const int size=512;
        var color=new Color[size*size];var mask=new Color[size*size];var normal=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            float patches=Mathf.PerlinNoise(u*17+11,v*13+7);
            float grain=Mathf.Repeat(Mathf.Sin(x*12.9898f+y*78.233f)*43758.5453f,1)-.5f;
            float machining=Mathf.Sin(v*Mathf.PI*2*139);
            float oxide=Mathf.SmoothStep(0,1,Mathf.Clamp01((patches-.30f)*2.5f));
            // Arc-length UV: rolling band is profile segment 8; remaining rim shoulders collect oxide.
            float contact=tread?Mathf.SmoothStep(.402f,.416f,v)*(1-Mathf.SmoothStep(.495f,.510f,v)):0;
            float scratches=Mathf.Pow(Mathf.Max(0,Mathf.Sin(v*2317+Mathf.Sin(u*37)*.15f)),24);
            float dust=Mathf.PerlinNoise(u*31+4,v*19+31);
            Color steel=new Color(.58f,.60f,.61f);
            Color worn=Color.Lerp(new Color(.30f,.205f,.12f),new Color(.46f,.37f,.25f),dust);
            Color surface=Color.Lerp(steel,worn,tread?(1-contact)*(.45f+oxide*.4f):.48f+oxide*.32f);
            surface=Color.Lerp(surface,new Color(.12f,.115f,.10f),Mathf.Clamp01((dust-.52f)*2.4f)*(1-contact)*.65f);
            color[y*size+x]=surface*(1+grain*.06f+machining*.003f-scratches*.055f);
            float metal=tread?Mathf.Lerp(.48f,.98f,contact):Mathf.Lerp(.65f,.22f,oxide);
            float polish=tread?Mathf.Lerp(.23f,.47f,contact):Mathf.Lerp(.30f,.15f,oxide);
            mask[y*size+x]=new Color(metal,0,0,polish+grain*.03f-scratches*.025f);
            var n=new Vector3(grain*.012f,machining*(tread?.009f:.004f),1).normalized;
            normal[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
        }
        material.SetColor("_BaseColor",Color.white);
        material.SetTexture("_BaseMap",Texture(name+"_Steel_Color",color,false));
        material.SetTexture("_MetallicGlossMap",Texture(name+"_Steel_Mask",mask,true));
        material.SetTexture("_BumpMap",Texture(name+"_Steel_Normal",normal,true));
        material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.SetFloat("_BumpScale",1);
        material.SetFloat("_SmoothnessTextureChannel",0);
        material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");material.EnableKeyword("_NORMALMAP");
        material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
        material.SetTextureScale("_BaseMap",Vector2.one);EditorUtility.SetDirty(material);
    }
    static Texture2D Texture(string name,Color[] colors,bool linear)
    {
        string path=Folder+name+".asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture==null){texture=new Texture2D(512,512,TextureFormat.RGBA32,true,linear){name=name};AssetDatabase.CreateAsset(texture,path);}
        texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=8;
        texture.SetPixels(colors);texture.Apply(true,false);EditorUtility.SetDirty(texture);return texture;
    }
}
