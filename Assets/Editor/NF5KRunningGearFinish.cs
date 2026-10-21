using UnityEditor;
using UnityEngine;

public static class NF5KRunningGearFinish
{
    const string Folder="Assets/Models/Cab/NF5KBodyMeshes/";
    public static void Apply(Transform root)
    {
        var frame=Paint("Bogie_Cast_Steel",new Color(.46f,.49f,.48f));
        var box=Paint("Bogie_Axlebox_Grey",new Color(.57f,.59f,.56f));
        var equipment=Paint("Bogie_Equipment_Warm_Grey",new Color(.43f,.45f,.42f));
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            string name=renderer.name;
            if(name=="Axlebox"||name=="Axlebox_End_Cover"||name=="Axlebox_Spring_Wing")renderer.sharedMaterial=box;
            else if(name=="Traction_Motor_Envelope"||name=="Motor_End_Shield"||name=="Axle_Gearcase"||name=="Underfloor_Equipment_Cabinet"||name=="Cabinet_Service_Cover")renderer.sharedMaterial=equipment;
            else if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.name=="Bogie_Cast_Steel")renderer.sharedMaterial=frame;
        }
    }
    public static Material Paint(string name,Color color)
    {
        string path="Assets/Resources/Cab/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",.035f);mat.SetFloat("_Smoothness",.26f);
        string texturePath=Folder+"/Bogie_Paint_Mottle.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(texture==null)
        {
            texture=new Texture2D(128,128,TextureFormat.RGBA32,true,true){name="Bogie_Paint_Mottle",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var pixels=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float coarse=Mathf.PerlinNoise(x*.055f,y*.055f),fine=Mathf.PerlinNoise(x*.55f,y*.55f);
                float value=.90f+.07f*coarse+.025f*fine;
                pixels[y*128+x]=new Color(value,value,value,.65f+.30f*fine);
            }
            texture.SetPixels(pixels);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,texturePath);
        }
        mat.SetTexture("_BaseMap",texture);mat.SetTextureScale("_BaseMap",Vector2.one*2);
        mat.SetTexture("_MetallicGlossMap",null);mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_SmoothnessTextureChannel",1);mat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Subtle_Surface_Normal.png"));
        mat.SetFloat("_BumpScale",.10f);mat.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(mat);return mat;
    }
}
