using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class NF5KReflectorReview
{
    [Serializable] class Result {public float lightOff, coaxial, oblique;public bool passiveReturn;}
    public static void BatchBuild()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.ambientProbe=new SphericalHarmonicsL2();
            var camera=new GameObject("Reflector_Test_Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-2);camera.orthographic=true;camera.orthographicSize=.65f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;NF5KLightingRendererSetup.Configure(camera);
            var sample=GameObject.CreatePrimitive(PrimitiveType.Quad);sample.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Ladder_Reflective_White.mat");
            var lamp=new GameObject("External_Lamp").AddComponent<Light>();lamp.type=LightType.Spot;lamp.range=10;lamp.spotAngle=55;lamp.intensity=1.5f;lamp.transform.position=camera.transform.position;
            lamp.enabled=false;float off=Measure(camera);lamp.enabled=true;float near=Measure(camera);
            lamp.transform.position=new Vector3(1.4f,0,-1.4f);lamp.transform.LookAt(Vector3.zero);float angled=Measure(camera);
            var result=new Result{lightOff=off,coaxial=near,oblique=angled,passiveReturn=near>off+.08f&&near>angled+.04f&&off<near*.25f};
            File.WriteAllText("tools/compile/cab_reflector_acceptance.json",JsonUtility.ToJson(result,true));
            if(!result.passiveReturn)throw new InvalidOperationException("Retroreflector test failed: "+JsonUtility.ToJson(result));
            Debug.Log("NF5K_PASSIVE_REFLECTOR_OK: "+JsonUtility.ToJson(result));EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static float Measure(Camera camera)
    {
        var rt=new RenderTexture(96,96,24,RenderTextureFormat.ARGBHalf);var image=new Texture2D(96,96,TextureFormat.RGB24,false);var old=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,96,96),0,0);image.Apply();
            float sum=0;for(int y=40;y<56;y++)for(int x=40;x<56;x++)sum+=image.GetPixel(x,y).grayscale;return sum/256;
        }
        finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
