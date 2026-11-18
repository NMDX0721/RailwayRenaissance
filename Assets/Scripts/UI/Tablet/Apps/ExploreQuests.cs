using System;
using System.Collections.Generic;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public enum QuestEvent { Talk, Interact, Enter }

    public sealed class QuestObjective
    {
        public QuestEvent kind;
        public string key;        // npc id / spot id / area id
        public string text;       // 显示文字
        public int need = 1, have;
        public bool Done => have >= need;
    }

    public sealed class Quest
    {
        public string id, title, giver, desc;
        public int rewardMoney, rewardTrust;
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public bool Done { get { foreach (var o in objectives) if (!o.Done) return false; return true; } }
        public bool tracked = true;
        public string RewardText
        {
            get
            {
                var s = "";
                if (rewardMoney != 0) s += $"资金 +{rewardMoney}";
                if (rewardTrust != 0) s += (s.Length > 0 ? "　" : "") + $"口碑 +{rewardTrust}";
                return s;
            }
        }
    }

    /// <summary>
    /// 探索场景的日常任务：每天重置一组「站长手记」。视图只管上报事件（对话/交互/进门），
    /// 任务进度、奖励结算、变更通知都在这里；UI 订阅 Changed / Completed。
    /// </summary>
    public static class ExploreQuests
    {
        public static readonly List<Quest> Active = new List<Quest>();
        public static event Action Changed;
        public static event Action<Quest> Completed;
        public static int CompletedToday { get; private set; }

        private static int day = int.MinValue;

        /// <summary>日期变化时重新发放今日任务（读取游戏天数，首次调用也会发放）。</summary>
        public static void EnsureToday()
        {
            int d = GameData.GetDay();
            if (d == day && Active.Count > 0) return;
            day = d; CompletedToday = 0;
            Active.Clear();
            Active.Add(Make("greet", "问候老陈", "老陈", "老陈在站台边守着机车，去打个招呼，听听水位和线路的情况。", 30, 0,
                Obj(QuestEvent.Talk, "laochen", "和老陈聊聊")));
            Active.Add(Make("timetable", "核对今日班次", "赵师傅", "赵师傅排好了班次表，到调度室的调度台前对一遍再发车。", 50, 1,
                Obj(QuestEvent.Enter, "dispatch", "走进调度室"),
                Obj(QuestEvent.Interact, "dispatch_desk", "在调度台前查看班次")));
            Active.Add(Make("hallboard", "看看委托公告板", "老钱", "站务大厅的公告板上挂着货主的委托，去看看有什么活可接。", 40, 0,
                Obj(QuestEvent.Enter, "hall", "走进站务大厅"),
                Obj(QuestEvent.Interact, "hall_board", "在公告板前查看委托")));
            Active.Add(Make("garage", "车库问问车况", "张工", "闸瓦和车况都得问问张工，别等出了毛病再说。", 40, 0,
                Obj(QuestEvent.Talk, "zhanggong", "和张工聊聊车况")));
            Active.Add(Make("tea", "尝尝新茶", "茶嫂", "茶嫂说霜降前这批茶得下山，去茶摊坐一坐。", 35, 1,
                Obj(QuestEvent.Talk, "chasao", "和茶嫂聊聊新茶"),
                Obj(QuestEvent.Interact, "tea", "在茶摊接一份委托")));
            Active.Add(Make("tinker", "闲不住的站长", "小芳", "小芳在学看信号灯：扳一扳站台边的信号机手柄，再去东端道口放一次栏杆给她看看。", 40, 1,
                Obj(QuestEvent.Interact, "signal_lever", "扳动信号机手柄"),
                Obj(QuestEvent.Interact, "east_crossing", "在东端道口操作栏杆")));
            Active.Add(Make("rest","歇口气", "自己", "忙了半天，在调度室的沙发上歇一会儿，恢复精力。", 0, 1,
                Obj(QuestEvent.Interact, "sofa", "在沙发上休息")));
            Changed?.Invoke();
        }

        public static void Report(QuestEvent kind, string key)
        {
            EnsureToday();
            if (key == "sofa2" || key == "bench") key = "sofa";
            bool any = false;
            foreach (var q in Active)
            {
                if (q.Done) continue;
                bool wasDone = false;
                foreach (var o in q.objectives)
                {
                    if (o.kind != kind || o.key != key || o.Done) continue;
                    o.have++; any = true;
                }
                if (!wasDone && q.Done) Finish(q);
            }
            if (any) Changed?.Invoke();
        }

        private static void Finish(Quest q)
        {
            if (q.rewardMoney != 0) GameData.AddMoney(q.rewardMoney);
            if (q.rewardTrust != 0) GameData.AddTrust(q.rewardTrust);
            CompletedToday++;
            Completed?.Invoke(q);
        }

        public static Quest First(bool unfinished)
        {
            foreach (var q in Active) if (q.tracked && q.Done != unfinished) return q;
            return null;
        }

        public static int Remaining { get { int n = 0; foreach (var q in Active) if (!q.Done) n++; return n; } }

        /// <summary>评审/新档用：清空进度，下次访问重新发放。</summary>
        public static void ResetForTests() { Active.Clear(); day = int.MinValue; CompletedToday = 0; }

        private static QuestObjective Obj(QuestEvent k, string key, string text) => new QuestObjective { kind = k, key = key, text = text };

        private static Quest Make(string id, string title, string giver, string desc, int money, int trust, params QuestObjective[] objs)
        {
            var q = new Quest { id = id, title = title, giver = giver, desc = desc, rewardMoney = money, rewardTrust = trust };
            q.objectives.AddRange(objs);
            return q;
        }
    }
}
