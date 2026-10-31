using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RailwayRenaissance.Cab;

public static class NF5KCabCeilingDetail
{
    public static void Apply(CabInteriorLighting lighting)
    {
        var old=lighting.transform.Find("Cab_Ceiling_Detail");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Cab_Ceiling_Detail").transform;root.SetParent(lighting.transform,false);
        var liner=NF5KPedalFinish.Material("Cab_Ceiling_Warm_Liner_V1",new Color(.57f,.59f,.54f),0,.12f,false);
        var trim=NF5KPedalFinish.Material("Cab_Ceiling_Trim_V1",new Color(.30f,.34f,.32f),.35f,.23f,false);
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Cab/Cab_Fan_Satin_Steel.mat");
        Transform Part(string name,Vector3 p,Vector3 size,Material material)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);g.transform.localPosition=p;g.layer=30;
            g.GetComponent<MeshFilter>().sharedMesh=NF5KPedalFinish.Rounded(size,.0015f);g.GetComponent<Renderer>().sharedMaterial=material;return g.transform;
        }
        void Screw(float x,float z)
        {
            var g=NF5KParkingConsoleMount.MeshPart(root,"Ceiling_Captive_Fastener",AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset"),steel);
            g.transform.localPosition=new Vector3(x,2.142f,z);g.transform.localRotation=Quaternion.Euler(180,0,0);g.transform.localScale=new Vector3(.007f,.001f,.007f);
        }
        // Thin individual facing panels sit against the preserved structural liner.
        foreach(float x in new[]{-.65f,.65f})foreach(float z in new[]{-.06f,.68f,1.42f})
        {
            Part("Ceiling_Removable_Liner_Panel",new Vector3(x,2.145f,z),new Vector3(1.296f,.005f,.736f),liner);
            foreach(float dx in new[]{-.60f,.60f})foreach(float dz in new[]{-.32f,.32f})Screw(x+dx,z+dz);
        }
        foreach(float x in new[]{-1.285f,1.285f})Part("Ceiling_Edge_Return",new Vector3(x,2.134f,.68f),new Vector3(.03f,.022f,2.22f),trim);
        foreach(int side in new[]{-1,1})
        {
            // Close the exposed space between the flat liner and body cant rail.
            string path="Assets/Resources/Cab/Cab_Ceiling_Cove_"+side+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                var vertices=new System.Collections.Generic.List<Vector3>();var indices=new System.Collections.Generic.List<int>();
                var profile=new[]{new Vector2(side*1.278f,2.146f),new Vector2(side*1.437f,2.146f),new Vector2(side*1.437f,2.027f),new Vector2(side*1.278f,2.127f)};
                void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
                {if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var q=b;b=d;d=q;}int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
                Vector3 P(int i,float z)=>new Vector3(profile[i].x,profile[i].y,z);
                var center=new Vector3(side*1.36f,2.09f,0);
                for(int i=0;i<4;i++){int j=(i+1)%4;var mid=(P(i,0)+P(j,0))*.5f;Quad(P(i,-.15f),P(j,-.15f),P(j,1.80f),P(i,1.80f),(mid-center).normalized);}
                Quad(P(0,-.15f),P(1,-.15f),P(2,-.15f),P(3,-.15f),Vector3.back);
                Quad(P(0,1.80f),P(1,1.80f),P(2,1.80f),P(3,1.80f),Vector3.forward);
                mesh=new Mesh{name="Closed_Cab_Ceiling_Cove"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.uv=vertices.Select(p=>new Vector2(p.z,p.y)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            NF5KParkingConsoleMount.MeshPart(root,"Ceiling_Closed_Side_Cove_"+side,mesh,liner);
        }
        // Follow the raked rounded windscreen shoulders instead of stopping at the straight side.
        const string frontCovePath="Assets/Resources/Cab/Cab_Ceiling_Front_Cove_V1.asset";
        var frontCove=AssetDatabase.LoadAssetAtPath<Mesh>(frontCovePath);
        if(frontCove==null)
        {
            var contour=new System.Collections.Generic.List<Vector2>();
            for(int i=0;i<=24;i++){float a=Mathf.PI+i*Mathf.PI/48;contour.Add(new Vector2(-1.10f+.337f*Mathf.Cos(a),-.15f+.50f*Mathf.Sin(a)));}
            for(int i=1;i<=24;i++){float a=-Mathf.PI/2+i*Mathf.PI/48;contour.Add(new Vector2(1.10f+.337f*Mathf.Cos(a),-.15f+.50f*Mathf.Sin(a)));}
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            foreach(var p in contour)
            {
                float corner=Mathf.Clamp01((Mathf.Abs(p.x)-1.10f)/.337f);
                float z=p.y+(.885f)*Mathf.Tan(8*Mathf.Deg2Rad)*Mathf.Sqrt(1-corner*corner);
                float innerX=p.x*(1.278f/1.437f),innerZ=Mathf.Max(-.425f,z);
                vertices.Add(new Vector3(innerX,2.146f,innerZ));
                vertices.Add(new Vector3(p.x,2.146f,z));
                vertices.Add(new Vector3(p.x,2.025f,z));
                vertices.Add(new Vector3(innerX,2.127f,innerZ));
            }
            void Face(int a,int b,int c,int d){triangles.AddRange(new[]{a,b,c,a,c,d});}
            for(int i=0;i<contour.Count-1;i++)for(int j=0;j<4;j++)
            {int k=(j+1)%4;Face(i*4+j,(i+1)*4+j,(i+1)*4+k,i*4+k);}
            Face(0,1,2,3);int end=(contour.Count-1)*4;Face(end+3,end+2,end+1,end);
            frontCove=new Mesh{name="Closed_Cab_Front_Cove"};frontCove.SetVertices(vertices);frontCove.SetTriangles(triangles,0);
            frontCove.uv=vertices.Select(p=>new Vector2(p.x,p.z)).ToArray();frontCove.RecalculateNormals();frontCove.RecalculateBounds();AssetDatabase.CreateAsset(frontCove,frontCovePath);
        }
        NF5KParkingConsoleMount.MeshPart(root,"Ceiling_Closed_Front_Cove",frontCove,liner);
        Part("Ceiling_Service_Hatch_Frame",new Vector3(0,2.139f,1.42f),new Vector3(.446f,.007f,.346f),trim);
        Part("Ceiling_Service_Hatch",new Vector3(0,2.1345f,1.42f),new Vector3(.424f,.003f,.324f),liner);
        foreach(float x in new[]{-.19f,.19f})
        {
            var latch=NF5KParkingConsoleMount.MeshPart(root,"Ceiling_Hatch_Quarter_Turn",AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset"),steel);
            latch.transform.localPosition=new Vector3(x,2.1325f,1.42f);latch.transform.localRotation=Quaternion.Euler(180,0,0);latch.transform.localScale=new Vector3(.010f,.001f,.010f);
        }
        NF5KEquipmentPrint.Add(root,"Ceiling_Access_Label","Cab_Ceiling_Access_Label",new Vector3(0,2.1328f,1.42f),new Vector2(.28f,.105f));
        var label=root.Find("Ceiling_Access_Label");label.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,180);
        NF5KLightingKeysSetup.ConfigureInk(label.GetComponent<Renderer>().sharedMaterial);
        label.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        const string texturePath="Assets/Resources/Cab/Cab_Ceiling_Opal_Emission.asset";
        var optical=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(optical==null)
        {
            optical=new Texture2D(64,64,TextureFormat.RGBA32,true,true){name="Ceiling_Opal_Emission",wrapMode=TextureWrapMode.Clamp};var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float edge=Mathf.Min(Mathf.Min(x,63-x),Mathf.Min(y,63-y))/10f;float value=Mathf.Lerp(.20f,.42f,Mathf.SmoothStep(0,1,edge));pixels[y*64+x]=new Color(value,value,value,1);}
            optical.SetPixels(pixels);optical.Apply();AssetDatabase.CreateAsset(optical,texturePath);
        }
        const string materialPath="Assets/Resources/Cab/Cab_Ceiling_Opal_Diffuser.mat";
        var opal=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        foreach(var fixture in lighting.Fixtures.Where(f=>f.circuit==InteriorLightCircuit.CabCeiling))
        {
            if(opal==null){opal=new Material(fixture.diffuser.sharedMaterial);AssetDatabase.CreateAsset(opal,materialPath);}
            opal.SetColor("_BaseColor",new Color(.52f,.54f,.49f));opal.SetFloat("_Smoothness",.22f);opal.SetTexture("_EmissionMap",optical);opal.EnableKeyword("_EMISSION");EditorUtility.SetDirty(opal);fixture.diffuser.sharedMaterial=opal;
            var assembly=fixture.light.transform.parent;
            // Keep the casing parallel to the liner; beam aiming is independent.
            var beamRotation=fixture.light.transform.rotation;
            assembly.rotation=lighting.transform.rotation;
            var mount=lighting.transform.InverseTransformPoint(assembly.position);mount.y=2.095f;
            assembly.position=lighting.transform.TransformPoint(mount);
            fixture.light.transform.rotation=beamRotation;
            var size=Vector3.Scale(fixture.diffuser.GetComponent<MeshFilter>().sharedMesh.bounds.size,fixture.diffuser.transform.localScale);size.y=.008f;
            foreach(Transform original in assembly)
                if(original.name.EndsWith("Housing")||original.name.EndsWith("Retainer")){var renderer=original.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;}
            var caseMaterial=NF5KPedalFinish.Material("Cab_Ceiling_Lamp_Case_V4",new Color(.43f,.46f,.43f),.35f,.22f,false);
            var casePart=Part("Ceiling_Lamp_Slim_Case",Vector3.zero,new Vector3(size.x+.028f,.022f,size.z+.028f),caseMaterial);
            casePart.position=assembly.TransformPoint(new Vector3(0,.0335f,0));casePart.rotation=assembly.rotation;
            foreach(float x in new[]{-.075f,.075f})foreach(float z in new[]{-.15f,.15f})
            {
                var pad=Part("Ceiling_Lamp_Mounting_Pad",Vector3.zero,new Vector3(.028f,.003f,.036f),trim);
                pad.position=assembly.TransformPoint(new Vector3(x,.046f,z));pad.rotation=assembly.rotation;
            }
            fixture.diffuser.GetComponent<MeshFilter>().sharedMesh=NF5KPedalFinish.Rounded(size,.002f);
            fixture.diffuser.transform.localScale=Vector3.one;fixture.diffuser.transform.localPosition=new Vector3(0,.019f,0);
            fixture.light.transform.localPosition=Vector3.zero;
            foreach(float x in new[]{-size.x/2-.003f,size.x/2+.003f})
            {
                var rail=Part("Ceiling_Lamp_Diffuser_Press_Rail",Vector3.zero,new Vector3(.007f,.008f,size.z+.006f),caseMaterial);rail.position=assembly.TransformPoint(new Vector3(x,.017f,0));rail.rotation=assembly.rotation;
            }
            foreach(float z in new[]{-size.z/2-.012f,size.z/2+.012f})
            {
                var cap=Part("Ceiling_Lamp_End_Cap",Vector3.zero,new Vector3(size.x+.028f,.020f,.018f),caseMaterial);cap.position=assembly.TransformPoint(new Vector3(0,.022f,z));cap.rotation=assembly.rotation;
                foreach(float x in new[]{-size.x/2,size.x/2})
                {
                    var screw=NF5KParkingConsoleMount.MeshPart(root,"Ceiling_Lamp_Retainer_Screw",AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Cab/Cab_Fan_Recessed_Slotted_Head_V6.asset"),steel);
                    screw.transform.position=assembly.TransformPoint(new Vector3(x,.011f,z));screw.transform.rotation=assembly.rotation*Quaternion.Euler(180,0,0);screw.transform.localScale=new Vector3(.006f,.001f,.006f);
                }
            }
            foreach(var corner in casePart.GetComponent<MeshFilter>().sharedMesh.vertices)
                if(lighting.transform.InverseTransformPoint(casePart.TransformPoint(corner)).y>2.140f)
                    throw new InvalidOperationException("Ceiling lamp housing penetrates liner clearance");
        }
        // Include the new opaque upper surfaces in the existing circuit-driven diffuse fill.
        var receivers=lighting.GetComponentsInChildren<Renderer>(true).Where(r=>
        {
            var p=lighting.transform.InverseTransformPoint(r.transform.TransformPoint(r.localBounds.center));
            return Mathf.Abs(p.x)<1.39f&&p.y>-.05f&&p.y<2.20f&&p.z>-.5f&&p.z<17.4f&&r.sharedMaterials.All(m=>m==null||m.renderQueue<3000);
        }).ToArray();
        lighting.GetComponent<CabDiffuseFill>().Configure(receivers);
        EditorUtility.SetDirty(lighting.GetComponent<CabDiffuseFill>());
        Debug.Log("NF5K_CEILING_DETAIL_BUILT: six liner panels, retained fasteners, edge returns and closed service hatch");
    }
    public static void Batch()
    {
        try
        {
            foreach(string n in new[]{"CabInteriorBlockout","CabDaylightReview","CabSimulationReview","CabRailMotionReview"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+n+".unity");Apply(UnityEngine.Object.FindAnyObjectByType<CabInteriorLighting>());EditorSceneManager.SaveScene(scene);
            }
            NF5KUpperCabLightingReview.Batch();
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
