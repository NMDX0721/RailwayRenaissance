using System;
using System.IO;
using UnityEngine;
using RailwayRenaissance.Cab.Simulation;

namespace RailwayRenaissance.Cab.Research
{
    [RequireComponent(typeof(CabSimulationDriver),typeof(CabEngineState))]
    [DisallowMultipleComponent]
    public sealed class CabAtoResearchDriver : MonoBehaviour
    {
        public AtoMethod Method=AtoMethod.AdaptivePredictive;
        public float RelativeTargetMeters=900,TargetSpeedMps=16;
        public double SwitchPenalty=.15;
        public int MinimumBrakeTicks=25;
        public bool ShowPanel=true;
        public bool UseRouteController=true,AdaptiveV2=true;
        public AtoObjective Objective=AtoObjective.Smooth;
        CabAtoControllerV2 routeController;
        public AtoV2State RouteState=>routeController?.Snapshot;
        string CheckpointPath=>Path.Combine(Application.persistentDataPath,"AtoResearch","checkpoint-v2.json");
        public void SaveResearchFile(string path){string json=CaptureResearchCheckpoint();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temporary=path+".tmp";File.WriteAllText(temporary,json);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
        public void LoadResearchFile(string path){RestoreResearchCheckpoint(File.ReadAllText(path));}
        public void SelectMode(AtoObjective mode){Objective=mode;routeController?.RequestMode(mode);}
        public bool Active {get;private set;}
        public string Status {get;private set;}="研究自动驾驶未接管";
        public double SignedErrorMeters=>driver?.Model==null?0:driver.Model.State.PositionM-(UseRouteController&&routeController!=null?routeController.Task.Target:controller?.Target??driver.Model.State.PositionM);
        CabSimulationDriver driver;CabEngineState facade;CabAtoController controller;
        int lastThrottle,lastBrake;long lastControl=-1;int heldTicks;
        bool hadCommand;
        Font font;

        public bool BlocksPointer(Vector2 guiPoint)=>ShowPanel&&new Rect(16,16,470,240).Contains(guiPoint);
        void OnEnable()
        {
            driver=GetComponent<CabSimulationDriver>();facade=GetComponent<CabEngineState>();
            driver.BeforeSimulationStep+=OnStep;
            driver.CheckpointRestoring+=Relinquish;
            CabManualControlIntent.Requested+=ManualRequested;
        }
        void OnDisable(){CabManualControlIntent.Requested-=ManualRequested;if(driver!=null){driver.BeforeSimulationStep-=OnStep;driver.CheckpointRestoring-=Relinquish;}TakeOver();}
        void ManualRequested(CabEngineState owner){if(owner==facade)Relinquish();}
        [Serializable] public sealed class ResearchCheckpoint
        {
            public int version;
            public string physics,controller,adapter,route,v2;
            public bool useRoute,adaptive;public int objective;
        }
        public void SetPaused(bool paused){driver.SimulationPaused=paused;}
        public string CaptureResearchCheckpoint()
        {
            if(!driver.SimulationPaused)throw new InvalidOperationException("Pause before saving research state");
            var state=new AtoAdapterState{Active=Active,HadCommand=hadCommand,LastControl=lastControl,LastThrottle=lastThrottle,LastBrake=lastBrake,HeldTicks=heldTicks,Method=(int)Method,RelativeTarget=RelativeTargetMeters,TargetSpeed=TargetSpeedMps,Penalty=SwitchPenalty,MinimumBrakeTicks=MinimumBrakeTicks};
            return JsonUtility.ToJson(new ResearchCheckpoint{version=2,useRoute=UseRouteController,adaptive=AdaptiveV2,objective=(int)Objective,route=routeController?.Task.Encode(),v2=routeController==null?null:SimulationCodec.Encode(routeController.Snapshot),physics=driver.CaptureCheckpoint(),controller=controller==null?null:SimulationCodec.Encode(CabAtoCheckpoint.Capture(controller)),adapter=SimulationCodec.Encode(state)});
        }
        public void RestoreResearchCheckpoint(string json)
        {
            if(!driver.SimulationPaused)throw new InvalidOperationException("Pause before restoring research state");
            var pack=JsonUtility.FromJson<ResearchCheckpoint>(json);
            if(pack==null||(pack.version!=1&&pack.version!=2))throw new ArgumentException("Unsupported research checkpoint");
            CabSimulationDriver.ValidateCheckpoint(pack.physics);
            var physics=JsonUtility.FromJson<CabSimulationDriver.Checkpoint>(pack.physics);
            var physicalState=SimulationCodec.Decode<SimulationState>(physics.exactState);
            var adapter=SimulationCodec.Decode<AtoAdapterState>(pack.adapter);CabAtoCheckpoint.ValidateAdapter(adapter,physicalState.Tick);
            var candidate=string.IsNullOrEmpty(pack.controller)?null:CabAtoCheckpoint.Restore(SimulationCodec.Decode<AtoLegacyState>(pack.controller));
            CabAtoControllerV2 routeCandidate=null;
            if(pack.version==2){if(pack.objective<0||pack.objective>2)throw new ArgumentException("Invalid objective");if(!string.IsNullOrEmpty(pack.v2))routeCandidate=CabAtoControllerV2.Restore(AtoRouteTask.Decode(pack.route),SimulationCodec.Decode<AtoV2State>(pack.v2),physicalState.Tick);}
            if(adapter.Active&&(pack.version==2&&pack.useRoute?routeCandidate==null:candidate==null))throw new ArgumentException("Active checkpoint lacks controller");
            if(routeCandidate!=null&&adapter.Active&&routeCandidate.Snapshot.LastTick!=adapter.LastControl)throw new ArgumentException("Inconsistent V2 schedule");
            if(candidate!=null&&CabAtoCheckpoint.Capture(candidate).LastTick>physicalState.Tick)throw new ArgumentException("Controller ahead of physics");
            driver.RestoreCheckpoint(pack.physics);
            controller=candidate;routeController=routeCandidate;UseRouteController=pack.version==2&&pack.useRoute;AdaptiveV2=pack.adaptive;Objective=(AtoObjective)pack.objective;Active=adapter.Active;hadCommand=adapter.HadCommand;lastControl=adapter.LastControl;
            lastThrottle=adapter.LastThrottle;lastBrake=adapter.LastBrake;heldTicks=adapter.HeldTicks;Status="研究状态已恢复（暂停）";
            Method=(AtoMethod)adapter.Method;RelativeTargetMeters=(float)adapter.RelativeTarget;TargetSpeedMps=(float)adapter.TargetSpeed;SwitchPenalty=adapter.Penalty;MinimumBrakeTicks=adapter.MinimumBrakeTicks;
        }
        public bool Engage()
        {
            driver.EnsureInitialized();var s=driver.Model.State;
            if(!s.Running||s.PipeKpa<470||s.MainKpa<600||driver.ParkingBrake||facade.Reverser!=ReverserPosition.Forward)
            {Status="请先启机、充风、前进换向并缓解停放制动";return false;}
            routeController=UseRouteController?new CabAtoControllerV2(AtoRouteTask.Standard(s.PositionM),s.Tick,Objective,AdaptiveV2):null;
            controller=new CabAtoController(Method,s.PositionM+RelativeTargetMeters,TargetSpeedMps,180,SwitchPenalty,MinimumBrakeTicks);
            lastControl=-1;hadCommand=false;heldTicks=0;Active=true;Status="自动运行已接管";return true;
        }
        public void TakeOver(){if(Active&&facade!=null)facade.SetThrottle(0);Relinquish();}
        void Relinquish(){Active=false;hadCommand=false;Status="人工接管，自动命令停止";}
        void OnStep(long tick)
        {
            if(!Active)return;
            // Physical/manual command changes always relinquish ownership before the next step.
            if(hadCommand&&(facade.ThrottleNotch!=lastThrottle||(int)facade.BrakePos!=lastBrake)||
                !facade.IsEngineRunning||driver.ParkingBrake||facade.Reverser!=ReverserPosition.Forward||facade.IndependentBrakeNotch>0)
            {Relinquish();return;}
            var s=driver.Model.State;
            heldTicks=(UseRouteController?routeController.Arrived:controller.BrakingPhase)&&Math.Abs(s.SpeedMps)<.01?heldTicks+1:0;
            if(heldTicks>=100){facade.SetThrottle(0);facade.SetBrakeNotch(BrakeNotch.Full);Active=false;Status=$"停车结束，误差 {SignedErrorMeters:F2} m";return;}
            if(lastControl>=0&&tick-lastControl<5)return;
            lastControl=tick;
            try
            {
                var observation=new AtoObservation{Tick=tick,Position=s.PositionM,Speed=s.SpeedMps,Acceleration=s.Acceleration,PipeKpa=s.PipeKpa,ControlKpa=s.ControlKpa,CylinderKpa=s.TrainCylinderKpa};
                var command=UseRouteController?routeController.Decide(observation):controller.Decide(observation);
                if(command.Throttle==0)facade.SetThrottle(0);
                facade.SetBrakeNotch((BrakeNotch)command.Brake);
                bool accepted=facade.SetThrottle(command.Throttle);
                lastThrottle=facade.ThrottleNotch;lastBrake=(int)facade.BrakePos;hadCommand=true;
                if(UseRouteController&&routeController.Intervention)Active=false;
                Status=accepted?command.Reason.Replace("Smooth","平稳").Replace("AirSaving","省气").Replace("Punctual","准点"):"联锁拒绝牵引，等待缓解";
            }
            catch(Exception e){facade.SetThrottle(0);facade.SetBrakeNotch(BrakeNotch.Full);Active=false;Status="自动控制退出："+e.Message;}
        }
        void OnGUI()
        {
            if(!ShowPanel)return;
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var previousFont=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            GUILayout.BeginArea(new Rect(16,16,470,240),GUI.skin.box);
            GUILayout.Label("研究自动驾驶 · 简化模型原型");GUILayout.Label(Status);
            if(Active){if(GUILayout.Button("人工接管"))TakeOver();}
            else if(GUILayout.Button("准备完成后启用自动运行"))Engage();
            GUILayout.BeginHorizontal();
            foreach(AtoObjective mode in Enum.GetValues(typeof(AtoObjective)))if(GUILayout.Button(mode==AtoObjective.Smooth?"平稳":mode==AtoObjective.AirSaving?"省气":"准点"))SelectMode(mode);
            GUILayout.EndHorizontal();
            if(UseRouteController&&routeController!=null&&driver.Model!=null){var rs=routeController.Snapshot;GUILayout.Label($"限速 {routeController.Task.LimitAt(driver.Model.State.PositionM):F1}m/s · 剩余 {-SignedErrorMeters:F1}m · 预计偏差 {rs.PredictedArrival-120:F1}s");}
            if(controller!=null)GUILayout.Label($"目标误差 {SignedErrorMeters:F2}m · 估计减速度 {(UseRouteController&&routeController!=null?routeController.Snapshot.Gain:controller.EstimatedDeceleration):F2}m/s²");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(driver.SimulationPaused?"继续":"暂停"))SetPaused(!driver.SimulationPaused);
            if(driver.SimulationPaused&&GUILayout.Button("保存研究检查点")){SaveResearchFile(CheckpointPath);}
            if(driver.SimulationPaused&&File.Exists(CheckpointPath)&&GUILayout.Button("恢复"))LoadResearchFile(CheckpointPath);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.skin.font=previousFont;
        }
    }
}
