using System;

namespace RailwayRenaissance.Cab.Research
{
    public enum AtoMethod { FixedPoint, DistanceRule, Predictive, EarlyPredictive, AdaptivePredictive }
    public struct AtoObservation
    {
        public long Tick;
        public double Position, Speed, Acceleration, PipeKpa, ControlKpa, CylinderKpa;
    }
    public struct AtoCommand
    {
        public int Throttle, Brake;
        public string Reason;
        public AtoCommand(int throttle, int brake, string reason) { Throttle=throttle;Brake=brake;Reason=reason; }
    }
    // Independent research controller. No access to hidden plant mass, grip or future inputs.
    public sealed class CabAtoController
    {
        public AtoMethod Method {get;}
        public double Target {get;}
        public double SpeedLimit {get;}
        public double EstimatedDeceleration {get;private set;}=1.10;
        public int PlansLastUpdate {get;private set;}
        public bool BrakingPhase {get;private set;}
        readonly double fixedDistance;
        long lastTick=-1;
        int lastBrake=1;
        int calibrationUpdates;
        public CabAtoController(AtoMethod method,double target,double speedLimit,double fixedDistance=180)
        {
            if(!Enum.IsDefined(typeof(AtoMethod),method)||!Finite(target)||!Finite(speedLimit)||speedLimit<=0||!Finite(fixedDistance)||fixedDistance<=0)throw new ArgumentException("Invalid ATO task");
            Method=method;Target=target;SpeedLimit=speedLimit;this.fixedDistance=fixedDistance;
        }
        static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        static double Clamp(double v,double lo,double hi)=>Math.Max(lo,Math.Min(hi,v));
        public AtoCommand Decide(AtoObservation o)
        {
            if(!Finite(o.Position)||!Finite(o.Speed)||!Finite(o.Acceleration)||!Finite(o.PipeKpa)||!Finite(o.ControlKpa)||!Finite(o.CylinderKpa)||o.Tick<=lastTick)
                throw new ArgumentException("Invalid or non-increasing ATO observation");
            lastTick=o.Tick;PlansLastUpdate=0;
            double remaining=Target-o.Position;
            if(o.Speed<-.05)return new AtoCommand(0,3,"反向移动，退出正常运行");
            if(remaining<=0)return new AtoCommand(0,3,"已到目标或越界，保持制动");
            if(Method==AtoMethod.AdaptivePredictive&&o.CylinderKpa>140&&o.Speed>2&&o.Acceleration<-.1)
            {
                double response=(-o.Acceleration-.03)/(o.CylinderKpa/380);
                EstimatedDeceleration=Clamp(.8*EstimatedDeceleration+.2*Clamp(response,.35,1.8),.35,1.8);
            }
            double ruleDistance=o.Speed*o.Speed/(2*1.10)+o.Speed*2.5;
            double trigger=Method==AtoMethod.FixedPoint?fixedDistance:ruleDistance+35;
            bool early=Method==AtoMethod.AdaptivePredictive||Method==AtoMethod.EarlyPredictive;
            if(early)trigger=o.Speed*o.Speed/(2*.45)+o.Speed*2.5+35;
            if(!BrakingPhase&&remaining<=trigger)
            {BrakingPhase=true;if(early)calibrationUpdates=30;}
            if(!BrakingPhase)
            {
                if(o.Speed>SpeedLimit+.25)return new AtoCommand(0,2,"超过目标速度，减速");
                int throttle=o.Speed<SpeedLimit-2?5:o.Speed<SpeedLimit-.4?2:0;
                return new AtoCommand(throttle,1,throttle>0?"跟踪目标速度":"接近目标速度，惰行");
            }
            if(o.Speed<.01)return new AtoCommand(0,3,"已停稳，保持制动");
            if(calibrationUpdates>0){calibrationUpdates--;lastBrake=2;return new AtoCommand(0,2,"提前施加常用制动，采样有效减速度");}
            if(Method==AtoMethod.FixedPoint)return new AtoCommand(0,3,"固定制动点");
            if(Method==AtoMethod.DistanceRule)return new AtoCommand(0,remaining<=ruleDistance?3:1,"恒减速度距离规则");
            double gain=Method==AtoMethod.AdaptivePredictive?EstimatedDeceleration:1.10;
            double best=double.PositiveInfinity;int selected=3;
            // Three one-second blocks, final block held to a terminal stop: 3*3*2=18 plans.
            foreach(int first in new[]{1,2,3})foreach(int second in new[]{1,2,3})foreach(int final in new[]{2,3})
            {
                PlansLastUpdate++;
                double x=o.Position,v=Math.Max(0,o.Speed),pipe=o.PipeKpa,cylinder=o.CylinderKpa;
                double score=0;bool stopped=false;int prior=lastBrake;
                int[] plan={first,second,final};
                for(int block=0;block<3;block++){if(plan[block]!=prior)score+=.15;prior=plan[block];}
                for(int k=0;k<600;k++)
                {
                    int brake=k<5?first:k<10?second:final;
                    double target=brake==3?350:brake==2?430:500;
                    pipe+=Clamp(target-pipe,-35*.2,70*.2);
                    double targetCylinder=Clamp((o.ControlKpa-pipe)*2.6,0,380);
                    cylinder+=Clamp(targetCylinder-cylinder,-150*.2,120*.2);
                    double next=Math.Max(0,v-(gain*cylinder/380+.03)*.2);
                    x+=(v+next)*.1;v=next;
                    if(v<.01){stopped=true;break;}
                }
                double error=x-Target;
                score+=error*error*(error>0?4:1)+(stopped?0:1e6);
                if(score<best-1e-9){best=score;selected=first;}
            }
            lastBrake=selected;
            return new AtoCommand(0,selected,"预测停车距离，滚动选择常用制动");
        }
    }
}
