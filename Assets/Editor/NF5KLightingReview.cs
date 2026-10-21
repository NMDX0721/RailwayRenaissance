using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KLightingReview
{
    public static void Validate(CabExteriorLighting controller)
    {
        if(controller==null||controller.Beams.Length!=5)throw new InvalidOperationException("Lighting binding count invalid");
        int[] expected={0,2,5,5,2};
        for(int i=0;i<5;i++)
        {
            controller.SetMode((ExteriorLightMode)i);int count=0;
            foreach(var beam in controller.Beams)if(beam.enabled&&beam.intensity>0)count++;
            if(count!=expected[i])throw new InvalidOperationException("Unexpected active lamp count in mode "+i);
            if(i==4)foreach(var beam in controller.Beams)if(beam.enabled&&beam.color.g>.1f)throw new InvalidOperationException("Tail lamps must be red");
        }
        controller.SetMode(ExteriorLightMode.Bright);float bright=controller.Beams[0].intensity;
        controller.SetMode(ExteriorLightMode.Dim);if(controller.Beams[0].intensity>=bright)throw new InvalidOperationException("Dim and bright are not distinct");
        controller.SetPowerAvailable(false);
        foreach(var beam in controller.Beams)if(beam.enabled)throw new InvalidOperationException("Light survives power isolation");
        controller.SetPowerAvailable(true);controller.SetMode(ExteriorLightMode.Off);
        Debug.Log("NF5K_LIGHTING_MODES_OK: Off/Marker/Dim/Bright/Tail counts, color, brightness and power isolation");
    }
    public static void Render(Camera camera,Action<Camera,string> shot)
    {
        var controller=UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>();Validate(controller);
        var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
        var background=camera.backgroundColor;var ambient=RenderSettings.ambientMode;
        var sky=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;var bottom=RenderSettings.ambientGroundColor;
        var key=GameObject.Find("Interior_Review_Key").GetComponent<Light>();var fill=GameObject.Find("Industrial_Review_Fill").GetComponent<Light>();
        float keyIntensity=key.intensity,fillIntensity=fill.intensity;
        var ground=GameObject.Find("Review_Ground_Not_Vehicle").transform;var groundPos=ground.position;var groundSize=ground.localScale;
        try
        {
            key.intensity=.055f;fill.intensity=.015f;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.018f,.025f,.04f);RenderSettings.ambientEquatorColor=new Color(.008f,.012f,.02f);RenderSettings.ambientGroundColor=new Color(.004f,.005f,.008f);
            camera.backgroundColor=new Color(.018f,.025f,.04f);camera.transform.position=new Vector3(7,5,-11);
            camera.transform.LookAt(new Vector3(0,.2f,-2));camera.orthographicSize=4.7f;
            ground.position=new Vector3(0,-1.39f,-12);ground.localScale=new Vector3(24,.04f,65);
            foreach(ExteriorLightMode mode in Enum.GetValues(typeof(ExteriorLightMode)))
            {
                controller.SetMode(mode);shot(camera,"tools/cab_reference/interior_review/lights_"+mode.ToString().ToLowerInvariant()+".png");
            }
            // Compare road-only crops, excluding lamps and body, to prove illumination beyond emissive surfaces.
            float off=RoadBrightness("off"),bright=RoadBrightness("bright");
            if(bright<=off+.002f)throw new InvalidOperationException("No measurable headlamp illumination on the review ground: "+off+" / "+bright);
            var lampPixel=camera.WorldToViewportPoint(new Vector3(0,2.43f,-.80f));
            float lampOff=LampBrightness("off",lampPixel),lampOn=LampBrightness("bright",lampPixel);
            if(lampOn<lampOff+.1f)throw new InvalidOperationException("Lamp surface is not visibly emissive: "+lampOff+" / "+lampOn);
            Debug.Log("NF5K_LIGHTING_EMISSION_RENDER_OK: off="+lampOff+", bright="+lampOn);
            Debug.Log("NF5K_LIGHTING_GROUND_RENDER_OK: off="+off+", bright="+bright);
        }
        finally
        {
            controller.SetMode(ExteriorLightMode.Off);camera.transform.position=position;camera.transform.rotation=rotation;camera.orthographicSize=size;camera.backgroundColor=background;
            RenderSettings.ambientMode=ambient;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=bottom;
            key.intensity=keyIntensity;fill.intensity=fillIntensity;ground.position=groundPos;ground.localScale=groundSize;
        }
    }
    static float LampBrightness(string mode,Vector3 uv)
    {
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("tools/cab_reference/interior_review/lights_"+mode+".png"));
        try{return image.GetPixel(Mathf.RoundToInt(uv.x*image.width),Mathf.RoundToInt(uv.y*image.height)).grayscale;}
        finally{UnityEngine.Object.DestroyImmediate(image);}
    }
    static float RoadBrightness(string mode)
    {
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("tools/cab_reference/interior_review/lights_"+mode+".png"));
        try
        {
            float sum=0;int count=0;
            for(int y=40;y<330;y+=5)for(int x=260;x<1050;x+=5){sum+=image.GetPixel(x,y).grayscale;count++;}
            return sum/count;
        }
        finally{UnityEngine.Object.DestroyImmediate(image);}
    }
}

[InitializeOnLoad]
public static class NF5KLightingPlayReview
{
    const string Pending="NF5KLightingReviewPending", Failed="NF5KLightingReviewFailed";
    static int frames;
    static NF5KLightingPlayReview(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Begin(){SessionState.SetBool(Pending,true);SessionState.SetBool(Failed,false);EditorApplication.isPlaying=true;}
    static void Changed(PlayModeStateChange change)
    {
        if(!SessionState.GetBool(Pending,false))return;
        if(change==PlayModeStateChange.EnteredPlayMode){frames=0;EditorApplication.update+=Check;}
        if(change==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Pending,false);EditorApplication.Exit(SessionState.GetBool(Failed,false)?1:0);}
    }
    static void Check()
    {
        try
        {
            if(++frames==10)
            {
                var controller=UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>();
                NF5KInteriorLightingReview.Validate(UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>());
                NF5KLightingReview.Validate(controller);controller.SetMode(ExteriorLightMode.Bright);
                var interior=UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>();interior.SetInstrumentDimmer(.37f);
                UnityEngine.Object.FindFirstObjectByType<CabEngineState>().InstrumentLightOn=false;
                UnityEngine.Object.FindFirstObjectByType<CabEngineState>().HeadlightOn=false;
            }
            if(frames==25)
            {
                var controller=UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>();
                if(controller.Mode!=ExteriorLightMode.Off)throw new InvalidOperationException("Legacy headlight switch did not disable runtime lighting");
                if(UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>().InstrumentDimmer!=0)throw new InvalidOperationException("Legacy instrument switch did not turn lighting off");
                UnityEngine.Object.FindFirstObjectByType<CabEngineState>().InstrumentLightOn=true;
                UnityEngine.Object.FindFirstObjectByType<CabEngineState>().HeadlightOn=true;
            }
            if(frames>=40)
            {
                var controller=UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>();
                if(controller.Mode!=ExteriorLightMode.Bright)throw new InvalidOperationException("Legacy switch did not restore selected mode");
                controller.enabled=false;foreach(var beam in controller.Beams)if(beam.enabled)throw new InvalidOperationException("Disabled controller left lights on");
                var interior=UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>();
                if(!Mathf.Approximately(interior.InstrumentDimmer,.37f))throw new InvalidOperationException("Legacy instrument switch did not restore dimmer");
                interior.SetSupply(false,true);interior.enabled=false;
                foreach(var fixture in interior.Fixtures)if(fixture.light.enabled||(fixture.bounce!=null&&fixture.bounce.enabled))throw new InvalidOperationException("Disabled interior controller left emergency lamps energized");
                interior.enabled=true;interior.SetSupply(true,true);interior.SetCabCeiling(true);interior.SetPassengerZone(0,true);interior.SetPassengerZone(1,true);
                int lowerCount=0;foreach(var fixture in interior.Fixtures)if(fixture.lowerBounce!=null&&fixture.lowerBounce.enabled)lowerCount++;
                if(lowerCount!=8)throw new InvalidOperationException("Lower cabin fill did not energize all eight bindings");
                interior.enabled=false;
                foreach(var fixture in interior.Fixtures)if(fixture.lowerBounce!=null&&fixture.lowerBounce.enabled)throw new InvalidOperationException("Disabled interior controller left floor fill energized");
                Debug.Log("NF5K_INTERIOR_PLAYMODE_OK: circuit/power tests, legacy instrument switch and remembered dimmer, emergency and eight floor-fill disable cleanup");
                Debug.Log("NF5K_LIGHTING_PLAYMODE_OK: actual runtime mode API, power isolation, legacy switch off/on, disable cleanup");
                EditorApplication.update-=Check;EditorApplication.isPlaying=false;
            }
        }
        catch(Exception e){Debug.LogException(e);SessionState.SetBool(Failed,true);EditorApplication.update-=Check;EditorApplication.isPlaying=false;}
    }
}
