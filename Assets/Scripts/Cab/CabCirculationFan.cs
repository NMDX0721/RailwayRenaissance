using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabCirculationFan : MonoBehaviour
    {
        [SerializeField] Transform rotor, speedKnob;
        [SerializeField] Transform yawPivot, swingSelector, flexibleLead, motorGland, fixedCableClip;
        [SerializeField] LineRenderer serviceLoop;
        [SerializeField] bool driverSide;
        int speed;
        float phase, angularSpeed, swingPhase, yawAngle;
        bool oscillating;
        Quaternion yawRest=Quaternion.identity;
        public int Speed => speed;
        public Transform Head => rotor != null ? rotor.parent : null;
        public float Phase => phase;
        public float AngularSpeed => angularSpeed;
        public float YawAngle => yawAngle;
        public bool Oscillating => oscillating;
        public Vector3 CableMotorEndpoint => motorGland != null ? motorGland.TransformPoint(Vector3.back*.65f) : transform.position;
        public Transform Control => speedKnob != null ? speedKnob.parent : transform;
        public bool Powered
        {
            get { var supply=GetComponentInParent<CabInteriorLighting>(); return isActiveAndEnabled && speed>0 && supply!=null && (supply.MainPower||supply.BatteryPower); }
        }
        public string Hint => (driverSide?"司机循环风扇":"副司机循环风扇")+" CF-17R · "+(speed==0?(angularSpeed>1?"停机滑行":"关闭"):(Powered?"运行":"等待供电")+" · "+(speed==1?"低档":"高档"))+" · "+(oscillating?"摆头":"定向");
        public void Configure(Transform blades,Transform knob,bool driver)
        { rotor=blades;speedKnob=knob;driverSide=driver;speed=0;phase=0;Apply(); }
        public void ConfigureMotion(Transform pivot,Transform selector,Transform lead,Transform gland,Transform clip)
        { yawPivot=pivot;swingSelector=selector;flexibleLead=lead;motorGland=gland;fixedCableClip=clip;serviceLoop=lead.parent.GetComponentInChildren<LineRenderer>();yawRest=pivot.localRotation;Apply(); }
        public bool ToggleOscillation()
        { if(!CanOperate())return false;oscillating=!oscillating;Apply();return true; }
        public bool CanOperate()
        { var camera=Camera.main; return isActiveAndEnabled && camera!=null && Vector3.Distance(camera.transform.position,Control.position)<=2.4f; }
        public bool SelectSpeed(int value)
        {
            if(!CanOperate()||value<0||value>2||value==speed)return false;
            speed=value;Apply();return true;
        }
        public void Advance(float seconds)
        {
            if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            float dt=Mathf.Min(seconds,.25f),before=angularSpeed;
            // Authored visual motor response; not measured RPM, torque or airflow.
            angularSpeed=Mathf.MoveTowards(angularSpeed,Powered?(speed==1?540:900):0,dt*(Powered?1200:480));
            phase=Mathf.Repeat(phase+(before+angularSpeed)*.5f*dt,360);
            if(Powered&&oscillating){swingPhase=Mathf.Repeat(swingPhase+dt*Mathf.PI/10,Mathf.PI*2);yawAngle=Mathf.Sin(swingPhase)*25;}
            Apply();
        }
        void Update(){Advance(Time.deltaTime);}
        void Apply()
        {
            if(rotor!=null)rotor.localRotation=Quaternion.Euler(0,0,phase);
            if(speedKnob!=null)speedKnob.localRotation=Quaternion.Euler(0,-60+speed*60,0);
            if(yawPivot!=null)yawPivot.localRotation=yawRest*Quaternion.Euler(0,yawAngle,0);
            if(swingSelector!=null)swingSelector.localRotation=Quaternion.Euler(oscillating?12:-12,0,0);
            if(flexibleLead!=null&&motorGland!=null&&fixedCableClip!=null)
            {
                var parent=flexibleLead.parent;var a=parent.InverseTransformPoint(CableMotorEndpoint);var b=parent.InverseTransformPoint(fixedCableClip.position);
                flexibleLead.localPosition=(a+b)*.5f;flexibleLead.localScale=new Vector3(.005f,Vector3.Distance(a,b)*.5f,.005f);
                flexibleLead.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
                if(serviceLoop!=null)
                {
                    var first=CableMotorEndpoint;var last=fixedCableClip.position;
                    var bend=(first+last)*.5f+parent.TransformVector(new Vector3(0,-.012f,-.025f));
                    for(int i=0;i<serviceLoop.positionCount;i++)
                    {float t=i/(float)(serviceLoop.positionCount-1);serviceLoop.SetPosition(i,(1-t)*(1-t)*first+2*(1-t)*t*bend+t*t*last);}
                }
            }
        }
        void OnDisable(){speed=0;phase=0;angularSpeed=0;oscillating=false;swingPhase=0;yawAngle=0;Apply();}
    }
}
