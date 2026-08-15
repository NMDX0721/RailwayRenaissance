using System.Collections.Generic;

namespace Narrative
{
    /// <summary>世界状态快照（N1.1）——WorldAnalyzer 的产出，叙事引擎的输入。</summary>
    [System.Serializable]
    public class NarrativeContext
    {
        public int gameDay;
        public float trustTrend, fiscalTrend, sandPenetration, politicalPressure, infrastructureDecay;
        public bool trustCrossedThreshold, fiscalCrossedThreshold; // 阈值检测结果
        public List<CharacterState> characterStates; // 角色状态
        public string[] unlockedCityIds; // 已解锁城市
        public string[] justUnlockedRegionCityIds; // 刚解锁的城市（GAP1）
        public string sandRivalActionType; // 铁龙当前行动类型（GAP6）
        public string[] fatiguedNpcIds; // 疲劳/忠诚越过阈值的NPC ID（GAP7）
        public WorldGen.WorldSeedData seed; // 引用种子
        public List<string> recentEventIds; // 最近N天事件ID
    }
}