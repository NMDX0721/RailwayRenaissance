using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：RDA 铁路行车调度助手（含 T304 五线态势快捷入口）
    /// 从 GameMainUI 剥离（T306 Batch 1-D）
    /// </summary>
    public static class TabletRDAAppUI
    {
    // ============ App: RDA（铁路行车调度助手）============
    private static Action<string> s_launchApp;
    private static Action s_openStory;
    private static Action s_openAffairs;
    private static Action<string> s_showToast;

    private static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[RDA] " + msg);
    }

    public static void BuildRDAApp(VisualElement win, Action<string> launchApp = null, Action onOpenStory = null, Action onOpenAffairs = null, Action<string> showToast = null)
    {
        if (launchApp != null) s_launchApp = launchApp;
        if (onOpenStory != null) s_openStory = onOpenStory;
        if (onOpenAffairs != null) s_openAffairs = onOpenAffairs;
        if (showToast != null) s_showToast = showToast;
        // 全屏双栏调度工作台
        var columns = new VisualElement();
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.flexGrow = 1;
        win.Add(columns);

        // 左栏：42% 宽度，线路与列车状态、资金与客流、操作快捷键
        var leftCol = new VisualElement();
        leftCol.style.width = new Length(42, LengthUnit.Percent);
        leftCol.style.flexDirection = FlexDirection.Column;
        leftCol.style.marginRight = 16;
        columns.Add(leftCol);

        // 线路状态大卡片
        var lineCard = new VisualElement();
        lineCard.style.backgroundColor = GlassBgSoft;
        lineCard.style.borderTopLeftRadius = 16; lineCard.style.borderTopRightRadius = 16;
        lineCard.style.borderBottomLeftRadius = 16; lineCard.style.borderBottomRightRadius = 16;
        lineCard.style.borderTopWidth = 1; lineCard.style.borderBottomWidth = 1;
        lineCard.style.borderLeftWidth = 1; lineCard.style.borderRightWidth = 1;
        lineCard.style.borderTopColor = GlassBorderSoft; lineCard.style.borderBottomColor = GlassBorderSoft;
        lineCard.style.borderLeftColor = GlassBorderSoft; lineCard.style.borderRightColor = GlassBorderSoft;
        lineCard.style.paddingLeft = 18; lineCard.style.paddingRight = 18;
        lineCard.style.paddingTop = 14; lineCard.style.paddingBottom = 14;
        lineCard.style.marginBottom = 14;
        leftCol.Add(lineCard);

        var lineTitleRow = new VisualElement();
        lineTitleRow.style.flexDirection = FlexDirection.Row;
        lineTitleRow.style.justifyContent = Justify.SpaceBetween;
        lineTitleRow.style.alignItems = Align.Center;
        lineCard.Add(lineTitleRow);

        var lineTitle = new Label("当前支线 · 雾峰村 - 矿区线");
        lineTitle.style.fontSize = 17; lineTitle.style.color = GlassAccent; lineTitle.style.unityFontDefinition = Fd();
        lineTitleRow.Add(lineTitle);

        var trendBtn = new Button(() => s_launchApp?.Invoke("五线态势"));
        trendBtn.text = "📊 五线态势";
        trendBtn.style.fontSize = 11;
        trendBtn.style.unityFontDefinition = Fd();
        trendBtn.style.backgroundColor = new Color(0.2f, 0.35f, 0.5f, 0.5f);
        trendBtn.style.color = Color.white;
        trendBtn.style.borderTopWidth = 0; trendBtn.style.borderBottomWidth = 0;
        trendBtn.style.borderLeftWidth = 0; trendBtn.style.borderRightWidth = 0;
        trendBtn.style.borderTopLeftRadius = 4; trendBtn.style.borderTopRightRadius = 4;
        trendBtn.style.borderBottomLeftRadius = 4; trendBtn.style.borderBottomRightRadius = 4;
        trendBtn.style.paddingLeft = 8; trendBtn.style.paddingRight = 8;
        trendBtn.style.paddingTop = 2; trendBtn.style.paddingBottom = 2;
        lineTitleRow.Add(trendBtn);

        var lineDesc = new Label("全长 23.0 km · 4站设防 · 主要运能：茶叶 / 原煤 / 村民通勤");
        lineDesc.style.fontSize = 12; lineDesc.style.color = GlassTextDim; lineDesc.style.unityFontDefinition = Fd();
        lineDesc.style.marginTop = 4;
        lineCard.Add(lineDesc);

        var trainInfo = new Label("机车：0721 号「沙子飞猪」先民内燃机车 · 2节客厢");
        trainInfo.style.fontSize = 13; trainInfo.style.color = GlassText; trainInfo.style.unityFontDefinition = Fd();
        trainInfo.style.marginTop = 10;
        lineCard.Add(trainInfo);

        // 车况进度条
        var condRow = new VisualElement();
        condRow.style.flexDirection = FlexDirection.Row;
        condRow.style.justifyContent = Justify.SpaceBetween;
        condRow.style.marginTop = 8;
        lineCard.Add(condRow);

        var condText = new Label($"车况健康度：{GameData.TrainCondition}%");
        condText.style.fontSize = 12; condText.style.color = GlassText; condText.style.unityFontDefinition = Fd();
        condRow.Add(condText);

        var condStatus = new Label(GameData.TrainCondition >= 70 ? "状态良好" : (GameData.TrainCondition >= 40 ? "注意损耗" : "亟待大修"));
        condStatus.style.fontSize = 12;
        condStatus.style.color = GameData.TrainCondition >= 70 ? new Color(0.4f, 0.85f, 0.4f, 0.9f) : new Color(0.9f, 0.6f, 0.2f, 0.9f);
        condStatus.style.unityFontDefinition = Fd();
        condRow.Add(condStatus);

        var condBar = new VisualElement();
        condBar.style.height = 6; condBar.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
        condBar.style.borderTopLeftRadius = 3; condBar.style.borderTopRightRadius = 3;
        condBar.style.borderBottomLeftRadius = 3; condBar.style.borderBottomRightRadius = 3;
        condBar.style.marginTop = 4;
        lineCard.Add(condBar);

        var condFill = new VisualElement();
        condFill.style.width = new Length(Mathf.Clamp(GameData.TrainCondition, 0, 100), LengthUnit.Percent);
        condFill.style.height = 6;
        condFill.style.backgroundColor = GameData.TrainCondition >= 70 ? new Color(0.4f, 0.85f, 0.4f, 0.9f) : new Color(0.9f, 0.6f, 0.2f, 0.9f);
        condFill.style.borderTopLeftRadius = 3; condFill.style.borderTopRightRadius = 3;
        condFill.style.borderBottomLeftRadius = 3; condFill.style.borderBottomRightRadius = 3;
        condBar.Add(condFill);

        // 核心数值卡片
        var statsCard = new VisualElement();
        statsCard.style.backgroundColor = GlassBgSoft;
        statsCard.style.borderTopLeftRadius = 16; statsCard.style.borderTopRightRadius = 16;
        statsCard.style.borderBottomLeftRadius = 16; statsCard.style.borderBottomRightRadius = 16;
        statsCard.style.borderTopWidth = 1; statsCard.style.borderBottomWidth = 1;
        statsCard.style.borderLeftWidth = 1; statsCard.style.borderRightWidth = 1;
        statsCard.style.borderTopColor = GlassBorderSoft; statsCard.style.borderBottomColor = GlassBorderSoft;
        statsCard.style.borderLeftColor = GlassBorderSoft; statsCard.style.borderRightColor = GlassBorderSoft;
        statsCard.style.paddingLeft = 18; statsCard.style.paddingRight = 18;
        statsCard.style.paddingTop = 14; statsCard.style.paddingBottom = 14;
        statsCard.style.marginBottom = 14;
        leftCol.Add(statsCard);

        var stat1 = new Label($"运营资金：{GameData.Money:N0} 沙币");
        stat1.style.fontSize = 15; stat1.style.color = GlassAccent; stat1.style.unityFontDefinition = Fd();
        statsCard.Add(stat1);

        var stat2 = new Label($"民众信任度：{GameData.Trust} / 100（基准平稳）");
        stat2.style.fontSize = 14; stat2.style.color = GlassText; stat2.style.unityFontDefinition = Fd();
        stat2.style.marginTop = 6;
        statsCard.Add(stat2);

        var stat3 = new Label($"预期客流量：{GameData.ExpectedPassengers} 人次 / 日");
        stat3.style.fontSize = 14; stat3.style.color = GlassText; stat3.style.unityFontDefinition = Fd();
        stat3.style.marginTop = 6;
        statsCard.Add(stat3);

        // 快捷操作区
        var actCard = new VisualElement();
        actCard.style.flexGrow = 1;
        actCard.style.justifyContent = Justify.FlexEnd;
        leftCol.Add(actCard);

        var btnStory = new Button(() => { s_openStory?.Invoke(); }) { text = "进入剧情 · Main Story" };
        btnStory.style.height = 46;
        btnStory.style.fontSize = 16;
        btnStory.style.unityFontDefinition = Fd();
        btnStory.style.backgroundColor = new Color(0.18f, 0.35f, 0.58f, 0.75f);
        btnStory.style.color = Color.white;
        btnStory.style.borderTopLeftRadius = 12; btnStory.style.borderTopRightRadius = 12;
        btnStory.style.borderBottomLeftRadius = 12; btnStory.style.borderBottomRightRadius = 12;
        btnStory.style.marginBottom = 10;
        actCard.Add(btnStory);

        var btnAffairs = new Button(() => { s_openAffairs?.Invoke(); }) { text = "站务大厅 · 事务管理" };
        btnAffairs.style.height = 46;
        btnAffairs.style.fontSize = 16;
        btnAffairs.style.unityFontDefinition = Fd();
        btnAffairs.style.backgroundColor = new Color(0.24f, 0.25f, 0.32f, 0.65f);
        btnAffairs.style.color = GlassText;
        btnAffairs.style.borderTopLeftRadius = 12; btnAffairs.style.borderTopRightRadius = 12;
        btnAffairs.style.borderBottomLeftRadius = 12; btnAffairs.style.borderBottomRightRadius = 12;
        actCard.Add(btnAffairs);

        // 右栏：58% 宽度，运行方案选择 + 待办与事件
        var rightCol = new VisualElement();
        rightCol.style.width = new Length(58, LengthUnit.Percent);
        rightCol.style.flexDirection = FlexDirection.Column;
        columns.Add(rightCol);

        // 方案选择卡片
        var planBox = new VisualElement();
        planBox.style.backgroundColor = GlassBgSoft;
        planBox.style.borderTopLeftRadius = 16; planBox.style.borderTopRightRadius = 16;
        planBox.style.borderBottomLeftRadius = 16; planBox.style.borderBottomRightRadius = 16;
        planBox.style.borderTopWidth = 1; planBox.style.borderBottomWidth = 1;
        planBox.style.borderLeftWidth = 1; planBox.style.borderRightWidth = 1;
        planBox.style.borderTopColor = GlassBorderSoft; planBox.style.borderBottomColor = GlassBorderSoft;
        planBox.style.borderLeftColor = GlassBorderSoft; planBox.style.borderRightColor = GlassBorderSoft;
        planBox.style.paddingLeft = 18; planBox.style.paddingRight = 18;
        planBox.style.paddingTop = 14; planBox.style.paddingBottom = 14;
        planBox.style.marginBottom = 14;
        rightCol.Add(planBox);

        var planTitle = new Label("今日运行方案制定");
        planTitle.style.fontSize = 16; planTitle.style.color = GlassAccent; planTitle.style.unityFontDefinition = Fd();
        planBox.Add(planTitle);

        for (int i = 0; i < GameData.DispatchPlans.Length; i++)
        {
            var plan = GameData.DispatchPlans[i];
            bool isSelected = (GameData.SelectedDispatchPlanIndex == i);

            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.justifyContent = Justify.SpaceBetween;
            item.style.alignItems = Align.Center;
            item.style.backgroundColor = isSelected ? new Color(0.2f, 0.35f, 0.5f, 0.45f) : new Color(0.15f, 0.15f, 0.2f, 0.35f);
            item.style.borderTopLeftRadius = 10; item.style.borderTopRightRadius = 10;
            item.style.borderBottomLeftRadius = 10; item.style.borderBottomRightRadius = 10;
            item.style.borderTopWidth = 1; item.style.borderBottomWidth = 1;
            item.style.borderLeftWidth = 1; item.style.borderRightWidth = 1;
            item.style.borderTopColor = isSelected ? GlassAccent : Color.clear;
            item.style.borderBottomColor = isSelected ? GlassAccent : Color.clear;
            item.style.borderLeftColor = isSelected ? GlassAccent : Color.clear;
            item.style.borderRightColor = isSelected ? GlassAccent : Color.clear;
            item.style.paddingLeft = 12; item.style.paddingRight = 12;
            item.style.paddingTop = 10; item.style.paddingBottom = 10;
            item.style.marginTop = 8;
            item.pickingMode = PickingMode.Position;
            planBox.Add(item);

            var leftPart = new VisualElement();
            item.Add(leftPart);

            var pName = new Label((isSelected ? "● " : "○ ") + plan.Name);
            pName.style.fontSize = 15; pName.style.color = isSelected ? GlassAccent : GlassText;
            pName.style.unityFontDefinition = Fd();
            leftPart.Add(pName);

            var pDesc = new Label(plan.Summary);
            pDesc.style.fontSize = 12; pDesc.style.color = GlassTextDim; pDesc.style.unityFontDefinition = Fd();
            pDesc.style.marginTop = 2;
            leftPart.Add(pDesc);

            var rightPart = new Label($"资金 {(plan.MoneyDelta >= 0 ? "+" : "")}{plan.MoneyDelta}   车况 {(plan.TrainConditionDelta >= 0 ? "+" : "")}{plan.TrainConditionDelta}");
            rightPart.style.fontSize = 12; rightPart.style.color = GlassTextDim; rightPart.style.unityFontDefinition = Fd();
            item.Add(rightPart);

            item.RegisterCallback<ClickEvent>(_ =>
            {
                ShowToast($"当前方案：{plan.Name}");
            });
        }

        // 简报与待办清单
        var eventBox = new VisualElement();
        eventBox.style.flexGrow = 1;
        eventBox.style.backgroundColor = GlassBgSoft;
        eventBox.style.borderTopLeftRadius = 16; eventBox.style.borderTopRightRadius = 16;
        eventBox.style.borderBottomLeftRadius = 16; eventBox.style.borderBottomRightRadius = 16;
        eventBox.style.borderTopWidth = 1; eventBox.style.borderBottomWidth = 1;
        eventBox.style.borderLeftWidth = 1; eventBox.style.borderRightWidth = 1;
        eventBox.style.borderTopColor = GlassBorderSoft; eventBox.style.borderBottomColor = GlassBorderSoft;
        eventBox.style.borderLeftColor = GlassBorderSoft; eventBox.style.borderRightColor = GlassBorderSoft;
        eventBox.style.paddingLeft = 18; eventBox.style.paddingRight = 18;
        eventBox.style.paddingTop = 14; eventBox.style.paddingBottom = 14;
        rightCol.Add(eventBox);

        var eventTitle = new Label("值班简报与待办清单");
        eventTitle.style.fontSize = 16; eventTitle.style.color = GlassAccent; eventTitle.style.unityFontDefinition = Fd();
        eventBox.Add(eventTitle);

        var todos = GameData.Todos;
        if (todos != null && todos.Count > 0)
        {
            foreach (var td in todos)
            {
                var row = new Label("▫ " + td);
                row.style.fontSize = 13; row.style.color = GlassText; row.style.unityFontDefinition = Fd();
                row.style.marginTop = 6;
                eventBox.Add(row);
            }
        }
        else
        {
            string[] defaultNotices = {
                "▫ 早间道尺巡查完成：雾峰村道口几何尺寸在允差内",
                "▫ 0721号水箱水位正常，已加注沙能过滤液",
                "▫ 松桥站临时道口通行许可已向村委会备案"
            };
            foreach (var dn in defaultNotices)
            {
                var row = new Label(dn);
                row.style.fontSize = 13; row.style.color = GlassText; row.style.unityFontDefinition = Fd();
                row.style.marginTop = 6;
                eventBox.Add(row);
            }
        }

        var footer = new Label("阿里郎内网 RDA-v4.2 认证通过 · 嘉颖徐铁路基金技术支持");
        footer.style.fontSize = 11; footer.style.color = GlassTextMuted; footer.style.unityFontDefinition = Fd();
        footer.style.marginTop = 12;
        eventBox.Add(footer);
    }


    }
}
