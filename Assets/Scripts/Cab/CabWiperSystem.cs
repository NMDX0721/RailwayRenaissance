using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabWiperSystem : MonoBehaviour
    {
        [Serializable] public sealed class Assembly
        {
            public Transform plane,glass,arm,parallel,carrier,rubber;
            public int side;
        }
        [SerializeField] Assembly[] assemblies;
        [SerializeField] Transform control,selector;
        [SerializeField] CabInteriorLighting lighting;
        [SerializeField] Cab3DSceneController legacy;
        readonly CabWiperMotion motion=new CabWiperMotion();
        public CabWiperMotion Motion => motion;
        public Assembly[] Assemblies => assemblies;
        public Transform Control => control;
        public bool Powered
        {
            get
            {
                if(lighting==null)lighting=GetComponentInParent<CabInteriorLighting>();
                return isActiveAndEnabled&&lighting!=null&&(lighting.MainPower||lighting.BatteryPower);
            }
        }
        public string Hint => "双雨刷 · "+(motion.Mode==0?(motion.Parking?"回位中":"关闭"):motion.Mode==1?"低速":"高速")+(!Powered&&(motion.Mode>0||motion.Parking)?" · 等待供电":"");
        public void Configure(Assembly[] links,Transform panel,Transform knob,CabInteriorLighting supply)
        {assemblies=links;control=panel;selector=knob;lighting=supply;Apply();}
        public void BindLegacy(Cab3DSceneController source)
        {
            if(legacy!=null)legacy.WiperStateChanged-=LegacyChanged;
            legacy=source;
            if(legacy!=null){legacy.WiperStateChanged+=LegacyChanged;LegacyChanged(legacy.WipersOn);}
        }
        void LegacyChanged(bool on){if(on&&motion.Mode==0)motion.SelectMode(1);else if(!on)motion.SelectMode(0);Apply();}
        void OnEnable(){if(legacy!=null){legacy.WiperStateChanged+=LegacyChanged;LegacyChanged(legacy.WipersOn);}}
        public bool CanOperate()=>isActiveAndEnabled&&control!=null&&Camera.main!=null&&Vector3.Distance(Camera.main.transform.position,control.position)<=2.4f;
        public bool SelectMode(int value)
        {
            if(!CanOperate()||!motion.SelectMode(value))return false;
            if(legacy!=null)legacy.SetWipers(value>0);Apply();return true;
        }
        public void Advance(float dt,bool powered){motion.Advance(dt,powered&&isActiveAndEnabled);Apply();}
        void Update(){Advance(Time.deltaTime,Powered);}
        public void Apply()
        {
            if(selector!=null)selector.localRotation=Quaternion.Euler(0,-60+motion.Mode*60,0);
            if(assemblies==null)return;
            foreach(var a in assemblies)
            {
                float angle=a.side*(50-100*(float)motion.Sweep)*Mathf.Deg2Rad;
                var p=new Vector3(-.0175f,-.398f,-.030f);
                var joint=p+new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),0)*.460f;
                Link(a.arm,p,joint,.016f,.006f);
                Link(a.parallel,p+Vector3.right*.035f,joint+Vector3.right*.035f,.009f,.004f);
                a.carrier.localPosition=new Vector3(joint.x+.0175f,joint.y,0);
            }
        }
        static void Link(Transform target,Vector3 a,Vector3 b,float width,float depth)
        {target.localPosition=(a+b)*.5f;target.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);target.localScale=new Vector3(width,Vector3.Distance(a,b),depth);}
        void OnDisable()
        {
            if(legacy!=null)legacy.WiperStateChanged-=LegacyChanged;
            motion.Reset();Apply();
        }
    }
}
