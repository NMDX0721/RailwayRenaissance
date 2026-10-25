using System;
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
        public bool Active {get;private set;}
        public string Status {get;private set;}="研究自动驾驶未接管";
        public double SignedErrorMeters=>driver?.Model==null||controller==null?0:driver.Model.State.PositionM-controller.Target;
        CabSimulationDriver driver;CabEngineState facade;CabAtoController controller;
        int lastThrottle,lastBrake;long lastControl=-1;int heldTicks;
        bool hadCommand;
        Font font;
        public bool BlocksPointer(Vector2 guiPoint)=>ShowPanel&&new Rect(16,16,470,160).Contains(guiPoint);
        void OnEnable()
        {
            driver=GetComponent<CabSimulationDriver>();facade=GetComponent<CabEngineState>();
            driver.BeforeSimulationStep+=OnStep;
        }
        void OnDisable(){if(driver!=null)driver.BeforeSimulationStep-=OnStep;TakeOver();}
        public bool Engage()
        {
            driver.EnsureInitialized();var s=driver.Model.State;
            if(!s.Running||s.PipeKpa<470||s.MainKpa<600||driver.ParkingBrake||facade.Reverser!=ReverserPosition.Forward)
            {Status="请先启机、充风、前进换向并缓解停放制动";return false;}
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
            heldTicks=controller.BrakingPhase&&Math.Abs(s.SpeedMps)<.01?heldTicks+1:0;
            if(heldTicks>=100){facade.SetThrottle(0);facade.SetBrakeNotch(BrakeNotch.Full);Active=false;Status=$"停车结束，误差 {SignedErrorMeters:F2} m";return;}
            if(lastControl>=0&&tick-lastControl<5)return;
            lastControl=tick;
            try
            {
                var command=controller.Decide(new AtoObservation{Tick=tick,Position=s.PositionM,Speed=s.SpeedMps,Acceleration=s.Acceleration,PipeKpa=s.PipeKpa,ControlKpa=s.ControlKpa,CylinderKpa=s.TrainCylinderKpa});
                if(command.Throttle==0)facade.SetThrottle(0);
                facade.SetBrakeNotch((BrakeNotch)command.Brake);
                bool accepted=facade.SetThrottle(command.Throttle);
                lastThrottle=facade.ThrottleNotch;lastBrake=(int)facade.BrakePos;hadCommand=true;
                Status=accepted?command.Reason:"联锁拒绝牵引，等待缓解";
            }
            catch(Exception e){facade.SetThrottle(0);facade.SetBrakeNotch(BrakeNotch.Full);Active=false;Status="自动控制退出："+e.Message;}
        }
        void OnGUI()
        {
            if(!ShowPanel)return;
            if(font==null)font=Resources.Load<Font>("Fonts/SarasaMonoSC-Regular");
            var previousFont=GUI.skin.font;if(font!=null)GUI.skin.font=font;
            GUILayout.BeginArea(new Rect(16,16,470,160),GUI.skin.box);
            GUILayout.Label("研究自动驾驶 · 简化模型原型");GUILayout.Label(Status);
            if(Active){if(GUILayout.Button("人工接管"))TakeOver();}
            else if(GUILayout.Button("准备完成后启用自动运行"))Engage();
            if(controller!=null)GUILayout.Label($"目标误差 {SignedErrorMeters:F2}m · 估计减速度 {controller.EstimatedDeceleration:F2}m/s²");
            GUILayout.EndArea();
            GUI.skin.font=previousFont;
        }
    }
}
