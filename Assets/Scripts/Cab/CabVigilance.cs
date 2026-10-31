using System;
using UnityEngine;
using RailwayRenaissance.Cab.Simulation;

namespace RailwayRenaissance.Cab
{
    // Authored periodic acknowledgement, not a certified national protection system.
    public sealed class CabVigilance : MonoBehaviour
    {
        [Serializable] public sealed class Snapshot
        {
            public int version=1;
            public double elapsed;
            public bool penalty;
        }
        [SerializeField] Transform tread;
        [SerializeField] double interval=120,visualLead=5,audibleGrace=8;
        Snapshot current=new Snapshot();
        [SerializeField] Vector3 rest;
        [SerializeField] Transform hinge;
        [SerializeField] Quaternion hingeRest=Quaternion.identity;
        float returnAt;
        AudioSource sound; AudioClip tone;
        [SerializeField] CabEngineState state;
        public bool Penalty=>current.penalty;
        public int Phase=>Penalty?3:current.elapsed>=interval?2:current.elapsed>=interval-visualLead?1:0;
        public Transform Tread=>tread;
        public Transform Hinge=>hinge;
        public string Hint=>CabLightingKey.English?"Driver vigilance pedal":"司机警惕踏板";
        public string ActionLabel=>CabLightingKey.English?"Press and release to acknowledge":"踩下并松开确认";
        public void Configure(CabEngineState owner,Transform moving,Transform pivot=null){state=owner;tread=moving;rest=moving.localPosition;hinge=pivot;if(hinge!=null)hingeRest=hinge.localRotation;}
        public string Capture()=>JsonUtility.ToJson(current);
        public static void Validate(string json)
        {
            if(string.IsNullOrEmpty(json))return;
            var s=JsonUtility.FromJson<Snapshot>(json);
            if(s==null||s.version!=1||double.IsNaN(s.elapsed)||double.IsInfinity(s.elapsed)||s.elapsed<0||s.elapsed>1000000)
                throw new ArgumentException("Invalid vigilance snapshot");
        }
        public void Restore(string json){Validate(json);current=string.IsNullOrEmpty(json)?new Snapshot():JsonUtility.FromJson<Snapshot>(json);}
        public void Step(SimulationInput input,SimulationState model)
        {
            // No free-running Unity timer: advances once for each actual simulation tick.
            if(!Penalty)
            {
                if(!input.Master||Math.Abs(model.SpeedMps)<.14)current.elapsed=0;
                else current.elapsed+=CabSimulation.StepSeconds;
                if(current.elapsed>=interval+audibleGrace)current.penalty=true;
            }
            if(Penalty){input.Throttle=0;input.Brake=(int)BrakeNotch.Emergency;}
        }
        public string UnavailableReason
        {
            get
            {
                bool en=CabLightingKey.English;var d=state!=null?state.GetComponent<CabSimulationDriver>():null;
                if(!isActiveAndEnabled||d==null||!d.isActiveAndEnabled||d.Model==null||Camera.main==null||Vector3.Distance(Camera.main.transform.position,transform.position)>2.4f)
                    return en?"Move closer to the pedal":"请靠近踏板";
                if(Time.timeScale<=0||d.SimulationPaused)return en?"Simulation paused":"模拟已暂停";
                if(!d.Input.Master)return en?"Master control is off":"主控未开启";
                if(Penalty&&(Math.Abs(d.Model.State.SpeedMps)>.028||state.ThrottleNotch!=0||state.BrakePos!=BrakeNotch.Emergency))
                    return en?"Stop; traction zero; brake in emergency":"停车、牵引归零并将大闸置于紧急位后复位";
                return null;
            }
        }
        public bool Acknowledge()
        {
            if(UnavailableReason!=null)return false;
            Research.CabManualControlIntent.Notify(state);
            current=new Snapshot();returnAt=Time.unscaledTime+.18f;Refresh();return true;
        }
        void Refresh()
        {
            var d=state!=null?state.GetComponent<CabSimulationDriver>():null;
            bool live=isActiveAndEnabled&&d!=null&&d.isActiveAndEnabled&&!d.SimulationPaused&&Time.timeScale>0;
            bool pressed=live&&Time.unscaledTime<returnAt;
            if(tread!=null)tread.localPosition=rest-(hinge==null&&pressed?Vector3.up*.006f:Vector3.zero);
            if(hinge!=null)hinge.localRotation=hingeRest*Quaternion.Euler(pressed?-2f:0,0,0);
            bool powered=d!=null&&d.Model!=null&&d.Input.Master&&(d.Model.State.Battery>.2||d.Model.State.Running);
            if(sound!=null)sound.mute=!live||!powered||Phase<2;
        }
        void Start()
        {
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.loop=true;sound.volume=.18f;sound.spatialBlend=0;
            tone=AudioClip.Create("Vigilance warning",24000,1,24000,false);var pcm=new float[24000];
            for(int i=0;i<pcm.Length;i++)pcm[i]=i%12000<6000?(float)(Math.Sin(2*Math.PI*880*i/24000)*.25):0;
            tone.SetData(pcm,0);sound.clip=tone;sound.mute=true;sound.Play();
        }
        void LateUpdate()=>Refresh();
        void OnDisable(){returnAt=0;Refresh();}
        void OnDestroy(){if(tone!=null)Destroy(tone);}
        void OnGUI()
        {
            if(Phase==0||state==null)return;
            var d=state.GetComponent<CabSimulationDriver>();if(d==null||d.SimulationPaused||Time.timeScale<=0)return;
            var old=GUI.color;GUI.color=Penalty?new Color(1,.45f,.3f):new Color(1,.85f,.3f);
            var style=new GUIStyle(GUI.skin.box){font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular"),fontSize=18};
            string text=Penalty?"警惕保护制动 · 停车后复位 / VIGILANCE BRAKE":Phase==2?"警惕报警 · 请踩踏板确认 / ACKNOWLEDGE":"警惕提醒 · 请确认 / VIGILANCE";
            GUI.Box(new Rect(Screen.width*.5f-250,60,500,38),text,style);GUI.color=old;
        }
    }
}
