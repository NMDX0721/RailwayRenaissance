using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板应用：相册（回忆画廊 · 历史车站与各时期CG灯箱）
    /// 从 GameMainUI 剥离（T306 Batch 1-B）
    /// </summary>
    public static class TabletGalleryAppUI
    {
    // ============ App: 相册（分类筛选 + 真实存档解锁）============
    private static int currentGalleryCategory = 0; // 0: 全部, 1: 历史车站, 2: 暮色与晨曦, 3: 车厢与机库

    public static readonly (string resId, string title, string desc, int cat)[] MasterGalleryItems = {
        ("songqiao_station", "松桥站 · 午后", "剥落红砖、生苔灰瓦、斜插狗尾草的旧铁牌。", 1),
        ("platform_evening", "雾峰站 · 暮色站台", "夕阳余晖斜照在石板上，远山如黛。", 2),
        ("platform_morning", "雾峰站 · 晨曦", "破晓的第一缕阳光穿透薄雾照耀钢轨。", 2),
        ("station", "雾峰站 · 正面全景", "老旧但依然挺立的车站主房，四年前的车站牌。", 1),
        ("station_sunset", "雾峰站 · 日落金辉", "金黄色的晚霞泼洒在梯田与铁路线上。", 2),
        ("station_office", "站务室 · 调度图", "木头老算盘、泛黄的日历与墙上的手绘时刻表。", 1),
        ("railway_track", "铁道延伸 · 枕木", "单线铁轨锈蚀严重，枕木缝里长出杂草与小黄花。", 1),
        ("depot_morning", "机务段车库 · 晨雾", "机修车间弥漫着机油与水汽的温润气息。", 3),
        ("hangar", "机车库 · 0721号", "停放着沉睡23年的先民动力机车。", 3),
        ("cabin_interior", "客厢白天 · 阳光", "暖阳靠窗木制长椅，绿皮革椅面有些磨损。", 3),
        ("tea_house", "茶室 · 梯田远眺", "山坡茶园梯田，茶树深绿弧形条带层层叠叠。", 1)
    };

    public static bool IsGalleryPhotoUnlocked(string resId)
    {
        if (resId == "songqiao_station") return true; // 序章默认解锁
        return PlayerPrefs.GetInt("ArchiveScene_" + resId, 0) == 1
            || PlayerPrefs.GetInt("ArchiveCG_" + resId, 0) == 1
            || PlayerPrefs.GetInt("VN_Got0721", 0) == 1;
    }

        private static VisualElement s_appLayer;
    private static Action<string> s_showToast;

    private static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[Gallery] " + msg);
    }

    public static void BuildGalleryApp(VisualElement win, VisualElement appLayer = null, Action<string> showToast = null)
    {
        if (appLayer != null) s_appLayer = appLayer;
        if (showToast != null) s_showToast = showToast;
        win.Clear();

        // 全屏画廊（左侧分类 + 右侧缩略图卡片网格 + 全屏灯箱预览）
        var rootRow = new VisualElement();
        rootRow.style.flexDirection = FlexDirection.Row;
        rootRow.style.flexGrow = 1;
        win.Add(rootRow);

        // 左侧分类导航
        var sideNav = new VisualElement();
        sideNav.style.width = 180;
        sideNav.style.flexDirection = FlexDirection.Column;
        sideNav.style.marginRight = 18;
        rootRow.Add(sideNav);

        string[] cats = { "全部相片", "历史车站", "暮色与晨曦", "车厢与机库" };
        var catBtns = new System.Collections.Generic.List<Button>();
        for (int i = 0; i < cats.Length; i++)
        {
            int catIdx = i;
            var btn = new Button(() =>
            {
                currentGalleryCategory = catIdx;
                BuildGalleryApp(win);
            }) { text = cats[i] };
            btn.style.height = 40;
            btn.style.fontSize = 14; btn.style.unityFontDefinition = Fd();
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.paddingLeft = 14;
            btn.style.backgroundColor = (i == currentGalleryCategory) ? BrownWarm : GlassBgSoft;
            btn.style.color = (i == currentGalleryCategory) ? GoldTrimColor : GlassText;
            btn.style.borderTopLeftRadius = 10; btn.style.borderBottomRightRadius = 10;
            btn.style.marginBottom = 8;
            sideNav.Add(btn);
            catBtns.Add(btn);
        }

        // 动态统计已收录场景数量
        int unlockedCount = 0;
        foreach (var item in MasterGalleryItems)
        {
            if (IsGalleryPhotoUnlocked(item.resId)) unlockedCount++;
        }

        var counter = new Label($"已收录场景：{unlockedCount} / {MasterGalleryItems.Length} 张");
        counter.style.fontSize = 12; counter.style.color = RicePaperLight; counter.style.unityFontDefinition = Fd();
        counter.style.marginTop = 12;
        sideNav.Add(counter);

        // 右侧网格（ScrollView滚动）
        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        rootRow.Add(scroll);

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        scroll.Add(grid);

        foreach (var item in MasterGalleryItems)
        {
            // 分类过滤
            if (currentGalleryCategory > 0 && item.cat != currentGalleryCategory) continue;

            bool isUnlocked = IsGalleryPhotoUnlocked(item.resId);

            var card = new VisualElement();
            card.style.width = 240; card.style.height = 150;
            card.style.marginRight = 14; card.style.marginBottom = 14;
            card.style.borderTopLeftRadius = 14; card.style.borderBottomRightRadius = 14;
            card.style.borderTopWidth = 1; card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1; card.style.borderRightWidth = 1;
            card.style.borderTopColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.borderBottomColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.borderLeftColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.borderRightColor = isUnlocked ? AmberAccent : GlassBorderSoft;
            card.style.backgroundColor = isUnlocked ? BrownDark : new Color(0.10f, 0.08f, 0.08f, 0.85f);
            card.style.overflow = Overflow.Hidden;
            card.style.justifyContent = Justify.FlexEnd;
            card.pickingMode = PickingMode.Position;
            grid.Add(card);

            if (isUnlocked)
            {
                var sp = Resources.Load<Sprite>("bg/" + item.resId);
                if (sp != null)
                {
                    var img = new Image { sprite = sp };
                    img.style.position = Position.Absolute;
                    img.style.top = 0; img.style.left = 0; img.style.right = 0; img.style.bottom = 0;
                    img.scaleMode = ScaleMode.ScaleAndCrop;
                    card.Add(img);
                }
            }
            else
            {
                // 未解锁锁态占位
                var lockBox = new VisualElement();
                lockBox.style.position = Position.Absolute;
                lockBox.style.top = 0; lockBox.style.left = 0; lockBox.style.right = 0; lockBox.style.bottom = 42;
                lockBox.style.alignItems = Align.Center;
                lockBox.style.justifyContent = Justify.Center;
                card.Add(lockBox);

                var lockIcon = new Label("🔒");
                lockIcon.style.fontSize = 28;
                lockBox.Add(lockIcon);

                var lockHint = new Label("通关剧情解锁");
                lockHint.style.fontSize = 11; lockHint.style.color = GlassTextMuted;
                lockHint.style.unityFontDefinition = Fd();
                lockHint.style.marginTop = 4;
                lockBox.Add(lockHint);
            }

            var bottomBar = new VisualElement();
            bottomBar.style.height = 42;
            bottomBar.style.backgroundColor = new Color(0.06f, 0.04f, 0.04f, 0.85f);
            bottomBar.style.paddingLeft = 10; bottomBar.style.paddingRight = 10;
            bottomBar.style.justifyContent = Justify.Center;
            card.Add(bottomBar);

            var cardTitle = new Label(isUnlocked ? item.title : "？？？");
            cardTitle.style.fontSize = 12;
            cardTitle.style.color = isUnlocked ? GoldTrimColor : GlassTextMuted;
            cardTitle.style.unityFontDefinition = Fd();
            bottomBar.Add(cardTitle);

            var cardSub = new Label(isUnlocked ? "点击查看大图" : "推进剧情解锁");
            cardSub.style.fontSize = 10;
            cardSub.style.color = isUnlocked ? RicePaperLight : GlassTextMuted;
            cardSub.style.unityFontDefinition = Fd();
            bottomBar.Add(cardSub);

            if (isUnlocked)
            {
                card.RegisterCallback<PointerEnterEvent>(_ => card.style.scale = new Scale(new Vector2(1.04f, 1.04f)));
                card.RegisterCallback<PointerLeaveEvent>(_ => card.style.scale = new Scale(new Vector2(1f, 1f)));

                string targetResId = item.resId;
                string targetTitle = item.title;
                string targetDesc = item.desc;
                card.RegisterCallback<ClickEvent>(_ => ShowGalleryLightbox(targetResId, targetTitle, targetDesc));
            }
            else
            {
                card.RegisterCallback<ClickEvent>(_ => ShowToast("该回忆场景尚未解锁，请在「归途」中推进剧情。"));
            }
        }
    }

    private static void ShowGalleryLightbox(string resId, string title, string desc)
    {
        var lightbox = new VisualElement { name = "gallery-lightbox" };
        lightbox.style.position = Position.Absolute;
        lightbox.style.top = 0; lightbox.style.left = 0; lightbox.style.right = 0; lightbox.style.bottom = 0;
        lightbox.style.backgroundColor = new Color(0f, 0f, 0f, 0.88f);
        lightbox.style.alignItems = Align.Center;
        lightbox.style.justifyContent = Justify.Center;
        s_appLayer.Add(lightbox);

        lightbox.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == lightbox) s_appLayer.Remove(lightbox);
        });

        var sp = Resources.Load<Sprite>("bg/" + resId);
        if (sp != null)
        {
            var bigImg = new Image { sprite = sp };
            bigImg.style.width = 760; bigImg.style.height = 430;
            bigImg.scaleMode = ScaleMode.ScaleToFit;
            bigImg.style.borderTopLeftRadius = 14; bigImg.style.borderTopRightRadius = 14;
            bigImg.style.borderBottomLeftRadius = 14; bigImg.style.borderBottomRightRadius = 14;
            lightbox.Add(bigImg);
        }

        var infoCard = new VisualElement();
        infoCard.style.width = 760;
        infoCard.style.backgroundColor = new Color(0.1f, 0.1f, 0.14f, 0.9f);
        infoCard.style.borderTopLeftRadius = 12; infoCard.style.borderTopRightRadius = 12;
        infoCard.style.borderBottomLeftRadius = 12; infoCard.style.borderBottomRightRadius = 12;
        infoCard.style.paddingLeft = 18; infoCard.style.paddingRight = 18;
        infoCard.style.paddingTop = 10; infoCard.style.paddingBottom = 10;
        infoCard.style.marginTop = 12;
        infoCard.style.flexDirection = FlexDirection.Row;
        infoCard.style.justifyContent = Justify.SpaceBetween;
        infoCard.style.alignItems = Align.Center;
        lightbox.Add(infoCard);

        var textCol = new VisualElement();
        infoCard.Add(textCol);

        var t = new Label(title);
        t.style.fontSize = 16; t.style.color = GlassAccent; t.style.unityFontDefinition = Fd();
        textCol.Add(t);

        var d = new Label(desc);
        d.style.fontSize = 12; d.style.color = GlassTextDim; d.style.unityFontDefinition = Fd();
        d.style.marginTop = 2;
        textCol.Add(d);

        var close = new Button(() => s_appLayer.Remove(lightbox)) { text = "关闭预览" };
        close.style.height = 32; close.style.paddingLeft = 14; close.style.paddingRight = 14;
        close.style.fontSize = 12; close.style.unityFontDefinition = Fd();
        close.style.backgroundColor = new Color(0.25f, 0.25f, 0.32f, 0.7f);
        close.style.color = GlassText;
        close.style.borderTopLeftRadius = 8; close.style.borderTopRightRadius = 8;
        close.style.borderBottomLeftRadius = 8; close.style.borderBottomRightRadius = 8;
        infoCard.Add(close);
    }


    }
}
