using System;
using UnityEngine;

namespace RailwayRenaissance.Cab
{
    public enum ReverserPosition
    {
        Reverse = -1,
        Neutral = 0,
        Forward = 1
    }

    public enum BrakeNotch
    {
        Release = 0,    // 缓解位
        Run = 1,        // 运转位
        Common = 2,     // 常用初制动
        Full = 3,       // 全常用制动
        Emergency = 4   // 紧急制动
    }

    public enum CabSignalAspect
    {
        Green = 0,       // 绿灯：进路开通，按规定速度运行
        Yellow = 1,      // 黄灯：一个闭塞分区空闲，注意减速
        RedYellow = 2,   // 红黄灯/双黄：进站道岔开通侧向
        Red = 3          // 红灯：停车信号，严禁越过
    }

    /// <summary>
    /// 驾驶台机车动力学物理与操作状态机
    /// 硬核版/悬浮版两套皮肤共用此单份数据与状态机
    /// </summary>
    public class CabEngineState : MonoBehaviour
    {
        // 1. 控制操作位
        public bool IsEngineStarted { get; private set; } = false;
        internal CabSimulationDriver DetailedSimulation {get;set;}
        public bool IsEngineRunning => DetailedSimulation!=null&&DetailedSimulation.isActiveAndEnabled ? DetailedSimulation.Model!=null&&DetailedSimulation.Model.State.Running : IsEngineStarted;

        public ReverserPosition Reverser { get; private set; } = ReverserPosition.Neutral;
        public ReverserPosition CurrentReverser => Reverser;

        public int ThrottleNotch { get; private set; } = 0; // 0 ~ 8 级

        public BrakeNotch BrakePos { get; private set; } = BrakeNotch.Run;
        public BrakeNotch CurrentBrakeNotch => BrakePos;

        public float IndependentBrakeNotch { get; private set; } = 0f; // 单阀/小闸 (0: 缓解, 1: 全制动)

        public bool HeadlightOn { get; set; } = true;
        public bool SandOn { get; set; } = false;
        public bool InstrumentLightOn { get; set; } = true;
        public bool FuelPumpOn { get; set; } = true;
        public bool CabHeaterOn { get; set; } = false;

        // 2. 物理运动状态
        public float CurrentSpeedKmh { get; set; } = 0f;
        public float AccelerationMps2 { get; private set; } = 0f;
        public float EngineRpm { get; private set; } = 600f; // 怠速 600
        public float WaterTempCelsius { get; private set; } = 75f;
        public float OilPressureMpa { get; private set; } = 0.45f;
        public float BrakeCylinderKpa { get; private set; } = 0f;      // 制动缸气压 (0~400 kPa)
        public float BrakePipeKpa { get; private set; } = 500f;        // 列车管压力 (标准定压 500 kPa)
        public float MainReservoirKpa { get; private set; } = 820f;    // 总风缸压力 (750~900 kPa)
        public float TractionCurrentAmps { get; private set; } = 0f;   // 牵引电流 (0~1800 A)

        // 3. 运行环境与区间
        public float TotalTraveledMeters { get; private set; } = 0f;
        public float DistanceToNextStation { get; set; } = 1200f; // 默认距离下个站 1200m
        public float DistanceToStationMeters => DistanceToNextStation;
        public bool SignalIsGreen => CurrentSignalAspect == CabSignalAspect.Green;
        public CabSignalAspect CurrentSignalAspect
        {
            get
            {
                if (DistanceToNextStation <= 45f) return CabSignalAspect.Red;
                if (DistanceToNextStation <= 180f) return CabSignalAspect.RedYellow;
                if (DistanceToNextStation <= 450f) return CabSignalAspect.Yellow;
                return CabSignalAspect.Green;
            }
        }
        public bool IsStationArrivalNear => DistanceToNextStation <= 180f;
        public bool IsStationStopped => Mathf.Abs(CurrentSpeedKmh) < 0.2f && DistanceToNextStation <= 15f;

        // 4. 动态反馈触发器
        public float HornPulseTimer { get; private set; } = 0f;
        bool physicalHornHeld;
        public bool IsHornActive => physicalHornHeld || HornPulseTimer > 0f;
        public void SetPhysicalHorn(bool active){physicalHornHeld=active;}
        public float BrakeImpactTimer { get; private set; } = 0f; // 制动顿挫回弹计时
        public float TargetPitchAngle { get; private set; } = 0f; // 加减速俯仰角

        // 5. 调试覆盖开关
        public bool UseDebugSpeedOverride { get; set; } = false;
        public float DebugSpeedKmh { get; set; } = 0f;

        // 6. 开机操作程序守卫
        public string LastProcedureWarning { get; private set; } = "等待启动发动机...";
        public int CurrentStartupStep { get; private set; } = 0; // 0:未启动, 1:已启机待换向, 2:待缓解, 3:可给牵引

        private void Update()
        {
            UpdatePhysics(Time.deltaTime);
        }

        public void StartEngine()
        {
            if (IsEngineStarted) return;
            if(DetailedSimulation!=null&&(!FuelPumpOn||Reverser!=ReverserPosition.Neutral||ThrottleNotch!=0||Mathf.Abs(CurrentSpeedKmh)>.36f))
            {LastProcedureWarning="启动条件不满足：油泵、空挡、零位及停车状态";return;}
            IsEngineStarted = true;
            CurrentStartupStep = 1;
            LastProcedureWarning = "发动机已点火（怠速600转），请设置换向手柄。";
        }

        public void StopEngine()
        {
            IsEngineStarted = false;
            ThrottleNotch = 0;
            CurrentStartupStep = 0;
            LastProcedureWarning = "发动机已停机。";
        }

        public bool SetReverser(ReverserPosition mode)
        {
            if(DetailedSimulation!=null&&!IsEngineRunning&&mode!=ReverserPosition.Neutral)
            {LastProcedureWarning="等待柴油机启动完成，再换向";return false;}
            if(DetailedSimulation!=null&&mode!=Reverser&&(ThrottleNotch!=0||Mathf.Abs(CurrentSpeedKmh)>.36f))
            {LastProcedureWarning="先归零并停稳，再换向";return false;}
            if (!IsEngineStarted && mode != ReverserPosition.Neutral)
            {
                LastProcedureWarning = "【操作警告】发动机尚未点火，禁止换向！";
                return false;
            }
            if (Mathf.Abs(CurrentSpeedKmh) > 2f && mode != Reverser)
            {
                LastProcedureWarning = "【操作警告】列车运行中禁止反向换向！";
                return false;
            }
            Reverser = mode;
            if (CurrentStartupStep == 1 && mode != ReverserPosition.Neutral)
            {
                CurrentStartupStep = 2;
                LastProcedureWarning = "换向就绪，请将制动阀置于缓解/运转位。";
            }
            return true;
        }

        public bool SetThrottle(int notch)
        {
            notch = Mathf.Clamp(notch, 0, 8);
            if (notch > ThrottleNotch)
            {
                if(DetailedSimulation!=null&&(!IsEngineRunning||DetailedSimulation.ParkingBrake||MainReservoirKpa<600||BrakePipeKpa<470||BrakeCylinderKpa>=20))
                {LastProcedureWarning="牵引闭锁：等待启机、充风，并缓解空气制动与停放制动";return false;}
                if (!IsEngineStarted)
                {
                    LastProcedureWarning = "【操作警告】未启动发动机，无法施加牵引！";
                    return false;
                }
                if (Reverser == ReverserPosition.Neutral)
                {
                    LastProcedureWarning = "【操作警告】换向手柄处于空档，无法施加牵引！";
                    return false;
                }
                if (BrakePos >= BrakeNotch.Common)
                {
                    LastProcedureWarning = "【操作警告】当前处于制动状态，请先缓解制动阀！";
                    return false;
                }
                CurrentStartupStep = 3;
                LastProcedureWarning = $"牵引力输出中：第 {notch} 档位。";
            }
            else if (notch > 0 && notch < ThrottleNotch)
            {
                LastProcedureWarning = $"主控手柄降至第 {notch} 档。";
            }
            else if (notch > 0)
            {
                return true;
            }
            else
            {
                LastProcedureWarning = "主控手柄归零，惰行中。";
            }

            ThrottleNotch = notch;
            return true;
        }

        public void ThrottleUp()
        {
            SetThrottle(ThrottleNotch + 1);
        }

        public void ThrottleDown()
        {
            SetThrottle(ThrottleNotch - 1);
        }

        public void SetBrakeNotch(BrakeNotch pos)
        {
            if (pos >= BrakeNotch.Full && BrakePos < BrakeNotch.Common)
            {
                // 急制动引发顿挫回弹
                BrakeImpactTimer = 0.45f;
            }
            BrakePos = pos;

            if (pos <= BrakeNotch.Run && CurrentStartupStep == 2)
            {
                CurrentStartupStep = 3;
                LastProcedureWarning = "制动已缓解，可以推主控施加牵引。";
            }
            else if (pos >= BrakeNotch.Common && ThrottleNotch > 0)
            {
                // 刹车时强制切断牵引
                ThrottleNotch = 0;
                LastProcedureWarning = "制动起效，牵引回路已自动切断。";
            }
        }

        public void BrakeApplyStep()
        {
            int next = Mathf.Clamp((int)BrakePos + 1, 0, (int)BrakeNotch.Emergency);
            SetBrakeNotch((BrakeNotch)next);
        }

        public void BrakeReleaseStep()
        {
            int prev = Mathf.Clamp((int)BrakePos - 1, 0, (int)BrakeNotch.Emergency);
            SetBrakeNotch((BrakeNotch)prev);
        }

        public void OverrideSpeed(float spd)
        {
            UseDebugSpeedOverride = true;
            DebugSpeedKmh = spd;
            CurrentSpeedKmh = spd;
        }

        public void TriggerHorn()
        {
            HornPulseTimer = 0.5f;
        }

        public string GetReverserString() => Reverser switch
        {
            ReverserPosition.Forward => "前进",
            ReverserPosition.Reverse => "后退",
            _ => "中立"
        };

        public string GetBrakeNotchString() => BrakePos switch
        {
            BrakeNotch.Release => "缓解",
            BrakeNotch.Run => "运转",
            BrakeNotch.Common => "常用初制",
            BrakeNotch.Full => "全制动",
            BrakeNotch.Emergency => "紧急制动",
            _ => "运转"
        };

        /// <summary>
        /// 每帧动力学数值更新
        /// </summary>
        public void UpdatePhysics(float dt)
        {
            if (HornPulseTimer > 0f) HornPulseTimer -= dt;
            if (BrakeImpactTimer > 0f) BrakeImpactTimer -= dt;
            if(DetailedSimulation!=null&&DetailedSimulation.isActiveAndEnabled){DetailedSimulation.Advance(dt);return;}

            if (UseDebugSpeedOverride)
            {
                CurrentSpeedKmh = DebugSpeedKmh;
                float distStep = (CurrentSpeedKmh / 3.6f) * dt;
                TotalTraveledMeters += distStep;
                DistanceToNextStation = Mathf.Max(0f, DistanceToNextStation - distStep);
                return;
            }

            // 1. 牵引力计算
            float tractiveForce = 0f;
            if (IsEngineStarted && Reverser != ReverserPosition.Neutral)
            {
                // 每一阶提供约 18.5 kN 牵引
                tractiveForce = ThrottleNotch * 18.5f * (float)Reverser;
            }

            // 2. 制动力计算 (kN)
            float brakeForce = 0f;
            switch (BrakePos)
            {
                case BrakeNotch.Release: brakeForce = 0f; break;
                case BrakeNotch.Run: brakeForce = 1.2f; break;     // 管道充气微阻
                case BrakeNotch.Common: brakeForce = 45f; break;
                case BrakeNotch.Full: brakeForce = 90f; break;
                case BrakeNotch.Emergency: brakeForce = 160f; break;
            }

            // 3. 基本阻力 (戴维斯阻力公式简化: A + B*v + C*v^2)
            float v = Mathf.Abs(CurrentSpeedKmh);
            float baseResistance = 3.5f + 0.05f * v + 0.0012f * v * v;
            if (v < 0.1f && ThrottleNotch == 0) baseResistance = 0f;

            // 4. 合力与加速度 (列车质量按 180 吨计)
            float trainMassTons = 180f;
            float netForceKn = tractiveForce;
            if (CurrentSpeedKmh > 0.05f)
            {
                netForceKn -= (brakeForce + baseResistance);
            }
            else if (CurrentSpeedKmh < -0.05f)
            {
                netForceKn += (brakeForce + baseResistance);
            }
            else
            {
                // 静止状态，制动力锁死
                if (Mathf.Abs(tractiveForce) <= brakeForce)
                {
                    netForceKn = 0f;
                }
            }

            AccelerationMps2 = netForceKn / trainMassTons;
            float newSpeedMps = (CurrentSpeedKmh / 3.6f) + AccelerationMps2 * dt;

            // 停车死区吸附
            if (Mathf.Abs(newSpeedMps) < 0.08f && ThrottleNotch == 0 && BrakePos >= BrakeNotch.Run)
            {
                newSpeedMps = 0f;
                AccelerationMps2 = 0f;
            }

            CurrentSpeedKmh = Mathf.Clamp(newSpeedMps * 3.6f, -25f, 105f); // 倒车限速25，最高105

            // 5. 里程与车站距离推移
            float moveMeters = (CurrentSpeedKmh / 3.6f) * dt;
            TotalTraveledMeters += moveMeters;
            DistanceToNextStation = Mathf.Max(0f, DistanceToNextStation - moveMeters);
            if (DistanceToNextStation <= 0.1f && Mathf.Abs(CurrentSpeedKmh) < 1f)
            {
                // 到站循环重置下一个区间
                DistanceToNextStation = 1500f;
            }

            // 6. 仪表指标模拟
            float targetRpm = IsEngineStarted ? (600f + ThrottleNotch * 145f) : 0f;
            EngineRpm = Mathf.MoveTowards(EngineRpm, targetRpm, 600f * dt);

            float targetWaterTemp = IsEngineStarted ? (70f + ThrottleNotch * 2.5f) : 25f;
            WaterTempCelsius = Mathf.MoveTowards(WaterTempCelsius, targetWaterTemp, 2f * dt);

            float targetOilPressure = IsEngineStarted ? (0.35f + (EngineRpm / 1800f) * 0.25f) : 0f;
            OilPressureMpa = Mathf.MoveTowards(OilPressureMpa, targetOilPressure, 0.5f * dt);

            // JZ-7 制动系统压力逻辑：
            // 大闸不同档位对应列车管减压量：
            // 运转/缓解: 列车管 500 kPa
            // 常用初制: 列车管减压至 430 kPa，制动缸升压至 140 kPa
            // 全常用: 列车管减压至 360 kPa，制动缸升压至 320 kPa
            // 紧急制动: 列车管瞬间排风至 0 kPa，制动缸达到最大 420 kPa
            float targetBrakePipe = BrakePos switch
            {
                BrakeNotch.Release => 520f,
                BrakeNotch.Run => 500f,
                BrakeNotch.Common => 430f,
                BrakeNotch.Full => 360f,
                BrakeNotch.Emergency => 0f,
                _ => 500f
            };
            float pipeRate = BrakePos == BrakeNotch.Emergency ? 1200f : 200f; // 紧急排风极快
            BrakePipeKpa = Mathf.MoveTowards(BrakePipeKpa, targetBrakePipe, pipeRate * dt);

            // 制动缸压力：大闸制动缸 + 单阀小闸制动缸（取较大值）
            float autoCylinder = Mathf.Clamp((500f - BrakePipeKpa) * 0.85f, 0f, 420f);
            float indepCylinder = IndependentBrakeNotch * 300f;
            float targetBrakeCylinder = Mathf.Max(autoCylinder, indepCylinder);
            BrakeCylinderKpa = Mathf.MoveTowards(BrakeCylinderKpa, targetBrakeCylinder, 380f * dt);

            // 总风缸气压：正常维持在 780~880 kPa，排风充风时轻微波动
            float targetMainReservoir = (BrakePos == BrakeNotch.Emergency || BrakePos == BrakeNotch.Release) ? 760f : 840f;
            MainReservoirKpa = Mathf.MoveTowards(MainReservoirKpa, targetMainReservoir, 25f * dt);

            // 牵引电流模拟：与主控手柄档位和车速负载相关
            float targetCurrent = (IsEngineStarted && ThrottleNotch > 0 && Reverser != ReverserPosition.Neutral)
                ? (ThrottleNotch * 180f + Mathf.Clamp(100f - Mathf.Abs(CurrentSpeedKmh), 0f, 100f) * 4f)
                : 0f;
            TractionCurrentAmps = Mathf.MoveTowards(TractionCurrentAmps, targetCurrent, 600f * dt);

            // 7. 加减速俯仰目标角 (±2度内，禁止瞬变)
            TargetPitchAngle = Mathf.Clamp(-AccelerationMps2 * 2.2f, -1.8f, 1.8f);
        }

        public void SetIndependentBrake(float val)
        {
            IndependentBrakeNotch = Mathf.Clamp01(val);
        }
        internal void ApplySimulation(Simulation.SimulationState s)
        {
            CurrentSpeedKmh=(float)(s.SpeedMps*3.6);AccelerationMps2=(float)s.Acceleration;
            EngineRpm=(float)s.Rpm;WaterTempCelsius=(float)s.CoolantC;OilPressureMpa=(float)s.OilMpa;
            BrakeCylinderKpa=(float)s.LocoCylinderKpa;BrakePipeKpa=(float)s.PipeKpa;MainReservoirKpa=(float)s.MainKpa;
            TractionCurrentAmps=(float)s.CurrentA;TotalTraveledMeters=(float)s.PositionM;
            DistanceToNextStation=(float)Math.Max(0,s.StopPositionM-s.PositionM);
            TargetPitchAngle=Mathf.Clamp(-AccelerationMps2*2.2f,-1.8f,1.8f);
            CurrentStartupStep=s.Running?(s.PipeKpa>=470&&s.MainKpa>=600&&s.LocoCylinderKpa<20?3:2):s.Cranking?1:0;
        }
        internal void RestoreSimulationCommands(Simulation.SimulationInput u)
        {
            IsEngineStarted=u.EngineRun;FuelPumpOn=u.FuelPump;Reverser=(ReverserPosition)u.Direction;
            ThrottleNotch=u.Throttle;BrakePos=(BrakeNotch)u.Brake;IndependentBrakeNotch=(float)u.IndependentBrake;SandOn=u.Sand;
        }
    }
}
