using System;
using System.Collections.Generic;
using RailwayRenaissance.Cab.Simulation;
namespace RailwayRenaissance.Cab.Research
{
    public enum AtoObjective { Smooth, AirSaving, Punctual }
    public sealed class AtoV2State
    {
        public int Version=1,Mode,PendingMode,LastBrake=1,LastThrottle,Calibration,Feasible;
        public long StartTick,LastTick=-1,LastSwitch=-1000;
        public double Gain=1.1,ArrivalSeconds=-1,PredictedArrival;
        public bool Adaptive,Braking,Intervention;
    }
    // Finite command-sequence search. Predictor is deliberately separate from the plant.
    public sealed class CabAtoControllerV2
    {
        public AtoRouteTask Task {get;}
        AtoV2State state;
        public bool Arrived=>state.ArrivalSeconds>=0;
        public bool Intervention=>state.Intervention;
        public AtoV2State Snapshot=>SimulationCodec.Decode<AtoV2State>(SimulationCodec.Encode(state));
        public CabAtoControllerV2(AtoRouteTask task,long startTick,AtoObjective mode,bool adaptive=true)
        { Task=task??throw new ArgumentNullException(nameof(task));state=new AtoV2State{StartTick=startTick,Mode=(int)mode,PendingMode=(int)mode,Adaptive=adaptive};Validate(state); }
        public static CabAtoControllerV2 Restore(AtoRouteTask task,AtoV2State s,long physicsTick)
        {Validate(s);if(s.LastTick>physicsTick||s.StartTick>physicsTick)throw new ArgumentException("Controller ahead of physics");var c=new CabAtoControllerV2(task,s.StartTick,(AtoObjective)s.Mode,s.Adaptive);c.state=SimulationCodec.Decode<AtoV2State>(SimulationCodec.Encode(s));return c;}
        static void Validate(AtoV2State s)
        {
            if(s==null||s.Version!=1||s.Mode<0||s.Mode>2||s.PendingMode<0||s.PendingMode>2||s.LastBrake<1||s.LastBrake>3||s.LastThrottle<0||s.LastThrottle>8||s.Calibration<0||s.Calibration>30||s.StartTick<0||s.LastTick< -1||s.LastSwitch< -1000||s.LastSwitch>s.LastTick&&s.LastTick>=0||s.Feasible<0||!Finite(s.Gain)||s.Gain<.35||s.Gain>1.8||!Finite(s.ArrivalSeconds)||s.ArrivalSeconds< -1||!Finite(s.PredictedArrival))throw new ArgumentException("Invalid V2 controller state");
        }
        public void RequestMode(AtoObjective mode){if(!Enum.IsDefined(typeof(AtoObjective),mode))throw new ArgumentException("Invalid mode");state.PendingMode=(int)mode;}
        static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
        static double Clamp(double x,double a,double b)=>Math.Max(a,Math.Min(b,x));
        AtoCommand Command(int throttle,int brake,long tick,string reason)
        {if(brake!=state.LastBrake)state.LastSwitch=tick;state.LastBrake=brake;state.LastThrottle=throttle;return new AtoCommand(throttle,brake,reason);}
        AtoCommand Fail(long tick,string reason){state.Intervention=true;return Command(0,3,tick,"干预退出："+reason);}
        public AtoCommand Decide(AtoObservation o)
        {
            foreach(double x in new[]{o.Position,o.Speed,o.Acceleration,o.PipeKpa,o.ControlKpa,o.CylinderKpa})if(!Finite(x))throw new ArgumentException("Invalid observation");
            if(o.Tick<=state.LastTick||o.Tick<state.StartTick)throw new ArgumentException("Non-increasing control tick");
            state.LastTick=o.Tick;state.Mode=state.PendingMode;state.Feasible=0;
            double remaining=Task.Target-o.Position,elapsed=(o.Tick-state.StartTick)*.02;
            if(state.Intervention)return Command(0,3,o.Tick,"干预后保持常用全制动");
            if(o.Speed>Task.LimitAt(o.Position)+.15)return Fail(o.Tick,"当前已越速，预测约束不可满足");
            if(o.Speed<-.05||remaining< -5)return Fail(o.Tick,"反向或越站");
            if(Math.Abs(remaining)<=5&&Math.Abs(o.Speed)<.01)
            {if(state.ArrivalSeconds<0)state.ArrivalSeconds=elapsed;state.Braking=true;return Command(0,3,o.Tick,"已停稳，固定确认时间，不等待准点时刻");}
            if(state.Adaptive&&o.CylinderKpa>140&&o.Speed>2&&o.Acceleration<-.1)
                state.Gain=Clamp(.8*state.Gain+.2*Clamp((-o.Acceleration-.03)/(o.CylinderKpa/380),.35,1.8),.35,1.8);
            if(!state.Braking&&remaining<=o.Speed*o.Speed/(2*.45)+3*o.Speed+25&&o.Speed>2)
            {state.Braking=true;state.Calibration=30;}
            if(state.Calibration>0){state.Calibration--;return Command(0,2,o.Tick,"提前采样制动响应，固定与自适应组均执行");}
            if(state.Braking&&o.Speed<.01)return Fail(o.Tick,"停车窗外提前停稳");
            double best=double.PositiveInfinity;int bt=0,bb=3;double eta=0;
            if(state.Braking)
            {
                // Coast then common/full brake. The final brake is held to a predicted stop.
                for(int initial=1;initial<=3;initial++)for(int coast=0;coast<=200;coast++)for(int brake=2;brake<=3;brake++)
                {
                    int first=initial;
                    if(first!=state.LastBrake&&o.Tick-state.LastSwitch<25)continue;
                    double x=o.Position,v=Math.Max(0,o.Speed),p=o.PipeKpa,c=o.CylinderKpa,air=0,jerk=0,priorA=o.Acceleration,t=0;bool valid=true,stopped=false;
                    for(int k=0;k<750;k++)
                    {
                        int b=k<3?initial:(k-3)*.2<coast*.2?1:brake;
                        double oldP=p;p+=Clamp((b==3?350:b==2?430:500)-p,-7,14);
                        double oldC=c;c+=Clamp(Clamp((o.ControlKpa-p)*2.6,0,380)-c,-30,24);
                        air+=Math.Max(0,oldP-p)*100+Math.Max(0,oldC-c)*40;
                        double a=-(state.Gain*c/380+.03);jerk+=(a-priorA)*(a-priorA)/.2;priorA=a;
                        double next=Math.Max(0,v+a*.2);x+=(v+next)*.1;v=next;t+=.2;
                        if(v>Task.LimitAt(x)+.15){valid=false;break;}
                        if(v<.01){stopped=true;break;}
                    }
                    double error=x-Task.Target;
                    if(!valid||!stopped||Math.Abs(error)>4)continue;
                    state.Feasible++;
                    double switches=(first==state.LastBrake?0:1)+(coast>0?2:initial==brake?0:1);
                    double score=state.Mode==0?jerk+switches*.001:state.Mode==1?air+switches*.001:Math.Abs(elapsed+t-Task.TargetSeconds)+switches*.001;
                    score+=Math.Abs(error)*1e-6;
                    if(score<best){best=score;bt=0;bb=first;eta=elapsed+t;}
                }
            }
            else
            {
                double preview=Task.PreviewSpeed(o.Position+Math.Max(0,o.Speed)*3,.4);
                double desired=state.Mode==2?Clamp(remaining/Math.Max(1,Task.TargetSeconds-elapsed-8),4,preview):Math.Min(preview,10);
                var choices=new List<(int throttle,int brake,double tracking,double jerk,double air,double change,double arrival)>();double leastTracking=double.PositiveInfinity;
                foreach(int throttle in new[]{0,2,5})foreach(int brake in new[]{1,2,3})
                {
                    if(brake>1&&throttle>0)continue;
                    if(brake!=state.LastBrake&&o.Tick-state.LastSwitch<25)continue;
                    double x=o.Position,v=Math.Max(0,o.Speed),p=o.PipeKpa,c=o.CylinderKpa,jerk=0,air=0,a0=o.Acceleration,tracking=0;bool valid=true;
                    for(int k=0;k<25;k++)
                    {
                        double old=p;p+=Clamp((brake==3?350:brake==2?430:500)-p,-7,14);air+=Math.Max(0,old-p)*100;
                        double oldC=c;c+=Clamp(Clamp((o.ControlKpa-p)*2.6,0,380)-c,-30,24);air+=Math.Max(0,oldC-c)*40;
                        double traction=throttle>0&&c<20&&p>=470?throttle*.075:0;
                        double a=traction-state.Gain*c/380-.03;
                        jerk+=(a-a0)*(a-a0)/.2;a0=a;
                        double next=Math.Max(0,v+a*.2);x+=(v+next)*.1;v=next;
                        if(v>Task.LimitAt(x)+.15||v>Task.PreviewSpeed(x,.4)+.15){valid=false;break;}
                        tracking+=(v-desired)*(v-desired)*.2;
                    }
                    if(!valid)continue;state.Feasible++;
                    double change=throttle!=state.LastThrottle||brake!=state.LastBrake?1:0;
                    double predicted=elapsed+remaining/Math.Max(1,v)+8;
                    choices.Add((throttle,brake,tracking,jerk,air,change,predicted));leastTracking=Math.Min(leastTracking,tracking);
                }
                // Common progress band prevents a zero-air policy from choosing permanent standstill.
                foreach(var choice in choices)
                {
                    if(choice.tracking>leastTracking+4)continue;
                    double score=state.Mode==0?choice.jerk:state.Mode==1?choice.air:Math.Abs(choice.arrival-Task.TargetSeconds);
                    score+=choice.change*1e-6;
                    if(score<best){best=score;bt=choice.throttle;bb=choice.brake;eta=choice.arrival;}
                }
            }
            if(state.Feasible==0)return Fail(o.Tick,"有限候选均不满足预测限速/停车窗或驻留约束");
            state.PredictedArrival=eta;
            return Command(bt,bb,o.Tick,((AtoObjective)state.Mode)+$"：筛选{state.Feasible}个可行序列，"+(state.Braking?"停车预测":"提前限速跟踪"));
        }
    }
}



