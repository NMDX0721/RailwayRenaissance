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
        var crossSection=new Vector2[size];
        for(int y=0;y<size;y++)crossSection[y]=NF5KWheelGeometry.At(y/(float)size);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size,angle=u*Mathf.PI*2;
            var point=crossSection[y];float radius=point.y;
            var position=new Vector3(point.x,radius*Mathf.Cos(angle),radius*Mathf.Sin(angle));
            float fine=Noise(position,260), medium=Noise(position,110), patina=Noise(position,32);
            float grain=Mathf.Repeat(Mathf.Sin(x*12.9898f+y*78.233f)*43758.5453f,1)-.5f;
            bool rolling=radius>.452f&&radius<.465f&&point.x>-.044f&&point.x<.089f;
            float flangeRub=Mathf.SmoothStep(.466f,.477f,radius)*(1-Mathf.SmoothStep(.485f,.490f,radius));
            float polish=rolling?1:tread?flangeRub*.65f:0;
            float crevice=Mathf.Exp(-Mathf.Pow((radius-.175f)/.025f,2))+Mathf.Exp(-Mathf.Pow((radius-.382f)/.016f,2));
            float rust=(.09f+Mathf.Clamp01((patina-.44f)*2)*.34f+Mathf.Clamp01((medium-.52f)*4)*.25f)*(1-polish)*.65f;
            float pits=Mathf.Clamp01((fine-.61f)*5)*(1-polish);
            Color steel=Color.Lerp(new Color(.66f,.68f,.68f),new Color(.74f,.76f,.77f),polish);
            Color surface=Color.Lerp(steel,new Color(.30f,.22f,.15f),rust);
            surface=Color.Lerp(surface,new Color(.28f,.27f,.25f),Mathf.Clamp01(crevice)*Mathf.Clamp01((patina-.48f)*3)*.16f*(1-polish));
            float machining=Mathf.Sin(v*1391+Mathf.Sin(angle*3)*.08f);
            float score=Mathf.Pow(Mathf.Max(0,Mathf.Sin(v*2317)),32)*polish;
            color[y*size+x]=surface*(1+grain*.03f+(fine-.5f)*.035f-score*.045f);
            float metal=Mathf.Lerp(.84f,.98f,polish)*(1-rust*.72f-pits*.20f);
            float smooth=Mathf.Lerp(.34f,.51f,polish)-rust*.12f+(fine-.5f)*.04f-score*.025f;
            mask[y*size+x]=new Color(metal,0,0,smooth);
            var n=new Vector3(grain*.018f,(machining*.004f+grain*.012f)*(1-polish*.5f),1).normalized;
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
    static float Noise(Vector3 p,float frequency)
    {
        p*=frequency;
        return (Mathf.PerlinNoise(20+p.x,20+p.y)+Mathf.PerlinNoise(70+p.y,70+p.z)+Mathf.PerlinNoise(120+p.z,120+p.x))/3;
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
