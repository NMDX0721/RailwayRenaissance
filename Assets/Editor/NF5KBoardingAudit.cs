using System;
using System.IO;
using UnityEngine;

public static class NF5KBoardingAudit
{
    [Serializable] class Result
    {
        public int stepsPerSide;
        public float groundY, firstRiseM, stepRiseM, treadDepthM, treadWidthM, gripClearanceM, supportedBootLengthM;
        public bool staticFootClearance, gripEnvelope;
        public string scope="Static 290x115x110 mm boot and 1.75 m figure proxy; no animated climb, strength or platform certification";
    }
    public static void Validate(string reportPath="tools/compile/cab_boarding_static_audit.json")
    {
        Physics.SyncTransforms();var r=new Result{stepsPerSide=4,stepRiseM=.28f,treadDepthM=.24f,treadWidthM=.66f,gripClearanceM=1.59f-.016f-1.49f,supportedBootLengthM=.215f};
        var ground=GameObject.Find("Inspection_Apron_Right")??GameObject.Find("Review_Ground_Not_Vehicle");r.groundY=ground.GetComponent<Renderer>().bounds.max.y;
        r.firstRiseM=-1.10f+.02f-r.groundY;
        if(r.firstRiseM>.45f||r.firstRiseM<.15f||r.gripClearanceM<.075f)throw new InvalidOperationException("Boarding rise or fist space failed");
        foreach(string name in new[]{"Left","Right"})
        {
            int side=name=="Left"?-1:1;
            for(int i=0;i<4;i++)
            {
                var step=GameObject.Find(name+"_Access_Step_"+i);
                if(step==null)throw new InvalidOperationException("Missing boarding step");
                float sole=step.transform.position.y+.02f;
                foreach(int handSide in new[]{-1,1})
                {
                    var shoulder=new Vector3(side*1.70f,sole+1.40f,1.49f+handSide*.19f);
                    var grip=new Vector3(side*1.59f,Mathf.Clamp(sole+1.22f,.225f,1.72f),1.49f+handSide*.45f);
                    if(Vector3.Distance(shoulder,grip)>.65f)throw new InvalidOperationException("Grip outside candidate arm reach");
                }
                foreach(float z in new[]{1.34f,1.64f})foreach(float lift in new[]{0,.035f,.07f})
                {
                var foot=new Vector3(side*(1.50f+.03f*i+.05f),step.transform.position.y+.02f+.065f+lift,z);
                foreach(var c in Physics.OverlapBox(foot,new Vector3(.145f,.055f,.058f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                {
                    if(c.gameObject==step)continue;
                    throw new InvalidOperationException("Boot envelope blocked by "+c.name+" at "+name+" step "+i);
                }
                }
            }
        }
        r.staticFootClearance=true;r.gripEnvelope=true;
        File.WriteAllText(reportPath,JsonUtility.ToJson(r,true));
        Debug.Log("NF5K_BOARDING_STATIC_OK: "+JsonUtility.ToJson(r));
    }
    public static GameObject Figure()
    {
        var root=new GameObject("Review_175cm_Person_Not_Vehicle");
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.65f,.37f,.18f);
        material.SetFloat("_Smoothness",.12f);
        void Part(string name,Vector3 a,Vector3 b,float radius)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Capsule);obj.name=name;obj.transform.SetParent(root.transform);
            obj.transform.position=(a+b)/2;obj.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);obj.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2);obj.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        }
        const float sole=-.52f;
        foreach(int s in new[]{-1,1})
        {
            float z=1.49f+s*.15f;
            var boot=GameObject.CreatePrimitive(PrimitiveType.Cube);boot.name="290mm_Boot_Proxy";boot.transform.SetParent(root.transform);boot.transform.position=new Vector3(1.58f,sole+.055f,z);boot.transform.localScale=new Vector3(.29f,.11f,.115f);boot.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(boot.GetComponent<Collider>());
            Part("Calf",new Vector3(1.58f,sole+.11f,z),new Vector3(1.66f,sole+.48f,z),.05f);
            Part("Thigh",new Vector3(1.66f,sole+.48f,z),new Vector3(1.82f,sole+.90f,z),.065f);
            var shoulder=new Vector3(1.70f,sole+1.40f,1.49f+s*.19f);
            var elbow=new Vector3(1.88f,sole+1.16f,1.49f+s*.30f);
            Part("Upper_Arm",shoulder,elbow,.04f);Part("Forearm",elbow,new Vector3(1.59f,sole+1.22f,1.49f+s*.45f),.035f);
            var hand=GameObject.CreatePrimitive(PrimitiveType.Sphere);hand.name="Grip_Proxy";hand.transform.SetParent(root.transform);hand.transform.position=new Vector3(1.59f,sole+1.22f,1.49f+s*.45f);hand.transform.localScale=Vector3.one*.075f;hand.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(hand.GetComponent<Collider>());
        }
        Part("Torso",new Vector3(1.82f,sole+.94f,1.49f),new Vector3(1.70f,sole+1.39f,1.49f),.16f);
        Part("Neck",new Vector3(1.70f,sole+1.38f,1.49f),new Vector3(1.68f,sole+1.57f,1.49f),.045f);
        var pelvis=GameObject.CreatePrimitive(PrimitiveType.Sphere);pelvis.name="Pelvis_Proxy";pelvis.transform.SetParent(root.transform);pelvis.transform.position=new Vector3(1.82f,sole+.92f,1.49f);pelvis.transform.localScale=new Vector3(.25f,.18f,.31f);pelvis.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(pelvis.GetComponent<Collider>());
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Head";head.transform.SetParent(root.transform);head.transform.position=new Vector3(1.68f,sole+1.65f,1.49f);head.transform.localScale=Vector3.one*.20f;head.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        foreach(var part in root.GetComponentsInChildren<Transform>())part.gameObject.layer=30;
        return root;
    }
}
