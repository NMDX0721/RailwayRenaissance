using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KVigilanceIndicatorSetup
{
    public static void Apply(CabEngineState state)
    {
        var panel=state.GetComponentInChildren<CabAnnunciatorPanel>();var v=state.GetComponentInChildren<CabVigilance>();
        if(panel==null||v==null)throw new InvalidOperationException("Existing annunciator and vigilance pedal required");
        var old=panel.transform.Find("Vigilance_Indicator_Module");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        if(panel.transform.Find("Vigilance_Expanded_Layout")==null)
        {
            foreach(Transform child in panel.transform)
                if(child.name.StartsWith("Annunciator_Seal_")||child.name.StartsWith("Annunciator_Rim_")||child.name.StartsWith("Annunciator_Recess_")||child.name.StartsWith("Annunciator_Smoked_")||child.name.StartsWith("Annunciator_Optical_")||child.name.StartsWith("Annunciator_Legend_"))child.localPosition+=Vector3.right*.0355f;
            var back=panel.transform.Find("Annunciator_Seated_Backplate");back.localScale=new Vector3(.441f,.086f,.0025f);
            foreach(Transform child in panel.transform)
            {
                if(child.name=="Annunciator_Captive_Screw")child.localPosition=new Vector3(child.localPosition.x<0?-.2125f:.2125f,child.localPosition.y,child.localPosition.z);
                if(child.name=="Annunciator_Lamp_Test_Button"||child.name=="Annunciator_Test_Label")child.localPosition+=Vector3.left*.071f;
            }
            new GameObject("Vigilance_Expanded_Layout").transform.SetParent(panel.transform,false);
        }
        foreach(string name in new[]{"Annunciator_Lamp_Test_Button","Annunciator_Test_Label"})
        {var item=panel.transform.Find(name);if(item!=null)item.localPosition=new Vector3(-.246f,item.localPosition.y,item.localPosition.z);}
        var unified=panel.transform.Find("Annunciator_Seated_Backplate");unified.localPosition=new Vector3(-.0275f,0,0);unified.localScale=new Vector3(.496f,.086f,.0025f);
        foreach(Transform child in panel.transform)
            if(child.name=="Annunciator_Captive_Screw")child.localPosition=new Vector3(child.localPosition.x<0?-.2675f:.2125f,child.localPosition.y,child.localPosition.z);
        var root=new GameObject("Vigilance_Indicator_Module").transform;root.SetParent(panel.transform,false);root.localPosition=new Vector3(-.1775f,.020f,0);
        Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/"+n+".mat");
        void Box(string name,Vector3 p,Vector3 size,Material m)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        Box("Vigilance_Indicator_Housing",Vector3.zero,new Vector3(.066f,.035f,.003f),M("Cab_Annunciator_Frame"));
        Box("Vigilance_Reserved_Blank",new Vector3(0,-.040f,0),new Vector3(.064f,.033f,.002f),M("Cab_Annunciator_Frame"));
        Box("Vigilance_Indicator_Seal",new Vector3(0,0,.002f),new Vector3(.066f,.029f,.002f),M("Cab_Annunciator_Gasket"));
        foreach(float side in new[]{-1f,1f})
        {
            Box("Vigilance_Indicator_Rim_Side_"+side,new Vector3(side*.03075f,0,.0045f),new Vector3(.0025f,.028f,.004f),M("Cab_Annunciator_Rim"));
            Box("Vigilance_Indicator_Rim_Rail_"+side,new Vector3(0,side*.01525f,.0045f),new Vector3(.064f,.0025f,.004f),M("Cab_Annunciator_Rim"));
        }
        NF5KEquipmentPrint.Add(root,"Vigilance_Optical_Face","Cab_Annunciator_Frost_V2",new Vector3(0,0,.00562f),new Vector2(.059f,.028f));
        var lens=root.Find("Vigilance_Optical_Face").GetComponent<Renderer>();lens.sharedMaterial=M("Cab_Annunciator_Diffuser_V2");
        NF5KEquipmentPrint.Add(root,"Vigilance_Indicator_Legend","Cab_Vigilance_Legend_V1",new Vector3(0,0,.00585f),new Vector2(.057f,.026f));
        var label=root.Find("Vigilance_Indicator_Legend").GetComponent<Renderer>();NF5KLightingKeysSetup.ConfigureInk(label.sharedMaterial);label.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;label.receiveShadows=false;
        root.gameObject.AddComponent<CabVigilanceIndicator>().Configure(v,panel,lens,label);
    }
    public static void Batch()
    {
        try
        {
            foreach(string n in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+n+".unity");var state=UnityEngine.Object.FindAnyObjectByType<CabEngineState>();
                Apply(state);Apply(state);NF5KVigilanceIndicatorReview.Geometry(state);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");
            UnityEngine.Object.FindAnyObjectByType<CabDaylightEnvironment>().Apply(CabDaylightEnvironment.DayPreset.Overcast);NF5KBogieDetailReview.ConfigureInspectionLighting();
            var lamp=UnityEngine.Object.FindAnyObjectByType<CabVigilanceIndicator>();var cam=Camera.main;
            var center=lamp.transform.parent.TransformPoint(new Vector3(-.0275f,0,0));
            cam.fieldOfView=45;cam.transform.position=center+lamp.transform.forward*.74f+lamp.transform.up*.06f;cam.transform.LookAt(center,lamp.transform.up);
            NF5KBogieDetailReview.Capture(cam,"tools/cab_reference/interior_review/vigilance_indicator_v5_off.png");
            EditorSceneManager.OpenScene("Assets/Scenes/CabRailMotionReview.unity");NF5KSimulationPlayReview.Begin();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
