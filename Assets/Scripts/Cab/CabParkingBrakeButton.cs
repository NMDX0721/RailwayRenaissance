using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabParkingBrakeButton : MonoBehaviour
    {
        [Serializable] public sealed class Label { public string zh,en; public string Text=>CabLightingKey.English?en:zh; }
        [Serializable] public sealed class Profile { public Label title,apply,release; }
        static Profile labels;
        public static Profile Labels=>labels??(labels=JsonUtility.FromJson<Profile>(Resources.Load<TextAsset>("Cab/parking_brake_profile").text));
        [SerializeField] CabEngineState state;
        [SerializeField] CabDrivingTarget command;
        [SerializeField] bool release;
        [SerializeField] Transform cap;
        [SerializeField] Vector3 rest;
        float pressedUntil;
        public bool ReleaseButton=>release;
        public Transform Cap=>cap;
        public string Hint=>Labels.title.Text+" · "+(release?Labels.release.Text:Labels.apply.Text);
        public string ActionLabel=>(CabLightingKey.English?"Hold to ":"长按")+(release?Labels.release.Text:Labels.apply.Text);
        public void Configure(CabEngineState owner,CabDrivingTarget facade,bool releasing,Transform moving)
        {state=owner;command=facade;release=releasing;cap=moving;rest=moving.localPosition;}
        public string UnavailableReason
        {
            get
            {
                bool en=CabLightingKey.English;
                if(!isActiveAndEnabled||state==null||command==null||!command.isActiveAndEnabled||Camera.main==null||Vector3.Distance(Camera.main.transform.position,transform.position)>2.4f)
                    return en?"Move closer to the button":"请靠近按钮操作";
                var driver=state.GetComponent<CabSimulationDriver>();
                if(driver==null||!driver.isActiveAndEnabled||driver.Model==null)return en?"Simulation unavailable":"当前场景未启用模拟";
                if(Time.timeScale<=0||driver.SimulationPaused)return en?"Simulation paused":"模拟已暂停";
                var s=driver.Model.State;
                if(!driver.Input.Master||!(s.Battery>.2||(s.Running&&s.Rpm>=driver.Model.Config.IdleRpm*.9)))return en?"Control power unavailable":"主控未供电";
                if(Mathf.Abs(state.CurrentSpeedKmh)>.1f||state.ThrottleNotch!=0)return en?"Stop and return traction to zero":"先停车并将牵引归零";
                if(driver.ParkingBrake==!release)return en?"Command already selected":"已在该指令状态";
                return null;
            }
        }
        public bool Press()
        {
            if(UnavailableReason!=null||!command.Activate(release))return false;
            pressedUntil=Time.unscaledTime+.18f;Refresh();return true;
        }
        public void Refresh()
        {
            if(cap==null)return;
            var driver=state!=null?state.GetComponent<CabSimulationDriver>():null;
            bool powered=driver!=null&&driver.isActiveAndEnabled&&driver.Model!=null&&driver.Input.Master
                &&(driver.Model.State.Battery>.2||driver.Model.State.Running);
            if(!isActiveAndEnabled||!powered||Time.timeScale<=0||driver.SimulationPaused)pressedUntil=0;
            cap.localPosition=rest-(Time.unscaledTime<pressedUntil?Vector3.up*.0018f:Vector3.zero);
        }
        void LateUpdate(){Refresh();}
        void OnDisable(){pressedUntil=0;if(cap!=null)cap.localPosition=rest;}
    }
}
