using System;

namespace RailwayRenaissance.Cab.Simulation
{
    [Serializable] public sealed class SimulationConfig
    {
        public double TrainMassKg=160000, PoweredMassKg=60000, EnginePowerW=1200000, Efficiency=.84;
        public double StartingForceN=170000, ServiceBrakeN=180000, LocoBrakeN=65000, ParkingBrakeN=120000;
        public double DavisA=2400, DavisB=35, DavisC=4, DryAdhesion=.28;
        public double MainVolumeL=1000, PipeVolumeL=100, AuxVolumeL=300, ControlVolumeL=8, CylinderVolumeL=40;
        public double PipeNominalKpa=500, CylinderMaxKpa=380, CompressorOnKpa=750, CompressorOffKpa=900;
        public double CompressorKpaPerSecond=8, IdleRpm=600, RatedRpm=1100, StartSeconds=5, MaxTractionSpeedMps=30;
        public double HeatCapacityJPerC=5000000, FullLoadHeatW=450000, TripTemperatureC=115;
        public double BatteryCapacityWh=30000, BatteryChargeW=5000, StarterPowerW=75000;
        public double CompressorLoadW=35000;
        public void Validate()
        {
            foreach(var field in typeof(SimulationConfig).GetFields())
            {
                double x=(double)field.GetValue(this);
                if(double.IsNaN(x)||double.IsInfinity(x)||x<=0)throw new ArgumentException("Invalid configuration: "+field.Name);
            }
            if(PoweredMassKg>TrainMassKg||LocoBrakeN>ServiceBrakeN||CompressorOnKpa>=CompressorOffKpa||Efficiency>1||RatedRpm<=IdleRpm)
                throw new ArgumentException("Inconsistent vehicle configuration");
        }
    }
    [Serializable] public sealed class SimulationInput
    {
        public bool Master, FuelPump, EngineRun, ParkingBrake=true, Sand, Emergency, CoolingFailure, CompressorFailure;
        public int Direction, Throttle, Brake; // Brake: release/run/common/full/emergency = 0..4.
        public double IndependentBrake, GradePermille, RailGrip=1, AmbientC=25, AuxiliaryLoadW=2000;
        public void Validate()
        {
            foreach(double x in new[]{IndependentBrake,GradePermille,RailGrip,AmbientC,AuxiliaryLoadW})
                if(double.IsNaN(x)||double.IsInfinity(x))throw new ArgumentException("Non-finite input");
            if(Direction< -1||Direction>1||Throttle<0||Throttle>8||Brake<0||Brake>4||IndependentBrake<0||IndependentBrake>1||RailGrip<.01||RailGrip>1||Math.Abs(GradePermille)>200||AmbientC< -80||AmbientC>70||AuxiliaryLoadW<0||AuxiliaryLoadW>20000)
                throw new ArgumentException("Input out of supported range");
        }
    }
    [Serializable] public sealed class SimulationState
    {
        public int Version=1, Seed, ActiveDirection; public long Tick;
        public bool Running, Cranking, Compressor, Fan, ThermalTrip, AdhesionLimited, EngineRequested;
        public double Time, SpeedMps, PositionM, DistanceM, StopPositionM, Acceleration, Rpm, Spool, CrankTime;
        public double CoolantC, OilC, OilMpa, FuelL, Battery, TrainMassKg;
        public double MainKpa, PipeKpa, AuxKpa, ControlKpa, TrainCylinderKpa, LocoCylinderKpa;
        public double TractionN, BrakeN, ResistanceN, GradeN, RailPowerW, CurrentA, FuelUsedL;
        public double CompressorAirKpaL, ExhaustAirKpaL;
        public SimulationState Copy() { return (SimulationState)MemberwiseClone(); }
    }
    // Pure longitudinal, lumped-tank prototype. All pressures are gauge kPa; SI motion units.
    public sealed class CabSimulation
    {
        public const double StepSeconds=.02;
        public SimulationConfig Config {get;}
        public SimulationState State {get;private set;}
        public CabSimulation(int seed,SimulationConfig config=null)
        {
            Config=config??new SimulationConfig();Config.Validate();
            var r=new Random(seed);
            State=new SimulationState {Seed=seed,TrainMassKg=Config.TrainMassKg*Jitter(r),CoolantC=25*Jitter(r),OilC=25*Jitter(r),FuelL=800*Jitter(r),Battery=.9*Jitter(r),StopPositionM=1200*Jitter(r)};
        }
        static double Jitter(Random r) {return 1+(r.NextDouble()*2-1)*.05;}
        static double Clamp(double x,double a,double b) {return Math.Max(a,Math.Min(b,x));}
        static double Move(double a,double b,double rate) {return a+Clamp(b-a,-rate,rate);}
        static double Lag(double a,double b,double tau) {return a+(b-a)*(1-Math.Exp(-StepSeconds/tau));}
        public void Restore(SimulationState snapshot)
        {
            if(snapshot==null||snapshot.Version!=1)throw new ArgumentException("Unsupported snapshot");
            foreach(var f in typeof(SimulationState).GetFields())if(f.FieldType==typeof(double))
            {double x=(double)f.GetValue(snapshot);if(double.IsNaN(x)||double.IsInfinity(x))throw new ArgumentException("Non-finite snapshot: "+f.Name);}
            if(snapshot.TrainMassKg<=0||snapshot.MainKpa<0||snapshot.MainKpa>Config.CompressorOffKpa||snapshot.PipeKpa<0||snapshot.PipeKpa>Config.PipeNominalKpa||snapshot.AuxKpa<0||snapshot.AuxKpa>Config.PipeNominalKpa||snapshot.ControlKpa<0||snapshot.ControlKpa>Config.PipeNominalKpa||snapshot.TrainCylinderKpa<0||snapshot.TrainCylinderKpa>Config.CylinderMaxKpa||snapshot.LocoCylinderKpa<0||snapshot.LocoCylinderKpa>Config.CylinderMaxKpa||snapshot.Battery<0||snapshot.Battery>1||snapshot.FuelL<0||snapshot.Tick<0||snapshot.Spool<0||snapshot.Spool>1||snapshot.Rpm<0||snapshot.DistanceM<0||snapshot.CrankTime<0||snapshot.Time<0||Math.Abs(snapshot.Time-snapshot.Tick*StepSeconds)>1e-7||snapshot.ActiveDirection< -1||snapshot.ActiveDirection>1)
                throw new ArgumentException("Snapshot outside physical bounds");
            State=snapshot.Copy();
        }
        public bool ResetThermalTrip()
        {if(State.Running||State.EngineRequested||State.CoolantC>80)return false;State.ThermalTrip=false;return true;}
        public double StoredAirKpaL => State.MainKpa*Config.MainVolumeL+State.PipeKpa*Config.PipeVolumeL+State.AuxKpa*Config.AuxVolumeL+State.ControlKpa*Config.ControlVolumeL+State.TrainCylinderKpa*Config.CylinderVolumeL+State.LocoCylinderKpa*Config.CylinderVolumeL;
        static void Transfer(ref double source,double sourceVolume,ref double sink,double sinkVolume,double target,double rate)
        {
            double change=Math.Max(0,Math.Min(rate*StepSeconds,Math.Min(target-sink,(source-sink)/(1+sinkVolume/sourceVolume))));
            sink+=change;source-=change*sinkVolume/sourceVolume;
        }
        void Vent(ref double p,double volume,double target,double rate)
        {double q=Math.Max(0,Math.Min(p-target,rate*StepSeconds));p-=q;State.ExhaustAirKpaL+=q*volume;}
        public void Step(SimulationInput u)
        {
            u.Validate();var s=State;var c=Config;double dt=StepSeconds;
            s.Tick++;s.Time=s.Tick*dt;
            s.EngineRequested=u.EngineRun;
            if(u.Direction!=s.ActiveDirection&&(u.Throttle==0||s.Spool<.001)&&Math.Abs(s.SpeedMps)<.1&&!s.Cranking)s.ActiveDirection=u.Direction;
            if(!u.EngineRun||!u.Master||!u.FuelPump||s.FuelL<=0||s.ThermalTrip)
            {s.Running=false;s.Cranking=false;s.CrankTime=0;}
            else if(!s.Running)
            {
                bool allowed=u.Throttle==0&&u.Direction==0&&Math.Abs(s.SpeedMps)<.1&&s.Battery>.2;
                s.Cranking=allowed;
                if(allowed){s.CrankTime+=dt;s.Battery=Math.Max(0,s.Battery-c.StarterPowerW*dt/(c.BatteryCapacityWh*3600));if(s.CrankTime>=c.StartSeconds){s.Running=true;s.Cranking=false;}}
                else s.CrankTime=0;
            }
            double demand=s.Running?u.Throttle/8.0:0;
            s.Rpm=Move(s.Rpm,s.Running?c.IdleRpm+demand*(c.RatedRpm-c.IdleRpm):s.Cranking?300:0,180*dt);
            s.Spool=Lag(s.Spool,demand,3);
            s.OilMpa=Lag(s.OilMpa,s.Running?.1+.45*Clamp(s.Rpm/c.RatedRpm,0,1):0,1.3);
            if(s.Running)s.Battery=Math.Min(1,s.Battery+c.BatteryChargeW*dt/(c.BatteryCapacityWh*3600));
            else if(u.Master)s.Battery=Math.Max(0,s.Battery-u.AuxiliaryLoadW*dt/(c.BatteryCapacityWh*3600));
            if(!s.Running||u.CompressorFailure)s.Compressor=false;
            else if(s.MainKpa<c.CompressorOnKpa)s.Compressor=true;
            else if(s.MainKpa>=c.CompressorOffKpa-.001)s.Compressor=false;
            if(s.Compressor)
            {double q=Math.Min(c.CompressorOffKpa-s.MainKpa,c.CompressorKpaPerSecond*dt);s.MainKpa+=q;s.CompressorAirKpaL+=q*c.MainVolumeL;}
            bool emergency=u.Emergency||u.Brake==4;
            double targetPipe=emergency?0:u.Brake==3?350:u.Brake==2?430:c.PipeNominalKpa;
            if(s.PipeKpa>targetPipe)Vent(ref s.PipeKpa,c.PipeVolumeL,targetPipe,emergency?180:35);
            else if(u.Brake<=1)Transfer(ref s.MainKpa,c.MainVolumeL,ref s.PipeKpa,c.PipeVolumeL,targetPipe,70);
            if(u.Brake<=1&&!emergency)
            {
                Transfer(ref s.PipeKpa,c.PipeVolumeL,ref s.AuxKpa,c.AuxVolumeL,c.PipeNominalKpa,35);
                Transfer(ref s.PipeKpa,c.PipeVolumeL,ref s.ControlKpa,c.ControlVolumeL,c.PipeNominalKpa,30);
            }
            double trainTarget=Clamp((s.ControlKpa-s.PipeKpa)*2.6,0,c.CylinderMaxKpa);
            if(trainTarget>s.TrainCylinderKpa)Transfer(ref s.AuxKpa,c.AuxVolumeL,ref s.TrainCylinderKpa,c.CylinderVolumeL,trainTarget,120);
            else Vent(ref s.TrainCylinderKpa,c.CylinderVolumeL,trainTarget,150);
            // Local relay uses MR air; never add an independent and automatic pressure together.
            double locoTarget=Math.Max(trainTarget,u.IndependentBrake*c.CylinderMaxKpa);
            if(locoTarget>s.LocoCylinderKpa)Transfer(ref s.MainKpa,c.MainVolumeL,ref s.LocoCylinderKpa,c.CylinderVolumeL,locoTarget,150);
            else Vent(ref s.LocoCylinderKpa,c.CylinderVolumeL,locoTarget,180);
            Vent(ref s.MainKpa,c.MainVolumeL,0,.02);Vent(ref s.PipeKpa,c.PipeVolumeL,0,.015);Vent(ref s.AuxKpa,c.AuxVolumeL,0,.01);
            double adhesion=c.DryAdhesion*Clamp(u.RailGrip+(u.Sand?.08:0),.01,1);
            double poweredMass=s.TrainMassKg*c.PoweredMassKg/c.TrainMassKg;
            double thermalDerate=Clamp((c.TripTemperatureC-s.CoolantC)/15,.0,1);
            double rpmFraction=Clamp(s.Rpm/c.RatedRpm,0,1);
            double shaftPower=Math.Min(c.EnginePowerW*s.Spool,c.EnginePowerW*rpmFraction*rpmFraction);
            double availablePower=Math.Max(0,(shaftPower-20000-(s.Compressor?c.CompressorLoadW:0))*c.Efficiency)*thermalDerate;
            bool tractionAllowed=s.Running&&u.Master&&u.FuelPump&&s.ActiveDirection!=0&&!emergency&&!u.ParkingBrake&&s.TrainCylinderKpa<20&&s.LocoCylinderKpa<20&&s.OilMpa>.15&&s.PipeKpa>=c.PipeNominalKpa*.94&&s.MainKpa>=600&&Math.Abs(s.SpeedMps)<c.MaxTractionSpeedMps;
            double requested=tractionAllowed?Math.Min(c.StartingForceN*demand,availablePower/Math.Max(Math.Abs(s.SpeedMps),.8)):0;
            requested*=Clamp((c.MaxTractionSpeedMps-Math.Abs(s.SpeedMps))/2,0,1);
            double adhesionLimit=adhesion*poweredMass*9.80665;
            s.AdhesionLimited=requested>adhesionLimit;s.TractionN=Math.Min(requested,adhesionLimit)*s.ActiveDirection;
            s.CurrentA=Math.Abs(s.TractionN)/c.StartingForceN*1500; // Force/current proxy, not an electrical circuit solver.
            s.RailPowerW=Math.Abs(s.TractionN*s.SpeedMps);
            s.BrakeN=Math.Min(c.ServiceBrakeN-c.LocoBrakeN,(c.ServiceBrakeN-c.LocoBrakeN)*s.TrainCylinderKpa/c.CylinderMaxKpa)
                +Math.Min(c.LocoBrakeN*s.LocoCylinderKpa/c.CylinderMaxKpa,adhesionLimit);
            s.BrakeN=Math.Min(s.BrakeN+(u.ParkingBrake?c.ParkingBrakeN:0),adhesion*s.TrainMassKg*9.80665);
            s.ResistanceN=c.DavisA+c.DavisB*Math.Abs(s.SpeedMps)+c.DavisC*s.SpeedMps*s.SpeedMps;
            s.GradeN=-s.TrainMassKg*9.80665*Math.Sin(Math.Atan(u.GradePermille/1000));
            double drive=s.TractionN+s.GradeN,resistance=s.BrakeN+s.ResistanceN,oldSpeed=s.SpeedMps;
            if(Math.Abs(oldSpeed)<1e-8)s.Acceleration=Math.Abs(drive)<=resistance?0:(drive-Math.Sign(drive)*resistance)/s.TrainMassKg;
            else s.Acceleration=(drive-Math.Sign(oldSpeed)*resistance)/s.TrainMassKg;
            double next=oldSpeed+s.Acceleration*dt;
            if(oldSpeed*next<0&&Math.Abs(drive)<=resistance)next=0;
            s.SpeedMps=next;s.Acceleration=(next-oldSpeed)/dt;
            double dx=(next+oldSpeed)*.5*dt;s.PositionM+=dx;s.DistanceM+=Math.Abs(dx);
            double load=s.Running?Clamp((s.RailPowerW/c.Efficiency+20000+(s.Compressor?c.CompressorLoadW:0))/c.EnginePowerW,0,1):0;
            double fuel=s.Running?(8+.25*load*c.EnginePowerW/1000)*dt/3600:0;
            fuel=Math.Min(s.FuelL,fuel);s.FuelL-=fuel;s.FuelUsedL+=fuel;
            if(s.CoolantC>83)s.Fan=true;else if(s.CoolantC<78)s.Fan=false;
            double cooling=(u.CoolingFailure?150:900+(s.Fan?5600:0)+Math.Abs(s.SpeedMps)*50)*(s.CoolantC-u.AmbientC);
            s.CoolantC+=(c.FullLoadHeatW*load-cooling)*dt/c.HeatCapacityJPerC;
            s.OilC=Lag(s.OilC,s.Running?s.CoolantC+8*load:u.AmbientC,120);
            if(s.CoolantC>=c.TripTemperatureC){s.ThermalTrip=true;s.Running=false;s.TractionN=0;s.CurrentA=0;s.RailPowerW=0;}
        }
    }
    [Serializable] public sealed class SimulationClock
    {
        public double Remainder;
        public void Advance(double seconds,Action tick)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentException("Invalid elapsed time");
            Remainder+=seconds;
            while(Remainder+1e-8>=CabSimulation.StepSeconds){tick();Remainder-=CabSimulation.StepSeconds;}
            if(Remainder<0)Remainder=0;
        }
    }
}
