using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KExteriorLightControlSetup
{
    public static void Build(Transform fittings,Transform body)
    {
        var source=body.GetComponentsInChildren<MeshFilter>().Single(t=>t.name=="Auxiliary_Control_Plate");
        var size=source.sharedMesh.bounds.size;int thin=size.x<size.y?(size.x<size.z?0:2):(size.y<size.z?1:2);
        var normal=source.transform.TransformVector(thin==0?Vector3.right:thin==1?Vector3.up:Vector3.forward).normalized;if(normal.z<0)normal=-normal;
        var up=Vector3.ProjectOnPlane(Vector3.up,normal).normalized;
        var root=new GameObject("NF5K_Exterior_Light_Control_Panel").transform;root.SetParent(fittings,false);
        root.SetPositionAndRotation(source.transform.position-up*.050f+normal*.00425f,Quaternion.LookRotation(normal,up));
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Satin_Steel.mat");
        var grip=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Selector_Graphite_V5.mat");
        var ivory=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Selector_Ivory_Index_V5.mat");
        var cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Circulator_Smooth_Cylinder_V3.asset");
        var screws=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset");
        var slot=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Fastener_Recess_V4.mat");
        GameObject Part(string name,Transform parent,Vector3 p,Vector3 dimensions,Material material,bool round=false)
        {
            var g=GameObject.CreatePrimitive(round?PrimitiveType.Cylinder:PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=dimensions;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;
            if(round){g.GetComponent<MeshFilter>().sharedMesh=cylinder;g.transform.localRotation=Quaternion.Euler(90,0,0);}return g;
        }
        Part("Exterior_Control_Thin_Mounting_Plate",root,Vector3.zero,new Vector3(.320f,.094f,.0025f),source.GetComponent<Renderer>().sharedMaterial);
        NF5KEquipmentPrint.Add(root,"Exterior_Control_Bilingual_Face","Cab_Exterior_Control_Face_V1",new Vector3(0,0,.00136f),new Vector2(.320f,.094f));
        var print=root.Find("Exterior_Control_Bilingual_Face").GetComponent<Renderer>();NF5KLightingKeysSetup.ConfigureInk(print.sharedMaterial);print.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;print.receiveShadows=false;
        foreach(float x in new[]{-.150f,.150f})foreach(float y in new[]{-.037f,.037f})
        {
            var screw=new GameObject("Exterior_Control_Captive_Screw");screw.layer=30;screw.transform.SetParent(root,false);screw.transform.localPosition=new Vector3(x,y,.0014f);screw.transform.localRotation=Quaternion.FromToRotation(Vector3.up,Vector3.forward);screw.transform.localScale=new Vector3(.005f,.002f,.005f);
            screw.AddComponent<MeshFilter>().sharedMesh=screws;screw.AddComponent<MeshRenderer>().sharedMaterials=new[]{steel,slot};
        }
        foreach(bool marker in new[]{false,true})
        {
            var target=new GameObject(marker?"Exterior_Operating_Mode_Control":"Headlight_Level_Control").transform;target.SetParent(root,false);target.localPosition=new Vector3(marker?-.078f:.078f,-.004f,.0015f);target.gameObject.layer=30;
            Part("Selector_Threaded_Collar",target,Vector3.forward*.001f,new Vector3(.035f,.001f,.035f),steel,true);
            var pivot=new GameObject("Exterior_Three_Detent_Selector").transform;pivot.SetParent(target,false);
            Part("Selector_Round_Base",pivot,Vector3.forward*.004f,new Vector3(.032f,.002f,.032f),grip,true);
            Part("Selector_Lever",pivot,new Vector3(0,0,.009f),new Vector3(.010f,.030f,.010f),grip);
            Part("Selector_Ivory_Index",pivot,new Vector3(0,.009f,.0141f),new Vector3(.002f,.007f,.0003f),ivory);
            var hit=target.gameObject.AddComponent<BoxCollider>();hit.size=new Vector3(.041f,.045f,.026f);hit.center=Vector3.forward*.010f;
            target.gameObject.AddComponent<CabExteriorLightControl>().Configure(marker,pivot);
        }
        foreach(var r in body.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Future_Radio_Location"||r.name=="Auxiliary_Plate_Title"))r.enabled=false;
        var oldStand=body.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Speedometer_Stand");
        var standBounds=oldStand.bounds;oldStand.enabled=false;
        var support=new GameObject("Speedometer_Folded_Metal_Bracket").transform;support.SetParent(fittings,false);support.position=standBounds.center;
        float width=standBounds.size.x, height=standBounds.size.y, depth=standBounds.size.z;
        // Preserve both mounting contact planes; open the centre instead of retaining a solid block.
        Part("Speedometer_Bracket_Foot",support,new Vector3(0,-height*.5f+.002f,0),new Vector3(width,.004f,depth),oldStand.sharedMaterial);
        Part("Speedometer_Bracket_Upper_Saddle",support,new Vector3(0,height*.5f-.002f,0),new Vector3(width*.86f,.004f,depth*.85f),oldStand.sharedMaterial);
        foreach(float side in new[]{-1f,1f})
        {
            Part("Speedometer_Bracket_Web",support,new Vector3(side*width*.32f,0,0),new Vector3(.004f,height-.008f,depth*.72f),oldStand.sharedMaterial);
            foreach(float z in new[]{-depth*.32f,depth*.32f})
            {
                var fastener=new GameObject("Speedometer_Bracket_Foot_Screw");fastener.layer=30;fastener.transform.SetParent(support,false);
                fastener.transform.localPosition=new Vector3(side*width*.40f,-height*.5f+.0041f,z);fastener.transform.localScale=new Vector3(.006f,.002f,.006f);
                fastener.AddComponent<MeshFilter>().sharedMesh=screws;fastener.AddComponent<MeshRenderer>().sharedMaterials=new[]{steel,slot};
            }
        }
        Debug.Log($"NF5K_SPEEDOMETER_BRACKET_BUILT: envelope {width:F4} x {height:F4} x {depth:F4} m; original contact planes retained, four seated screws");
        var lighting=body.GetComponentInChildren<CabExteriorLighting>();if(lighting==null)throw new InvalidOperationException("Exterior light controller missing");lighting.SetReviewControls(false);
        Debug.Log("NF5K_EXTERIOR_CONTROL_BUILT: one cab, existing auxiliary panel, headlight level and six-position operating profile selectors");
    }
}
