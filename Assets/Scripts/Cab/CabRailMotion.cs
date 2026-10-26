using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    // Kinematic rail follower plus bounded secondary-suspension presentation. Not a contact-force solver.
    [DefaultExecutionOrder(100)]
    public sealed class CabRailMotion : MonoBehaviour
    {
        public const double RollingRadius=.461222222222;
        [SerializeField] CabSimulationDriver driver;
        [SerializeField] Transform vehicle;
        [SerializeField] int reviewTrackSeed=20261021;
        [SerializeField] Bogie[] bogies;
        [Serializable] public sealed class Bogie
        {
            public Transform frame;
            public double stationOffset;
            public Transform[] axles, shoes, coils, lowerParts, upperPlates, pads;
            public Vector3[] axlePositions, shoePositions, coilPositions, coilScales, lowerPositions, upperVehiclePositions, padScales;
            public Quaternion[] upperVehicleRotations;
        }
        struct Spring
        {
            public double Value,Velocity;
            public void Reset(double value){Value=value;Velocity=0;}
            public void Step(double target,double frequency,double damping,double dt)
            {
                double w=2*Math.PI*frequency;
                Velocity+=(w*w*(target-Value)-2*damping*w*Velocity)*dt;Value+=Velocity*dt;
            }
        }
        Spring height,pitch,roll;
        bool initialized;
        double lastTime=-1,lastPosition;
        readonly CabReviewTrack track=new CabReviewTrack();
        public Transform Vehicle=>vehicle;
        public Bogie[] Bogies=>bogies;
        public double Station {get;private set;}
        public double WheelAngleDegrees {get;private set;}
        public double HeaveError {get;private set;}
        public double RollDegrees {get;private set;}
        public double PitchDegrees {get;private set;}
        public CabReviewTrack Track=>track;
        public int TrackSeed {get;private set;}
        public void Configure(CabSimulationDriver source,Transform body)
        {
            driver=source;vehicle=body;var list=new List<Bogie>();
            foreach(Transform node in body.GetComponentsInChildren<Transform>(true))
            {
                if(!node.name.StartsWith("Powered_Bogie_",StringComparison.Ordinal))continue;
                var b=new Bogie{frame=node,stationOffset=8.6-node.localPosition.z};
                var axles=new List<Transform>();var shoes=new List<Transform>();var coils=new List<Transform>();
                var lower=new List<Transform>();var upper=new List<Transform>();var pads=new List<Transform>();
                foreach(Transform part in node)
                {
                    switch(part.name)
                    {
                        case "Driven_Wheelset":axles.Add(part);break;
                        case "Curved_Tread_Brake_Shoe":shoes.Add(part);break;
                        case "Primary_Coil_Spring":coils.Add(part);break;
                        case "Axlebox":case "Axlebox_End_Cover":case "Axlebox_Cover_Bolt":case "Primary_Lower_Spring_Seat":case "Axlebox_Spring_Wing":lower.Add(part);break;
                        case "Secondary_Upper_Plate":upper.Add(part);break;
                        case "Secondary_Elastomer_Pad":pads.Add(part);break;
                    }
                }
                b.axles=axles.ToArray();b.shoes=shoes.ToArray();b.coils=coils.ToArray();b.lowerParts=lower.ToArray();b.upperPlates=upper.ToArray();b.pads=pads.ToArray();
                b.axlePositions=Positions(b.axles);b.shoePositions=Positions(b.shoes);b.coilPositions=Positions(b.coils);b.coilScales=Scales(b.coils);b.lowerPositions=Positions(b.lowerParts);b.padScales=Scales(b.pads);
                b.upperVehiclePositions=new Vector3[b.upperPlates.Length];b.upperVehicleRotations=new Quaternion[b.upperPlates.Length];
                for(int i=0;i<b.upperPlates.Length;i++){b.upperVehiclePositions[i]=body.InverseTransformPoint(b.upperPlates[i].position);b.upperVehicleRotations[i]=Quaternion.Inverse(body.rotation)*b.upperPlates[i].rotation;}
                list.Add(b);
            }
            if(list.Count!=2||list.Exists(b=>b.axles.Length!=2))throw new InvalidOperationException("Rail motion requires two two-axle bogies");
            bogies=list.ToArray();initialized=false;
        }
        static Vector3[] Positions(Transform[] t){var a=new Vector3[t.Length];for(int i=0;i<t.Length;i++)a[i]=t[i].localPosition;return a;}
        static Vector3[] Scales(Transform[] t){var a=new Vector3[t.Length];for(int i=0;i<t.Length;i++)a[i]=t[i].localScale;return a;}
        void LateUpdate(){ApplyBackend();ProtectReviewRouteEnd();}
        public void ApplyBackend(bool reset=false)
        {
            if(driver==null||driver.Model==null)return;
            var s=driver.Model.State;
            bool jump=lastTime<0||s.Time<lastTime||Math.Abs(s.PositionM-lastPosition)>Math.Abs(s.SpeedMps)*Math.Max(0,s.Time-lastTime)+2;
            Apply(s.PositionM,s.SpeedMps,s.Acceleration,Math.Max(s.LocoCylinderKpa,s.TrainCylinderKpa)/380,Math.Min(.1,Math.Max(0,s.Time-lastTime)),reviewTrackSeed,reset||jump);
            lastTime=s.Time;lastPosition=s.PositionM;
            // Cabin interaction raycasts must see the moved colliders in the current frame.
            Physics.SyncTransforms();
        }
        void ProtectReviewRouteEnd()
        {
            if(driver==null||driver.Model==null)return;
            var s=driver.Model.State;
            double remaining=s.SpeedMps>=0?CabReviewTrack.End-Station-10:Station-CabReviewTrack.Start-10;
            if(Math.Abs(s.SpeedMps)>.01&&remaining<s.SpeedMps*s.SpeedMps/(2*.25)+30)
                driver.GetComponent<CabEngineState>().SetBrakeNotch(BrakeNotch.Emergency);
        }
        public void Apply(double station,double speed,double acceleration,double brake,double dt,int seed,bool reset=false)
        {
            if(vehicle==null||bogies==null)return;
            if(double.IsNaN(station)||double.IsInfinity(station)||double.IsNaN(dt)||dt<0)throw new ArgumentException("Invalid rail pose input");
            Station=station;TrackSeed=seed;WheelAngleDegrees=-station/RollingRadius*180/Math.PI;
            var front=track.At(station+6.1,seed);var rear=track.At(station-6.1,seed);
            Vector3 forward=front.Position-rear.Position;forward.y=0;forward.Normalize();
            Quaternion yaw=Quaternion.LookRotation(-forward,Vector3.up);
            double targetHeight=(front.Position.y+rear.Position.y)*.5-CabReviewTrack.RailDatum;
            double targetPitch=Math.Max(-.07,Math.Min(.07,acceleration*.15));
            double targetRoll=Math.Max(-.28,Math.Min(.28,speed*speed*track.At(station,seed).Curvature*.9));
            if(reset||!initialized){height.Reset(targetHeight);pitch.Reset(targetPitch);roll.Reset(targetRoll);initialized=true;}
            for(double remaining=Math.Min(dt,.1);remaining>1e-9;)
            {
                double step=Math.Min(1.0/120,remaining);height.Step(targetHeight,2,.72,step);pitch.Step(targetPitch,1.4,.75,step);roll.Step(targetRoll,1.1,.75,step);remaining-=step;
            }
            height.Value=Math.Max(targetHeight-.004,Math.Min(targetHeight+.004,height.Value));
            pitch.Value=Math.Max(-.08,Math.Min(.08,pitch.Value));roll.Value=Math.Max(-.30,Math.Min(.30,roll.Value));
            HeaveError=height.Value-targetHeight;PitchDegrees=pitch.Value;RollDegrees=roll.Value;
            var centre=(front.Position+rear.Position)*.5f;centre.y=(float)height.Value;
            vehicle.rotation=yaw*Quaternion.Euler((float)pitch.Value,0,(float)roll.Value);
            vehicle.position=centre-vehicle.rotation*new Vector3(0,0,8.6f);
            foreach(var b in bogies)
            {
                double bs=station+b.stationOffset;
                var a=track.At(bs+1.1,seed);var c=track.At(bs-1.1,seed);
                var tangent=a.Position-c.Position;tangent.y=0;
                Quaternion rotation=Quaternion.LookRotation(-tangent,Vector3.up);
                var framePosition=(a.Position+c.Position)*.5f;framePosition.y-=CabReviewTrack.RailDatum;
                b.frame.SetPositionAndRotation(framePosition,rotation);
                for(int i=0;i<b.axles.Length;i++)
                {
                    var rail=track.At(bs-b.axlePositions[i].z,seed);
                    b.axles[i].SetPositionAndRotation(rail.Position+Vector3.up*(float)RollingRadius,rotation*Quaternion.AngleAxis((float)(WheelAngleDegrees%360),Vector3.right));
                }
                float Delta(float z){return track.At(bs-z,seed).Position.y-CabReviewTrack.RailDatum-framePosition.y;}
                for(int i=0;i<b.lowerParts.Length;i++)b.lowerParts[i].localPosition=b.lowerPositions[i]+Vector3.up*Delta(Mathf.Sign(b.lowerPositions[i].z)*1.1f);
                for(int i=0;i<b.coils.Length;i++)
                {
                    float delta=Delta(Mathf.Sign(b.coilPositions[i].z)*1.1f);b.coils[i].localPosition=b.coilPositions[i]+Vector3.up*delta;
                    var scale=b.coilScales[i];scale.y*=Mathf.Clamp(1-delta/.21f,.90f,1.10f);b.coils[i].localScale=scale;
                }
                for(int i=0;i<b.shoes.Length;i++)b.shoes[i].localPosition=b.shoePositions[i]+Vector3.up*Delta(Mathf.Sign(b.shoePositions[i].z)*1.1f)+Vector3.forward*(Mathf.Sign(b.shoePositions[i].z)*.0025f*Mathf.Clamp01((float)brake));
                for(int i=0;i<b.upperPlates.Length;i++)b.upperPlates[i].SetPositionAndRotation(vehicle.TransformPoint(b.upperVehiclePositions[i]),vehicle.rotation*b.upperVehicleRotations[i]);
                for(int i=0;i<b.pads.Length;i++)
                {
                    var pad=b.pads[i];var upper=b.upperPlates[i];Vector3 top=b.frame.InverseTransformPoint(upper.position);float h=Mathf.Clamp(top.y-.01f+.3825f,.045f,.085f);
                    var pos=pad.localPosition;pos.y=-.3825f+h*.5f;pad.localPosition=pos;var scale=b.padScales[i];scale.y=h;pad.localScale=scale;
                }
            }
        }
    }
}
