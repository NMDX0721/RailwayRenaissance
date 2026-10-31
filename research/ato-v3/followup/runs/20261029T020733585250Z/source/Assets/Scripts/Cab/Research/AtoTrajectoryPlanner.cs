using System;
using RailwayRenaissance.Cab.Research;
namespace RailwayRenaissance.Cab.Research
{
    // Spatial speed envelope for a candidate 900 m run. This is a reference,
    // not a promise that the simulated air brake can execute the trajectory.
    public sealed class AtoTrajectoryReference
    {
        public readonly double[] Position,Speed,Time;
        public double PredictedArrival => Time[Time.Length-1];
        public AtoTrajectoryReference(double[] x,double[] v,double[] t){Position=x;Speed=v;Time=t;}
    }
    public static class AtoTrajectoryPlanner
    {
        public static AtoTrajectoryReference Plan(AtoRouteTask task,double cruise,double acceleration,double deceleration,double initialSpeed=0)
        {
            if(task==null||!Finite(cruise)||!Finite(acceleration)||!Finite(deceleration)||cruise<=0||acceleration<=0||deceleration<=0||!Finite(initialSpeed)||initialSpeed<0||initialSpeed>task.LimitAt(task.Origin)||task.Distance>10000)throw new ArgumentException("Invalid trajectory parameters");
            int n=(int)Math.Ceiling(task.Distance);var x=new double[n+1];var v=new double[n+1];var t=new double[n+1];
            for(int i=0;i<=n;i++){x[i]=task.Origin+task.Distance*i/n;double initialBrakingEnvelope=Math.Sqrt(Math.Max(0,initialSpeed*initialSpeed-2*deceleration*(x[i]-task.Origin)));v[i]=Math.Min(Math.Max(cruise,initialBrakingEnvelope),task.LimitAt(x[i]));}
            v[0]=initialSpeed;v[n]=0;
            for(int i=1;i<=n;i++){double dx=x[i]-x[i-1];v[i]=Math.Min(v[i],Math.Sqrt(v[i-1]*v[i-1]+2*acceleration*dx));}
            for(int i=n-1;i>=0;i--){double dx=x[i+1]-x[i];v[i]=Math.Min(v[i],Math.Sqrt(v[i+1]*v[i+1]+2*deceleration*dx));}
            if(v[0]<initialSpeed-1e-8)throw new InvalidOperationException("Initial speed cannot meet the route envelope");
            for(int i=1;i<=n;i++){double sum=v[i-1]+v[i];if(sum<=0)throw new InvalidOperationException("Stationary trajectory segment");t[i]=t[i-1]+2*(x[i]-x[i-1])/sum;}
            return new AtoTrajectoryReference(x,v,t);
        }
        // Monotonic cruise-speed search; reports infeasibility instead of inventing a solution.
        public static AtoTrajectoryReference PlanForArrival(AtoRouteTask task,double targetSeconds,double acceleration,double deceleration,double initialSpeed=0)
        {
            if(task==null||!Finite(targetSeconds)||targetSeconds<=0)throw new ArgumentException("Invalid arrival target");
            var fast=Plan(task,30,acceleration,deceleration,initialSpeed);
            if(fast.PredictedArrival>targetSeconds)throw new InvalidOperationException("Arrival target is faster than this reference model permits");
            double lo=.05,hi=30;
            var slow=Plan(task,lo,acceleration,deceleration,initialSpeed);
            if(slow.PredictedArrival<targetSeconds)throw new InvalidOperationException("Arrival target exceeds reference model range");
            for(int i=0;i<60;i++){double middle=(lo+hi)*.5;var candidate=Plan(task,middle,acceleration,deceleration,initialSpeed);if(candidate.PredictedArrival>targetSeconds)lo=middle;else hi=middle;}
            return Plan(task,hi,acceleration,deceleration,initialSpeed);
        }
        // Discrete reference-profile search. The air score is only a pressure-loss proxy.
        public static AtoTrajectoryReference PlanForMode(AtoRouteTask task,AtoObjective mode,double initialSpeed=0)
        {
            if(!Enum.IsDefined(typeof(AtoObjective),mode))throw new ArgumentException("Invalid objective");
            AtoTrajectoryReference best=null;double bestScore=double.PositiveInfinity;
            foreach(double acceleration in new[]{.3,.4,.5})foreach(double deceleration in new[]{.35,.45,.55})
            {
                AtoTrajectoryReference candidate;
                try{candidate=PlanForArrival(task,task.TargetSeconds,acceleration,deceleration,initialSpeed);}catch(InvalidOperationException){continue;}
                double jerk=0,air=0,pipe=500,cylinder=0,previousAcceleration=0,schedule=0;
                for(int i=1;i<candidate.Time.Length;i++){
                    double dt=candidate.Time[i]-candidate.Time[i-1];double a=(candidate.Speed[i]-candidate.Speed[i-1])/dt;
                    jerk+=(a-previousAcceleration)*(a-previousAcceleration)/dt;previousAcceleration=a;
                    double pipeTarget=a<-.3?350:a<-.005?430:500,old=pipe;
                    pipe+=Math.Max(-35*dt,Math.Min(70*dt,pipeTarget-pipe));air+=Math.Max(0,old-pipe)*100;
                    old=cylinder;double cylinderTarget=Math.Max(0,Math.Min(380,(500-pipe)*2.6));cylinder+=Math.Max(-150*dt,Math.Min(120*dt,cylinderTarget-cylinder));air+=Math.Max(0,old-cylinder)*40;
                    schedule+=Math.Abs(candidate.Time[i]-task.TargetSeconds*(candidate.Position[i]-task.Origin)/task.Distance);
                }
                double score=mode==AtoObjective.Smooth?jerk:mode==AtoObjective.AirSaving?air:schedule;
                if(score<bestScore){bestScore=score;best=candidate;}
            }
            return best??throw new InvalidOperationException("No reference profile reaches the target time");
        }
        static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    }
}
