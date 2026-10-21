using System;
using System.IO;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KInteriorLightingReview
{
    public static void Validate(CabInteriorLighting c)
    {
        if(c==null||c.Fixtures.Length!=14)throw new InvalidOperationException("Interior fixture bindings incomplete");
        c.SetSupply(true,true);c.AllOff();Expect(c,0);
        c.SetCabCeiling(true);Expect(c,1);c.SetTask(true);Expect(c,2);c.SetInstrumentDimmer(.25f);Expect(c,4);
        c.SetPassengerZone(0,true);Expect(c,7);c.SetPassengerZone(1,true);Expect(c,10);
        c.SetSupply(false,true);Expect(c,4);
        foreach(var f in c.Fixtures)if(f.light.enabled&&f.circuit!=InteriorLightCircuit.Emergency)throw new InvalidOperationException("Ordinary circuit energized during outage");
        c.SetSupply(false,false);Expect(c,0);c.SetSupply(true,true);Expect(c,10);
        c.AllOff();c.SetInstrumentDimmer(2);if(c.InstrumentDimmer!=1)throw new InvalidOperationException("Instrument dimmer not clamped");
        c.SetInstrumentDimmer(-1);Expect(c,0);
        var ext=UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>();var previous=ext.Mode;ext.SetMode(ExteriorLightMode.Marker);
        c.SetCabCeiling(true);if(ext.Mode!=ExteriorLightMode.Marker)throw new InvalidOperationException("Interior switch changed exterior lighting");
        c.AllOff();ext.SetMode(previous);
        foreach(var f in c.Fixtures)
        {
            if(f.circuit!=InteriorLightCircuit.CabCeiling&&f.circuit!=InteriorLightCircuit.Task)continue;
            var glassPoint=new Vector3(.55f,1.65f,-.58f);var delta=glassPoint-f.light.transform.position;
            if(delta.magnitude<f.light.range && Vector3.Angle(f.light.transform.forward,delta)<f.light.spotAngle/2)
                throw new InvalidOperationException("Cab light directly illuminates upper forward observation area");
        }
        Debug.Log("NF5K_INTERIOR_LIGHTING_STATE_OK: independent circuits, dimmer clamp, emergency transfer, total blackout, power restoration and forward-cone check");
    }
    static void Expect(CabInteriorLighting c,int count)
    {
        int active=0;foreach(var f in c.Fixtures)if(f.light.enabled&&f.light.intensity>0)active++;
        if(active!=count)throw new InvalidOperationException("Interior active fixture count: "+active+" expected "+count);
    }
    public static void Render(Camera camera,Action<Camera,string> shot)
    {
        var c=UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>();Validate(c);
        var exterior=UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>();
        var p=camera.transform.position;var rotation=camera.transform.rotation;bool ortho=camera.orthographic;float fov=camera.fieldOfView;var bg=camera.backgroundColor;
        var key=GameObject.Find("Interior_Review_Key").GetComponent<Light>();var fill=GameObject.Find("Industrial_Review_Fill").GetComponent<Light>();float keyValue=key.intensity,fillValue=fill.intensity;
        var sky=RenderSettings.ambientSkyColor;var eq=RenderSettings.ambientEquatorColor;var bottom=RenderSettings.ambientGroundColor;
        try
        {
            key.intensity=.008f;fill.intensity=0;RenderSettings.ambientSkyColor=new Color(.006f,.009f,.014f);RenderSettings.ambientEquatorColor=new Color(.003f,.004f,.006f);RenderSettings.ambientGroundColor=Color.black;
            camera.backgroundColor=new Color(.007f,.011f,.018f);camera.orthographic=false;camera.fieldOfView=72;
            camera.transform.position=new Vector3(.65f,1.38f,.78f);camera.transform.LookAt(new Vector3(.60f,1.02f,-.40f));
            exterior.SetMode(ExteriorLightMode.Dim);
            Capture("driver_interior_off");
            c.SetInstrumentDimmer(.25f);Capture("driver_night_instruments");
            c.SetInstrumentDimmer(1);Capture("driver_instruments_full");
            c.SetInstrumentDimmer(.25f);c.SetTask(true);Capture("driver_task_light");
            c.SetCabCeiling(true);Capture("driver_cab_service");
            c.AllOff();exterior.SetMode(ExteriorLightMode.Off);
            camera.transform.position=new Vector3(0,1.58f,2.18f);camera.transform.LookAt(new Vector3(0,1.28f,11));
            Capture("passenger_lights_off");c.SetPassengerZone(0,true);c.SetPassengerZone(1,true);Capture("passenger_lights_on");
            c.SetSupply(false,true);Capture("passenger_emergency");c.SetSupply(false,false);Capture("passenger_blackout");
            float off=Brightness("passenger_lights_off"),on=Brightness("passenger_lights_on"),emergency=Brightness("passenger_emergency"),blackout=Brightness("passenger_blackout");
            if(on<off+.008f||emergency<=blackout+.002f||on<=emergency)throw new InvalidOperationException("Passenger illumination render contrast invalid: "+off+" / "+on+" / "+emergency+" / "+blackout);
            float low=Brightness("driver_night_instruments"),high=Brightness("driver_instruments_full");
            if(high<=low+.001f)throw new InvalidOperationException("Instrument dimmer does not measurably illuminate driver panel: "+low+" / "+high);
            Debug.Log("NF5K_INTERIOR_RENDER_OK: passenger off/on/emergency/blackout="+off+"/"+on+"/"+emergency+"/"+blackout+"; instrument low/high="+low+"/"+high);
        }
        finally
        {
            c.SetSupply(true,true);c.AllOff();exterior.SetMode(ExteriorLightMode.Off);
            key.intensity=keyValue;fill.intensity=fillValue;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=eq;RenderSettings.ambientGroundColor=bottom;
            camera.transform.position=p;camera.transform.rotation=rotation;camera.orthographic=ortho;camera.fieldOfView=fov;camera.backgroundColor=bg;
        }
        void Capture(string name){shot(camera,"tools/cab_reference/interior_review/"+name+".png");}
    }
    static float Brightness(string name)
    {
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("tools/cab_reference/interior_review/"+name+".png"));
        try{float sum=0;int count=0;for(int y=30;y<600;y+=8)for(int x=150;x<1250;x+=8){sum+=image.GetPixel(x,y).grayscale;count++;}return sum/count;}
        finally{UnityEngine.Object.DestroyImmediate(image);}
    }
}
