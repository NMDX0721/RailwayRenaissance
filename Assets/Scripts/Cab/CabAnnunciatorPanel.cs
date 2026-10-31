using System;
using UnityEngine;
using RailwayRenaissance.Cab.Simulation;

namespace RailwayRenaissance.Cab
{
    // Indications consume simulation state; they never change protection or driving commands.
    public sealed class CabAnnunciatorLogic
    {
        public const int Count=10;
        public readonly bool[] Active=new bool[Count];
        public bool Powered {get;private set;}
        bool lowOil,hot,lowAir,braking,wasRunning;
        double runningSince,lastTime=-1;
        public void Evaluate(SimulationState s,SimulationInput u,SimulationConfig c)
        {
            if(s.Time<lastTime){lowOil=hot=lowAir=braking=wasRunning=false;}
            lastTime=s.Time;
            if(s.Running&&!wasRunning)runningSince=s.Time;
            wasRunning=s.Running;
            bool generated=s.Running&&s.Rpm>=c.IdleRpm*.9;
            Powered=u.Master&&(s.Battery>.2||generated);
            lowOil=s.Running&&s.Time-runningSince>=3&&s.OilMpa<(lowOil?.18:.15);
            hot=s.ThermalTrip||s.CoolantC>=(hot?c.TripTemperatureC-25:c.TripTemperatureC-20);
            lowAir=s.MainKpa<(lowAir?650:600);
            braking=Math.Max(s.LocoCylinderKpa,s.TrainCylinderKpa)>(braking?10:20);
            Active[0]=lowOil;Active[1]=hot;Active[2]=lowAir;Active[3]=!generated;
            Active[4]=u.Emergency||u.Brake==4;Active[5]=u.ParkingBrake;
            Active[6]=braking;Active[7]=s.AdhesionLimited;Active[8]=s.Cranking;Active[9]=s.Running;
        }
    }

    public sealed class CabAnnunciatorPanel : MonoBehaviour
    {
        [SerializeField] CabEngineState state;
        [SerializeField] Renderer[] lenses,legends;
        [SerializeField] Color[] colors;
        CabSimulationDriver driver;
        readonly CabAnnunciatorLogic logic=new CabAnnunciatorLogic();
        MaterialPropertyBlock properties;
        float testUntil;
        public bool IsTesting=>isActiveAndEnabled&&Time.unscaledTime<testUntil&&SupplyAvailable;
        public bool SupplyAvailable
        {
            get
            {
                if(state==null)return false;
                if(driver==null)driver=state.GetComponent<CabSimulationDriver>();
                if(driver==null||!driver.isActiveAndEnabled||driver.Model==null)return false;
                var s=driver.Model.State;
                return driver.Input.Master&&(s.Battery>.2||(s.Running&&s.Rpm>=driver.Model.Config.IdleRpm*.9));
            }
        }
        public bool RequestLampTest()
        {
            if(!isActiveAndEnabled||!SupplyAvailable||IsTesting)return false;
            testUntil=Time.unscaledTime+2;Refresh();return true;
        }
        public void CancelLampTest(){testUntil=0;}
        public Renderer[] Lenses=>lenses;
        public Renderer[] Legends=>legends;
        public void Configure(CabEngineState owner,Renderer[] windows,Renderer[] labels,Color[] tints)
        {state=owner;lenses=windows;legends=labels;colors=tints;ApplyReading(new bool[CabAnnunciatorLogic.Count],false);}
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!isActiveAndEnabled){CancelLampTest();ApplyReading(null,false);return;}
            if(state==null)return;
            if(driver==null)driver=state.GetComponent<CabSimulationDriver>();
            if(driver==null||!driver.isActiveAndEnabled||driver.Model==null)
            {ApplyReading(null,false);return;}
            logic.Evaluate(driver.Model.State,driver.Input,driver.Model.Config);
            if(!logic.Powered)testUntil=0;
            ApplyReading(logic.Active,logic.Powered);
        }
        public void ApplyReading(bool[] reading,bool powered)
        {
            if(lenses==null)return;
            properties??=new MaterialPropertyBlock();
            for(int i=0;i<lenses.Length;i++)
            {
                bool on=powered&&(IsTesting||(reading!=null&&reading[i]));
                properties.Clear();properties.SetColor("_BaseColor",colors[i]*(on?.12f:.045f)+new Color(.018f,.021f,.022f,0));
                properties.SetColor("_EmissionColor",on?colors[i]*1.05f:Color.black);lenses[i].SetPropertyBlock(properties);
                properties.Clear();properties.SetColor("_BaseColor",on?new Color(.10f,.10f,.08f):new Color(.65f,.65f,.59f));
                properties.SetColor("_EmissionColor",Color.black);legends[i].SetPropertyBlock(properties);
            }
        }
        void OnDisable(){CancelLampTest();ApplyReading(null,false);}
    }
}
