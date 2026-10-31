using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabLightingKey : MonoBehaviour
    {
        [Serializable] public sealed class TextPair { public string zh, en; public string Get(bool english)=>english?en:zh; }
        [Serializable] public sealed class Entry { public string id,zh,en,printZh,printEn; }
        [Serializable] public sealed class Profile
        {
            public Entry[] entries;
            public TextPair on,off,noPower,spare,turnOn,turnOff,approach,already,execute;
        }
        static Profile profile;
        public static Profile Labels
        {
            get
            {
                if(profile==null)
                {
                    var source=Resources.Load<TextAsset>("Cab/lighting_keys_profile");
                    if(source==null)throw new InvalidOperationException("Missing lighting key labels");
                    profile=JsonUtility.FromJson<Profile>(source.text);
                    if(profile.entries==null||profile.entries.Length!=10)throw new InvalidOperationException("Expected ten lighting key labels");
                    var ids=new System.Collections.Generic.HashSet<string>();
                    foreach(var e in profile.entries)
                        if(string.IsNullOrWhiteSpace(e.id)||!ids.Add(e.id)||string.IsNullOrWhiteSpace(e.zh)||string.IsNullOrWhiteSpace(e.en))throw new InvalidOperationException("Invalid lighting key translation");
                }
                return profile;
            }
        }
        // Adapter point for the future application-wide language service; physical prints remain bilingual.
        public static bool English { get; private set; }
        public static void SetLanguage(string locale) { English=locale!=null&&locale.StartsWith("en",StringComparison.OrdinalIgnoreCase); }
        [SerializeField] int index;
        [SerializeField] Transform pivot;
        CabInteriorLighting lighting;
        CabInteriorLighting Lighting => lighting!=null?lighting:lighting=GetComponentInParent<CabInteriorLighting>();
        public int Index=>index;
        public Transform Pivot=>pivot;
        public bool Connected=>index>=0&&index<7;
        public string Label=>English?Labels.entries[index].en:Labels.entries[index].zh;
        public bool Selected
        {
            get
            {
                var l=Lighting;if(l==null||!Connected)return false;
                switch(index)
                {
                    case 0:return l.CabCeilingOn;
                    case 1:return l.InstrumentDimmer>0;
                    case 2:return l.TaskOn;
                    case 3:return l.AssistantTaskOn;
                    case 4:return l.AccessOn;
                    case 5:return l.PassengerZoneOn(0);
                    case 6:return l.PassengerZoneOn(1);
                    default:return false;
                }
            }
        }
        public string Hint=>Label+" · "+(!Connected?Labels.spare:Selected?(Lighting!=null&&Lighting.MainPower?Labels.on:Labels.noPower):Labels.off).Get(English);
        public string ActionLabel(bool on)=>(on?Labels.turnOn:Labels.turnOff).Get(English);
        public void Configure(int number,Transform movingPivot)
        {
            if(number<0||number>=10||movingPivot==null)throw new ArgumentOutOfRangeException(nameof(number));
            index=number;pivot=movingPivot;lighting=null;ApplyVisual(true);
        }
        public bool CanOperate()=>isActiveAndEnabled&&gameObject.activeInHierarchy&&Connected&&Lighting!=null&&Camera.main!=null&&Vector3.Distance(Camera.main.transform.position,transform.position)<=2.4f;
        public bool SetSelected(bool on)
        {
            if(!CanOperate()||Selected==on)return false;
            var l=Lighting;
            switch(index)
            {
                case 0:l.SetCabCeiling(on);break;
                case 1:l.SetInstrumentEnabled(on);break;
                case 2:l.SetTask(on);break;
                case 3:l.SetAssistantTask(on);break;
                case 4:l.SetAccess(on);break;
                case 5:l.SetPassengerZone(0,on);break;
                case 6:l.SetPassengerZone(1,on);break;
            }
            return true;
        }
        public void ApplyVisual(bool immediate=false)
        {
            if(pivot==null)return;
            float target=Selected?22:-22;
            float angle=immediate?target:Mathf.MoveTowardsAngle(pivot.localEulerAngles.x,target,360*Time.deltaTime);
            pivot.localRotation=Quaternion.Euler(angle,0,0);
        }
        void LateUpdate(){ApplyVisual();}
    }
}
