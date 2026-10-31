using System;
using UnityEditor;
using UnityEngine;

public static class NF5KEquipmentPrint
{
    public static void Add(Transform parent,string name,string image,Vector3 position,Vector2 size)
    {
        string texturePath="Assets/Resources/Cab/"+image+".png";
        var importer=AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Generate equipment print first: "+image);
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=true;importer.anisoLevel=8;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
        string materialPath="Assets/Resources/Cab/"+image+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.16f);EditorUtility.SetDirty(material);
        string meshPath="Assets/Models/Cab/NF5KBodyMeshes/"+name+"_Print_V2.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null)
        {
            float x=size.x*.5f,y=size.y*.5f;
            mesh=new Mesh{name=name+"_Printed_Face"};
            mesh.vertices=new[]{new Vector3(x,-y,0),new Vector3(-x,-y,0),new Vector3(x,y,0),new Vector3(-x,y,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
            mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
        }
        else if(Mathf.Abs(mesh.bounds.size.x-size.x)>.00001f||Mathf.Abs(mesh.bounds.size.y-size.y)>.00001f)
        {
            float x=size.x*.5f,y=size.y*.5f;
            mesh.vertices=new[]{new Vector3(x,-y,0),new Vector3(-x,-y,0),new Vector3(x,y,0),new Vector3(-x,y,0)};
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        }
        var face=new GameObject(name);face.layer=30;face.transform.SetParent(parent,false);face.transform.localPosition=position;
        face.AddComponent<MeshFilter>().sharedMesh=mesh;face.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
}
