using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabWorkstationEquipment : MonoBehaviour
    {
        public enum EquipmentKind { Radio, Documents }
        [SerializeField] EquipmentKind kind;
        [SerializeField] Transform volumeKnob, channelKnob;
        [SerializeField] Renderer powerLamp;
        [SerializeField] Material lampOff, lampOn;
        [SerializeField, TextArea(4, 20)] string document;
        [SerializeField] Quaternion volumeRest, channelRest;
        [SerializeField] bool initialized;
        bool switchedOn;
        int volume=3, channel=1;
        public EquipmentKind Kind => kind;
        public int Volume => volume;
        public int Channel => channel;
        public bool SwitchedOn => switchedOn;
        public bool Powered
        {
            get
            {
                var lighting=GetComponentInParent<CabInteriorLighting>();
                return switchedOn&&lighting!=null&&(lighting.MainPower||lighting.BatteryPower);
            }
        }
        public string Description => document;
        public string Hint => kind==EquipmentKind.Documents?"随车检查记录 / ONBOARD CHECK RECORD":
            "列车无线电 · "+(!switchedOn?"关闭":Powered?"本机通电":"等待供电")+" · CH "+channel.ToString("D2")+" · 音量 "+volume+"/5";
        public void Configure(EquipmentKind type,string content,Transform volumeControl=null,Transform channelControl=null,
            Renderer lamp=null,Material off=null,Material on=null)
        {
            kind=type;document=content;volumeKnob=volumeControl;channelKnob=channelControl;powerLamp=lamp;lampOff=off;lampOn=on;
            initialized=false;Initialize();Apply();
        }
        void Initialize()
        {
            if(initialized)return;
            if(volumeKnob!=null)volumeRest=volumeKnob.localRotation;
            if(channelKnob!=null)channelRest=channelKnob.localRotation;
            initialized=true;
        }
        public bool CanOperate()
        {
            var camera=Camera.main;
            return isActiveAndEnabled&&camera!=null&&Vector3.Distance(camera.transform.position,transform.position)<=2.4f;
        }
        // The supply gates the receiver, not its mechanical switches and detents.
        public bool Operate(int action)
        {
            if(kind!=EquipmentKind.Radio||!CanOperate())return false;
            if(action==0)switchedOn=!switchedOn;
            else if(action==1)channel=channel%8+1;
            else if(action==2)channel=(channel+6)%8+1;
            else if(action==3&&volume<5)volume++;
            else if(action==4&&volume>0)volume--;
            else return false;
            Apply();return true;
        }
        void OnEnable(){Initialize();Apply();}
        void LateUpdate(){Apply();}
        void Apply()
        {
            Initialize();
            if(volumeKnob!=null)volumeKnob.localRotation=volumeRest*Quaternion.Euler(0,(-120+volume*48),0);
            if(channelKnob!=null)channelKnob.localRotation=channelRest*Quaternion.Euler(0,(channel-1)*45,0);
            if(powerLamp!=null&&lampOff!=null&&lampOn!=null)powerLamp.sharedMaterial=Powered?lampOn:lampOff;
        }
        void OnDisable()
        {
            switchedOn=false;volume=3;channel=1;
            Apply();
            if(powerLamp!=null&&lampOff!=null)powerLamp.sharedMaterial=lampOff;
        }
    }
}
