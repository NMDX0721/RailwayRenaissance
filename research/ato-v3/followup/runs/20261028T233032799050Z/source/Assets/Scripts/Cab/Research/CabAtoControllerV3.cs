using System;
using RailwayRenaissance.Cab.Simulation;
namespace RailwayRenaissance.Cab.Research
{
    // Research prototype: finite local action search against a spatial reference.
    public sealed class AtoV3State
    {
        public int Version=1,Mode,PendingMode,LastThrottle,LastBrake=1;
        public long StartTick,LastTick=-1,LastThrottleSwitch=-1000;
        public double Coast=-.03,BrakeGain=1.1,Arrival=-1,PredictedArrival,InitialSpeed;
        public bool Intervention;
    }
    public sealed class CabAtoControllerV3
    {
        readonly AtoRouteTask task; AtoTrajectoryReference reference; AtoV3State state;
        public AtoV3State Snapshot=>SimulationCodec.Decode<AtoV3State>(SimulationCodec.Encode(state));
        public CabAtoControllerV3(AtoRouteTask task,long tick,AtoObjective objective,double initialSpeed=0)
        {this.task=task??throw new ArgumentNullException(nameof(task));reference=AtoTrajectoryPlanner.PlanForMode(task,objective,initialSpeed);state=new AtoV3State{StartTick=tick,Mode=(int)objective,PendingMode=(int)objective,InitialSpeed=initialSpeed};Validate(state,tick);}
        public static CabAtoControllerV3 Restore(AtoRouteTask task,AtoV3State saved,long tick)
        {Validate(saved,tick);var result=new CabAtoControllerV3(task,saved.StartTick,(AtoObjective)saved.Mode,saved.InitialSpeed);result.state=SimulationCodec.Decode<AtoV3State>(SimulationCodec.Encode(saved));return result;}
        static void Validate(AtoV3State s,long tick)
        {if(s==null||s.Version!=1||s.StartTick<0||s.StartTick>tick||s.LastTick>tick||s.LastTick< -1||s.Mode<0||s.Mode>2||s.PendingMode<0||s.PendingMode>2||s.LastThrottle<0||s.LastThrottle>8||s.LastBrake<0||s.LastBrake>4||!Finite(s.Coast)||Math.Abs(s.Coast)>.3||!Finite(s.BrakeGain)||s.BrakeGain<.35||s.BrakeGain>1.8||!Finite(s.Arrival)||s.Arrival< -1||!Finite(s.InitialSpeed)||s.InitialSpeed<0||s.InitialSpeed>30||!Finite(s.PredictedArrival)||s.LastThrottleSwitch< -1000||(s.LastTick>=0&&s.LastThrottleSwitch>s.LastTick)||(s.LastTick>=0&&s.LastTick<s.StartTick))throw new ArgumentException("Invalid V3 checkpoint");}
        static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        static double Clamp(double x,double min,double max)=>Math.Max(min,Math.Min(max,x));
        public void RequestMode(AtoObjective mode){if(!Enum.IsDefined(typeof(AtoObjective),mode))throw new ArgumentException("Invalid mode");state.PendingMode=(int)mode;}
        AtoCommand Exit(long tick,string reason){state.Intervention=true;state.LastThrottle=0;state.LastBrake=3;return new AtoCommand(0,3,tick>=0?reason:"invalid tick");}
        public AtoCommand Decide(AtoObservation o,int acceptedThrottle,int acceptedBrake)
        {
            foreach(double x in new[]{o.Position,o.Speed,o.Acceleration,o.PipeKpa,o.ControlKpa,o.CylinderKpa})if(!Finite(x))throw new ArgumentException("Non-finite observation");
            if(o.Tick<=state.LastTick||o.Tick<state.StartTick||acceptedThrottle<0||acceptedThrottle>8||acceptedBrake<0||acceptedBrake>4)throw new ArgumentException("Invalid observation or accepted command");
            state.LastTick=o.Tick;if(state.Mode!=state.PendingMode){reference=AtoTrajectoryPlanner.PlanForMode(task,(AtoObjective)state.PendingMode,state.InitialSpeed);state.Mode=state.PendingMode;}
            if(state.Intervention)return new AtoCommand(0,3,"干预后保持全制动");
            if(acceptedBrake==4)return Exit(o.Tick,"检测到紧急制动，退出自动运行");
            if(acceptedThrottle!=state.LastThrottle){state.LastThrottle=acceptedThrottle;state.LastThrottleSwitch=o.Tick;}
            state.LastBrake=acceptedBrake;
            double remaining=task.Target-o.Position;
            if(o.Speed<-.05||remaining< -5||o.Speed>task.LimitAt(o.Position)+.15)return Exit(o.Tick,"越站、反向或越速：退出自动运行");
            if(Math.Abs(remaining)<=5&&Math.Abs(o.Speed)<.01){if(state.Arrival<0)state.Arrival=(o.Tick-state.StartTick)*.02;state.LastThrottle=0;state.LastBrake=3;return new AtoCommand(0,3,"停车窗内停稳");}
            if(acceptedThrottle==0&&acceptedBrake<=1&&o.CylinderKpa<5&&o.Speed>2)state.Coast=Clamp(.9*state.Coast+.1*o.Acceleration,-.3,.3);
            if(acceptedThrottle==0&&o.CylinderKpa>140&&o.Speed>2){double gain=(state.Coast-o.Acceleration)/(o.CylinderKpa/380);state.BrakeGain=Clamp(.9*state.BrakeGain+.1*Clamp(gain,.35,1.8),.35,1.8);}
            int index=(int)Clamp(Math.Round((o.Position+Math.Max(10,o.Speed*5)-task.Origin)/task.Distance*(reference.Speed.Length-1)),0,reference.Speed.Length-1);
            double target=Math.Max(reference.Speed[index],remaining>5?1.5:0),elapsed=(o.Tick-state.StartTick)*.02;
            int currentIndex=(int)Clamp(Math.Round((o.Position-task.Origin)/task.Distance*(reference.Speed.Length-1)),0,reference.Speed.Length-1);
            state.PredictedArrival=elapsed+Math.Max(0,reference.PredictedArrival-reference.Time[currentIndex]);
            double best=double.PositiveInfinity;int chosenT=0,chosenB=3,count=0;
            foreach(int throttle in new[]{0,1,2,5})foreach(int brake in new[]{1,2,3})
            {
                if(throttle>0&&brake>1)continue;
                if(throttle>state.LastThrottle&&o.Tick-state.LastThrottleSwitch<25)continue;
                bool valid=true;double nominalSpeed=0,nominalJerk=0,nominalAir=0;
                foreach(double multiplier in new[]{.75,1.0,1.25})
                {
                    double x=o.Position,v=Math.Max(0,o.Speed),p=o.PipeKpa,c=o.CylinderKpa,prev=o.Acceleration,jerk=0,air=0;
                    for(int k=0;k<25;k++)
                    {
                        double old=p;p+=Clamp((brake==3?350:brake==2?430:500)-p,-7,14);air+=Math.Max(0,old-p)*100;
                        old=c;c+=Clamp(Clamp((o.ControlKpa-p)*2.6,0,380)-c,-30,24);air+=Math.Max(0,old-c)*40;
                        double a=(throttle>0&&c<20&&p>=470?throttle*.075:0)+state.Coast-state.BrakeGain*multiplier*c/380;
                        jerk+=(a-prev)*(a-prev)/.2;prev=a;double next=Math.Max(0,v+a*.2);x+=(v+next)*.1;v=next;
                        if(v>task.LimitAt(x)+.15||x>task.Target+5){valid=false;break;}
                    }
                    // A full-service backup is simulated after the candidate prefix.
                    // This finite model is only a screening heuristic, not a safety proof.
                    if(valid){double bx=x,bv=v,bp=p,bc=c;for(int k=0;k<750&&bv>.01;k++){
                        bp+=Clamp(350-bp,-7,14);bc+=Clamp(Clamp((o.ControlKpa-bp)*2.6,0,380)-bc,-30,24);
                        double bn=Math.Max(0,bv+(state.Coast-state.BrakeGain*multiplier*bc/380)*.2);bx+=(bv+bn)*.1;bv=bn;
                        if(bv>task.LimitAt(bx)+.15||bx>task.Target+5){valid=false;break;}
                    }if(bv>.01)valid=false;}
                    if(!valid)break;if(multiplier==1){nominalSpeed=v;nominalJerk=jerk;nominalAir=air;}
                }
                if(!valid)continue;count++;
                double tracking=(nominalSpeed-target)*(nominalSpeed-target);
                double score=tracking*100+(state.Mode==0?nominalJerk*.2:state.Mode==1?nominalAir*.001:Math.Abs(elapsed+remaining/Math.Max(1,nominalSpeed)-task.TargetSeconds)*.01);
                if(score<best){best=score;chosenT=throttle;chosenB=brake;}
            }
            if(count==0)return Exit(o.Tick,"预测无可行候选：退出自动运行");
            return new AtoCommand(chosenT,chosenB,"轨迹跟踪：有限情景筛选 "+count+(state.PredictedArrival>task.TargetSeconds+task.TimeTolerance?"；预计晚点":""));
        }
    }
}



