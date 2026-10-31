using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NF5KParkingPlacementAudit
{
    [Serializable] sealed class Item {public string name;public Vector3 min,max;}
    [Serializable] sealed class Face {public string name;public Vector3 a,b,c,normal;}
    [Serializable] sealed class Report {public Item[] meshes;public Face[] faces;}
    public static void Batch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CabDaylightReview.unity");var body=GameObject.Find("CabInterior_Blockout").transform;
            var meshes=body.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled).ToArray();
            Vector3[] V(MeshFilter f)=>f.sharedMesh.vertices.Select(v=>body.InverseTransformPoint(f.transform.TransformPoint(v))).ToArray();
            var rows=meshes.Select(f=>
            {var vs=V(f);var b=new Bounds(vs[0],Vector3.zero);foreach(var v in vs)b.Encapsulate(v);return new Item{name=f.name,min=b.min,max=b.max};})
                .Where(r=>r.max.x>.65f&&r.min.x<1.35f&&r.max.z>0&&r.min.z<1.25f&&r.max.y>.4f&&r.min.y<.95f).ToArray();
            var faces=new System.Collections.Generic.List<Face>();
            foreach(var f in meshes.Where(f=>f.name=="Continuous_Folded_Console_Shell"||f.name=="Contoured_Control_Deck"))
            {
                var vs=V(f);var ts=f.sharedMesh.triangles;
                for(int i=0;i<ts.Length;i+=3)
                {
                    var a=vs[ts[i]];var b=vs[ts[i+1]];var c=vs[ts[i+2]];var n=Vector3.Cross(b-a,c-a).normalized;
                    var at=(a+b+c)/3;if(n.z>.5f&&at.y>.65f&&at.z>.20f)faces.Add(new Face{name=f.name,a=a,b=b,c=c,normal=n});
                }
            }
            File.WriteAllText("tools/compile/parking_mount_before.json",JsonUtility.ToJson(new Report{meshes=rows,faces=faces.ToArray()},true));
            Debug.Log("NF5K_PARKING_MOUNT_AUDIT_OK: transformed mesh vertices and real console facets, scene unchanged");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
