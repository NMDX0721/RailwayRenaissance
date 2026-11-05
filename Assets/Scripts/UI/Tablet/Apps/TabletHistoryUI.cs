using System;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    public static class TabletHistoryUI
    {
        public static void BuildAchievements(VisualElement win, Action back)
        {
            win.Clear();
            var body = BuildAppBody(win, "成就记录", "已获得的荣誉与尚未达成的目标");
            var close = new Button(back) { text = "返回笔记" }; TabletBookmarksUI.StyleAction(close); close.style.alignSelf = Align.FlexStart; body.Add(close);
            var scroll = new ScrollView(); scroll.style.flexGrow = 1; body.Add(scroll);
            AchievementManager.Initialize();
            foreach (var achievement in AchievementManager.GetAll())
            {
                var card = BuildAppCard(); card.name = "achievement-" + achievement.id; card.style.marginTop = 10; card.style.flexShrink = 0; scroll.Add(card);
                card.Add(new Label(achievement.title + (achievement.unlocked ? " · 已达成" : " · 未达成")));
                card.Add(new Label(achievement.description));
                if (achievement.unlocked) card.Add(new Label(achievement.unlockedDate));
            }
        }
        public static void BuildStatistics(VisualElement win, Action back)
        {
            win.Clear();
            var body = BuildAppBody(win, "运营统计", "累计记录保留在本机，跨会话延续");
            var close = new Button(back) { text = "返回 RDA" }; TabletBookmarksUI.StyleAction(close); close.style.alignSelf = Align.FlexStart; body.Add(close);
            var scroll = new ScrollView(); scroll.style.flexGrow = 1; body.Add(scroll);
            Row("累计游戏时长", StatsManager.FormatPlayTime(StatsManager.TotalPlaySeconds));
            Row("累计运营天数", StatsManager.MaxDay + " 天");
            Row("累计客运量", StatsManager.TotalPassengers + " 人次");
            Row("累计运营收入", StatsManager.TotalRevenue + " 沙币");
            Row("其中政府补贴", StatsManager.TotalSubsidy + " 沙币");
            Row("累计运营支出", StatsManager.TotalExpense + " 沙币");
            Row("累计净利润", (StatsManager.TotalRevenue - StatsManager.TotalExpense) + " 沙币");
            Row("当前运营天数", GameData.Day + " 天"); Row("当前资金", GameData.Money + " 沙币");
            Row("当前信任", GameData.Trust + "%"); Row("机车状态", GameData.TrainCondition + "%");
            Row("预期客流", GameData.ExpectedPassengers + " 人/日");
            void Row(string title, string value)
            {
                var card = BuildAppCard(); card.style.marginTop = 6; card.style.flexShrink = 0; card.Add(new Label(title + "    " + value)); scroll.Add(card);
            }
        }
    }
}
