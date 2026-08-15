using System.Collections.Generic;

namespace Narrative
{
    /// <summary>角色状态（N1.5）——NPC 疲劳/忠诚/好感度/记忆与对话历史。</summary>
    [System.Serializable]
    public class CharacterState
    {
        public string npcId, name;
        public int fatigue, loyalty, favorability;
        public string emotionalState; // 正常/疲惫/不满
        public string[] unlockedMemories; // 已解锁的记忆片段ID
        public List<string> recentDialogue;
        public bool fatigueTriggered; // 是否已触发疲劳对话
    }
}