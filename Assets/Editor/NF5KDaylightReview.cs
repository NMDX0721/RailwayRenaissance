using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using RailwayRenaissance.Cab;

public static class NF5KDaylightReview
{
    public const string ScenePath="Assets/Scenes/CabDaylightReview.unity";
    const string Main="Assets/Scenes/CabInteriorBlockout.unity";
    [Serializable] sealed class Dimensions
    {
        public string units="metres; scene geometry measured; NF5K exterior dimensions remain design estimates";
        public int seatCount;
        public float cushionWidth, seatTop, rowPitch, clearAisle, clearInnerWidth, ceilingClearHeight, driverEyeHeight;
    }
    public static void AuditDimensions()
    {
        var fittings=GameObject.Find("Passenger_Interior_Fittings");
        var renderers=fittings.GetComponentsInChildren<Renderer>();
        var cushions=renderers.Where(r=>r.name=="Passenger_Seat_Cushion").ToArray();
        var rows=cushions.Select(r=>Mathf.Round(r.bounds.center.z*1000)/1000).Distinct().OrderBy(z=>z).ToArray();
        var floor=GameObject.Find("Passenger_Floor_Module_0").GetComponent<Renderer>();
        var ceiling=GameObject.Find("Passenger_Ceiling_Module_0").GetComponent<Renderer>();
        var d=new Dimensions {seatCount=cushions.Length,cushionWidth=cushions[0].bounds.size.x,
            seatTop=cushions[0].bounds.max.y-floor.bounds.max.y,rowPitch=rows[1]-rows[0],
            clearAisle=2*renderers.Where(r=>r.name=="Passenger_Armrest").Min(r=>Mathf.Abs(r.bounds.center.x)-r.bounds.extents.x),
            clearInnerWidth=2*renderers.Where(r=>r.name=="Passenger_Lower_Liner").Min(r=>Mathf.Abs(r.bounds.center.x)-r.bounds.extents.x),
            ceilingClearHeight=ceiling.bounds.min.y-floor.bounds.max.y,driverEyeHeight=Camera.main.transform.position.y};
        if(d.seatCount!=48||Mathf.Abs(d.cushionWidth-.45f)>.001f||Mathf.Abs(d.rowPitch-1.1f)>.002f||d.clearAisle<.65f||d.ceilingClearHeight<2.10f||d.seatTop<.48f||d.seatTop>.56f)
            throw new InvalidOperationException("Cab proportion regression: "+JsonUtility.ToJson(d));
        File.WriteAllText("tools/compile/nf5k_proportions.json",JsonUtility.ToJson(d,true));
        Debug.Log("NF5K_PROPORTIONS_OK: "+JsonUtility.ToJson(d));
    }
    [MenuItem("RailRevival/Cab/Prepare Daylight Review Scene")]
    public static void Prepare()
    {
        EditorSceneManager.OpenScene(Main);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath,true);
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var camera=Camera.main;var sun=GameObject.Find("Interior_Review_Key").GetComponent<Light>();
        GameObject.Find("Industrial_Review_Fill").GetComponent<Light>().enabled=false;
        var ground=GameObject.Find("Review_Ground_Not_Vehicle");ground.transform.localScale=new Vector3(240,.04f,240);
        const string skyPath="Assets/Settings/URP/NF5KDaySky.mat";
        var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if(sky==null){var shader=Shader.Find("Skybox/Procedural");if(shader==null)throw new InvalidOperationException("Procedural sky shader missing");sky=new Material(shader);AssetDatabase.CreateAsset(sky,skyPath);}
        sky.SetFloat("_SunDisk",2);sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",1);
        sky.SetColor("_SkyTint",new Color(.50f,.50f,.50f));sky.SetColor("_GroundColor",new Color(.36f,.35f,.34f));sky.SetFloat("_Exposure",1);
        EditorUtility.SetDirty(sky);
        var environment=new GameObject("Daylight_Environment").AddComponent<CabDaylightEnvironment>();
        environment.Configure(sun,camera,sky);
        environment.SetView(0);
        UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>().AllOff();
        UnityEngine.Object.FindFirstObjectByType<CabExteriorLighting>().SetMode(ExteriorLightMode.Off);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
    }
    public static void Render(Action<Camera,string> shot)
    {
        Prepare();
        var camera=Camera.main;var environment=UnityEngine.Object.FindFirstObjectByType<CabDaylightEnvironment>();
        if(Mathf.Abs(camera.fieldOfView-65)>.01f)throw new InvalidOperationException("Saved daylight driver view must use 65 degrees");
        environment.SetView(2);if(Mathf.Abs(camera.fieldOfView-55)>.01f)throw new InvalidOperationException("Exterior view setup failed");
        var interior=UnityEngine.Object.FindFirstObjectByType<CabInteriorLighting>();
        foreach(CabDaylightEnvironment.DayPreset preset in Enum.GetValues(typeof(CabDaylightEnvironment.DayPreset)))
        {
            environment.Apply(preset);interior.AllOff();
            environment.SetView(1);
            shot(camera,"tools/cab_reference/interior_review/day_"+preset+"_lamps_off.png");
            interior.SetPassengerZone(0,true);interior.SetPassengerZone(1,true);
            shot(camera,"tools/cab_reference/interior_review/day_"+preset+"_lamps_on.png");
        }
        environment.Apply(CabDaylightEnvironment.DayPreset.Clear);
        environment.SetView(0);
        interior.AllOff();shot(camera,"tools/cab_reference/interior_review/day_driver.png");
        EditorSceneManager.OpenScene(Main);
        Debug.Log("NF5K_DAYLIGHT_REVIEW_OK: saved independent daylight scene; clear/overcast/low-sun lamp-off/on pairs rendered; main scene restored");
    }
}
