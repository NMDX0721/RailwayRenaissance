using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public sealed class CabHornAudio : MonoBehaviour
    {
        [SerializeField] CabEngineState state;
        [SerializeField] AudioSource source;
        public AudioSource Source=>source;
        public void Configure(CabEngineState owner,AudioSource voice){state=owner;source=voice;}
        public void Refresh(float delta)
        {
            if(source==null||state==null)return;
            bool on=state.IsHornActive&&state.MainReservoirKpa>=400;
            source.volume=Mathf.MoveTowards(source.volume,on?.34f:0,Mathf.Max(0,delta)*3);
            if(on&&!source.isPlaying)source.Play();
            else if(!on&&source.volume==0&&source.isPlaying)source.Stop();
        }
        void Update(){Refresh(Time.unscaledDeltaTime);}
        void OnDisable(){if(source!=null){source.Stop();source.volume=0;}}
    }
}
