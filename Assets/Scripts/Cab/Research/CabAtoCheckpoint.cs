using System;
using System.Reflection;
using RailwayRenaissance.Cab.Simulation;

namespace RailwayRenaissance.Cab.Research
{
    [Serializable] public sealed class AtoLegacyState
    {
        public int Version=1,Method,MinimumBrakeTicks,LastBrake,CalibrationUpdates,Plans;
        public double Target,SpeedLimit,FixedDistance,SwitchPenalty,Gain;
        public long LastTick,LastSwitchTick;
        public bool Braking;
    }
    [Serializable] public sealed class AtoAdapterState
    {
        public int Version=1,LastThrottle,LastBrake,HeldTicks;
        public long LastControl;
        public bool Active,HadCommand;
        public int Method,MinimumBrakeTicks;
        public double RelativeTarget=900,TargetSpeed=16,Penalty=.15;
    }
    // Sidecar keeps the published V1 controller byte-for-byte unchanged.
    // Private-field names are a versioned V1 compatibility contract, tested in .NET and Unity Mono.
    public static class CabAtoCheckpoint
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static T Get<T>(CabAtoController c,string name)=>(T)typeof(CabAtoController).GetField(name,Private).GetValue(c);
        static void Set(CabAtoController c,string name,object value)=>typeof(CabAtoController).GetField(name,Private).SetValue(c,value);
        static void Property(CabAtoController c,string name,object value)=>typeof(CabAtoController).GetProperty(name).GetSetMethod(true).Invoke(c,new[]{value});
        public static AtoLegacyState Capture(CabAtoController c)
        {
            if(c==null)throw new ArgumentNullException(nameof(c));
            return new AtoLegacyState{Method=(int)c.Method,Target=c.Target,SpeedLimit=c.SpeedLimit,
                FixedDistance=Get<double>(c,"fixedDistance"),SwitchPenalty=c.SwitchPenalty,MinimumBrakeTicks=c.MinimumBrakeTicks,
                Gain=c.EstimatedDeceleration,Braking=c.BrakingPhase,Plans=c.PlansLastUpdate,
                LastTick=Get<long>(c,"lastTick"),LastSwitchTick=Get<long>(c,"lastSwitchTick"),
                LastBrake=Get<int>(c,"lastBrake"),CalibrationUpdates=Get<int>(c,"calibrationUpdates")};
        }
        public static CabAtoController Restore(AtoLegacyState s)
        {
            if(s==null||s.Version!=1||s.LastTick< -1||s.LastSwitchTick< -1||s.LastSwitchTick>s.LastTick||
                s.LastBrake<1||s.LastBrake>3||s.CalibrationUpdates<0||s.CalibrationUpdates>30||s.Plans<0||s.Plans>18||
                double.IsNaN(s.Gain)||double.IsInfinity(s.Gain)||s.Gain<.35||s.Gain>1.8)throw new ArgumentException("Invalid V1 controller checkpoint");
            var c=new CabAtoController((AtoMethod)s.Method,s.Target,s.SpeedLimit,s.FixedDistance,s.SwitchPenalty,s.MinimumBrakeTicks);
            Set(c,"lastTick",s.LastTick);Set(c,"lastSwitchTick",s.LastSwitchTick);Set(c,"lastBrake",s.LastBrake);Set(c,"calibrationUpdates",s.CalibrationUpdates);
            Property(c,"EstimatedDeceleration",s.Gain);Property(c,"BrakingPhase",s.Braking);Property(c,"PlansLastUpdate",s.Plans);return c;
        }
        public static void ValidateAdapter(AtoAdapterState s,long tick)
        {
            if(s==null||s.Version!=1||s.LastControl< -1||s.LastControl>tick||s.LastThrottle<0||s.LastThrottle>8||s.LastBrake<0||s.LastBrake>4||s.HeldTicks<0||s.HeldTicks>100||
                !Enum.IsDefined(typeof(AtoMethod),s.Method)||s.RelativeTarget<=0||double.IsNaN(s.RelativeTarget)||double.IsInfinity(s.RelativeTarget)||s.TargetSpeed<=0||double.IsNaN(s.TargetSpeed)||double.IsInfinity(s.TargetSpeed)||s.Penalty<0||double.IsNaN(s.Penalty)||double.IsInfinity(s.Penalty)||s.MinimumBrakeTicks<0||s.MinimumBrakeTicks>50)
                throw new ArgumentException("Invalid controller scheduling checkpoint");
        }
    }
}
