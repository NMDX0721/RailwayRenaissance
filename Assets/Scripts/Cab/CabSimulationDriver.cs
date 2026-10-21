using System;
using UnityEngine;
using RailwayRenaissance.Cab.Simulation;

namespace RailwayRenaissance.Cab
{
    [RequireComponent(typeof(CabEngineState))]
    public sealed class CabSimulationDriver : MonoBehaviour
    {
        [SerializeField] SimulationConfig configuration=new SimulationConfig();
        [SerializeField] int noWorldSeedReviewFallback=20261021;
        [SerializeField] bool parkingBrake=true, coolingFailure, compressorFailure, wetRail;
        [SerializeField,Range(-40,40)] float gradePermille;
        CabEngineState facade;
        CabStartupControls startup;
        CabInteriorLighting interiorLights;
        CabExteriorLighting exteriorLights;
        SimulationClock clock=new SimulationClock();
        Font font;
        public Rect ReviewPanelRect=>new Rect(16,Mathf.Max(265,Screen.height-370),470,354);
        public bool BlocksPointer(Vector2 screenPoint)=>ReviewPanelRect.Contains(new Vector2(screenPoint.x,Screen.height-screenPoint.y));
        public CabSimulation Model {get;private set;}
        public SimulationInput Input {get;private set;}=new SimulationInput();
        public bool ParkingBrake {get=>parkingBrake;set=>parkingBrake=value;}
        public void ConfigureReviewSeed(int seed) {noWorldSeedReviewFallback=seed;}
        void OnEnable()
        {
            facade=GetComponent<CabEngineState>();
            if(facade.DetailedSimulation!=null&&facade.DetailedSimulation!=this)throw new InvalidOperationException("Multiple simulation writers");
            facade.DetailedSimulation=this;
        }
        void OnDisable() {if(facade!=null&&facade.DetailedSimulation==this)facade.DetailedSimulation=null;}
        public void EnsureInitialized()
        {
            if(Model!=null)return;
            facade=GetComponent<CabEngineState>();startup=GetComponentInChildren<CabStartupControls>(true);
            interiorLights=GetComponentInParent<CabInteriorLighting>();exteriorLights=GetComponentInParent<CabExteriorLighting>();
            string seed=GameData.CurrentSeed?.seedCode??GameData.CurrentSeed?.seedId;
            int value=noWorldSeedReviewFallback;
            if(!string.IsNullOrEmpty(seed)){unchecked{uint hash=2166136261;foreach(char c in seed){hash^=c;hash*=16777619;}value=(int)hash;}}
            Model=new CabSimulation(value,configuration);
            facade.ApplySimulation(Model.State);
        }
        public void Advance(double seconds)
        {
            EnsureInitialized();facade.UseDebugSpeedOverride=false;
            Input.Master=startup!=null&&startup.MasterEnabled;
            Input.FuelPump=facade.FuelPumpOn;Input.EngineRun=facade.IsEngineStarted;
            Input.Direction=(int)facade.Reverser;Input.Throttle=facade.ThrottleNotch;Input.Brake=(int)facade.BrakePos;
            Input.IndependentBrake=facade.IndependentBrakeNotch;Input.Sand=facade.SandOn;
            Input.ParkingBrake=parkingBrake;Input.GradePermille=gradePermille;Input.RailGrip=wetRail?.55:1;
            Input.CoolingFailure=coolingFailure;Input.CompressorFailure=compressorFailure;
            clock.Advance(seconds,()=>Model.Step(Input));facade.ApplySimulation(Model.State);
            ApplyPower();
        }
        void ApplyPower()
        {
            bool generated=Model.State.Running&&Model.State.Rpm>=configuration.IdleRpm*.9;
            bool normal=Input.Master&&generated,backup=Input.Master&&Model.State.Battery>.2;
            if(interiorLights!=null&&(interiorLights.MainPower!=normal||interiorLights.BatteryPower!=backup))interiorLights.SetSupply(normal,backup);
            if(exteriorLights!=null&&exteriorLights.PowerAvailable!=(normal||backup))exteriorLights.SetPowerAvailable(normal||backup);
        }
        [Serializable] public sealed class Checkpoint
        {
            public int version=2; public string exactConfig,exactState,exactInput,remainder;
        }
        public string CaptureCheckpoint()
        {EnsureInitialized();return JsonUtility.ToJson(new Checkpoint{exactConfig=SimulationCodec.Encode(configuration),exactState=SimulationCodec.Encode(Model.State),exactInput=SimulationCodec.Encode(Input),remainder=clock.Remainder.ToString("R",System.Globalization.CultureInfo.InvariantCulture)},true);}
        // Restore via a cold/paused scene; global game slot integration is intentionally separate.
        public void RestoreCheckpoint(string json)
        {
            var saved=JsonUtility.FromJson<Checkpoint>(json);
            if(saved==null||saved.version!=2||!double.TryParse(saved.remainder,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double remainder)||double.IsNaN(remainder)||double.IsInfinity(remainder)||remainder<0||remainder>=CabSimulation.StepSeconds)
                throw new ArgumentException("Invalid simulation checkpoint");
            var config=SimulationCodec.Decode<SimulationConfig>(saved.exactConfig);var input=SimulationCodec.Decode<SimulationInput>(saved.exactInput);var state=SimulationCodec.Decode<SimulationState>(saved.exactState);
            input.Validate();var candidate=new CabSimulation(state.Seed,config);candidate.Restore(state);
            EnsureInitialized();
            if(startup!=null&&startup.MasterEnabled!=input.Master)throw new InvalidOperationException("Restore requires matching key/master state in the paused cockpit");
            configuration=config;Model=candidate;Input=input;clock.Remainder=remainder;
            parkingBrake=Input.ParkingBrake;coolingFailure=Input.CoolingFailure;compressorFailure=Input.CompressorFailure;
            gradePermille=(float)Input.GradePermille;wetRail=Input.RailGrip<1;
            GetComponent<CabEngineState>().RestoreSimulationCommands(Input);
            GetComponent<CabEngineState>().ApplySimulation(Model.State);
            ApplyPower();
        }
        void OnGUI()
        {
            EnsureInitialized();var s=Model.State;
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");var old=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            GUILayout.BeginArea(ReviewPanelRect,GUI.skin.box);
            GUILayout.Label("NF-5K 模拟后端 · 候选车型 / 50 Hz");
            GUILayout.Label($"{(s.Cranking?"启动中":s.Running?"运转":"停机")}  {s.SpeedMps*3.6:F1} km/h  {s.Rpm:F0} r/min  水温 {s.CoolantC:F1}℃");
            GUILayout.Label($"总风 {s.MainKpa:F0}  管压 {s.PipeKpa:F0}  制动缸 {s.LocoCylinderKpa:F0} kPa");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("插钥匙"))startup?.InsertKey();
            if(GUILayout.Button("开主控"))startup?.SetMaster(true);
            if(GUILayout.Button("开油泵"))startup?.SetFuelPump(true);
            if(GUILayout.Button("启动"))startup?.TryStartEngine();
            if(GUILayout.Button("停机"))startup?.StopEngine();GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("后退"))facade.SetReverser(ReverserPosition.Reverse);
            if(GUILayout.Button("中立"))facade.SetReverser(ReverserPosition.Neutral);
            if(GUILayout.Button("前进"))facade.SetReverser(ReverserPosition.Forward);
            if(GUILayout.Button("牵引 −"))facade.ThrottleDown();GUILayout.Label(facade.ThrottleNotch.ToString());
            if(GUILayout.Button("牵引 +"))facade.ThrottleUp();GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();string[] labels={"缓解","运转","初制","全制","紧急"};
            for(int i=0;i<5;i++)if(GUILayout.Button(labels[i]))facade.SetBrakeNotch((BrakeNotch)i);GUILayout.EndHorizontal();
            GUILayout.Label("独立制动");facade.SetIndependentBrake(GUILayout.HorizontalSlider(facade.IndependentBrakeNotch,0,1));
            parkingBrake=GUILayout.Toggle(parkingBrake,"停放制动（冷车默认施加）");
            GUILayout.BeginHorizontal();wetRail=GUILayout.Toggle(wetRail,"低黏着");coolingFailure=GUILayout.Toggle(coolingFailure,"冷却故障");compressorFailure=GUILayout.Toggle(compressorFailure,"空压机故障");GUILayout.EndHorizontal();
            GUILayout.Label($"坡度 {gradePermille:F0}‰  牵引 {s.TractionN/1000:F1} kN  制动 {s.BrakeN/1000:F1} kN");
            gradePermille=GUILayout.HorizontalSlider(gradePermille,-40,40);
            GUILayout.Label(s.ThermalTrip?"过热保护：停机冷却后复位":facade.LastProcedureWarning);
            if(s.ThermalTrip&&GUILayout.Button("复位热保护"))Model.ResetThermalTrip();
            GUILayout.EndArea();GUI.skin.font=old;
        }
    }
}
