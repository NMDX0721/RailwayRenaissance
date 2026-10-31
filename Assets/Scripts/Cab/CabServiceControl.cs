using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabServiceControl : MonoBehaviour
    {
        [SerializeField] CabEngineState state;
        [SerializeField] bool sanding;
        [SerializeField] Transform cap;
        [SerializeField] Vector3 rest;
        float until;
        public bool Active {get;private set;}
        public bool Sanding=>sanding;
        public Transform Cap=>cap;
        public string Hint=>CabLightingKey.English?(sanding?"Manual sanding":"Air horn"):(sanding?"手动撒砂":"鸣笛");
        public string ActionLabel=>CabLightingKey.English?"Hold primary · click for short pulse":"按住交互键 · 菜单点按短动作";
        public void Configure(CabEngineState owner,bool sand,Transform moving){state=owner;sanding=sand;cap=moving;rest=moving.localPosition;}
        public bool CanOperate()
        {
            if(!isActiveAndEnabled||Time.timeScale<=0||state==null||Camera.main==null||Vector3.Distance(Camera.main.transform.position,transform.position)>2.4f)return false;
            var driver=state.GetComponent<CabSimulationDriver>();
            if(driver==null||!driver.isActiveAndEnabled||driver.Model==null)return false;
            var s=driver.Model.State;
            return driver.Input.Master&&(s.Battery>.2||(s.Running&&s.Rpm>=driver.Model.Config.IdleRpm*.9))&&s.MainKpa>=400;
        }
        public string UnavailableReason=>CanOperate()?null:(CabLightingKey.English?"Move closer; control power and main air ≥ 400 kPa required":"请靠近操作；需要主控供电和总风不低于 400 kPa");
        public bool Begin()
        {
            if(!CanOperate())return false;
            until=Time.time+.5f;Active=true;ApplyOutput();return true;
        }
        public void Maintain(){if(Active&&CanOperate()){until=Time.time+.5f;ApplyOutput();}else Release();}
        public void Release()
        {
            if(Active&&state!=null){if(sanding)state.SandOn=false;else state.SetPhysicalHorn(false);}
            Active=false;ApplyVisual();
        }
        void ApplyOutput(){if(sanding)state.SandOn=true;else state.SetPhysicalHorn(true);ApplyVisual();}
        public void ApplyVisual(){if(cap!=null)cap.localPosition=rest-(Active?Vector3.up*.002f:Vector3.zero);}
        public void Refresh(){if(Active&&(!CanOperate()||Time.time>=until))Release();}
        void Update(){Refresh();}
        void OnDisable(){Release();}
        void OnApplicationFocus(bool focus){if(!focus)Release();}
    }
}
