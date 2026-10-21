using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>
    /// 平板轻量系统工具集（时钟、日历、天气、计算器、米家遥测、安全、网络、应用商店、地图与占位等）
    /// 从 GameMainUI 剥离（T306 Batch 1-D）
    /// </summary>
    public static class TabletSystemToolsAppUI
    {
    private static Action<string> s_showToast;

    private static void ShowToast(string msg)
    {
        if (s_showToast != null) s_showToast(msg);
        else Debug.Log("[SystemTools] " + msg);
    }

    public static void SetToastHandler(Action<string> showToast)
    {
        s_showToast = showToast;
    }

    // ============ App: 时钟 ============
    public static void BuildClockApp(VisualElement win)
    {
        var body = BuildAppBody(win, "时钟", "阿里郎高精度授时中心 · 运行授时服务");

        // 大表盘卡片
        var clockCard = BuildAppCard();
        clockCard.style.alignItems = Align.Center;
        clockCard.style.paddingTop = 24; clockCard.style.paddingBottom = 24;

        var time = new Label(System.DateTime.Now.ToString("HH:mm:ss"));
        time.style.fontSize = 54; time.style.color = InkPrimary;
        time.style.unityFontStyleAndWeight = FontStyle.Bold;
        time.style.unityFontDefinition = Fd();
        clockCard.Add(time);

        var date = new Label($"运营第 {GameData.Day} 天 · 主体历 105 年 1 月 1 日");
        date.style.fontSize = 14; date.style.color = AccentSystem;
        date.style.unityFontDefinition = Fd(); date.style.marginTop = 8;
        clockCard.Add(date);
        body.Add(clockCard);

        // 分组卡片：功能Tab（闹钟/世界时钟/秒表/倒计时）
        var funcCard = BuildAppCard("时钟功能");
        funcCard.Add(BuildLightRow("⏰ 晨间出车早擎闹钟", "已设定 05:30（工作日）", () => ShowToast("闹钟已同步到先民排班系统")));
        funcCard.Add(BuildLightRow("🌐 平壤内网主时钟同步", "偏移量 +0.002s · 阿里郎1号卫星校准", () => ShowToast("授时服务器连接稳定")));
        funcCard.Add(BuildLightRow("⏱ 运行耗时秒表", "用于测定雾峰站至茶山站实际单程区间时分", () => ShowToast("秒表功能就绪")));
        body.Add(funcCard);
    }

    // ============ App: 日历 ============
    public static void BuildCalendarApp(VisualElement win)
    {
        var body = BuildAppBody(win, "日历", "主体历 105 年 1 月（公历 2076 年 1 月）");

        var calCard = BuildAppCard("月度运行视图");
        calCard.style.alignSelf = Align.Center;
        calCard.style.width = 680;

        string[] days = { "一", "二", "三", "四", "五", "六", "日" };
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.marginBottom = 10;
        calCard.Add(header);
        foreach (var d in days)
        {
            var dl = new Label(d);
            dl.style.width = 85; dl.style.fontSize = 13; dl.style.color = InkSecondary;
            dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(dl);
        }

        // 5周网格，完整呈现 31 天
        for (int w = 0; w < 5; w++)
        {
            var week = new VisualElement();
            week.style.flexDirection = FlexDirection.Row;
            week.style.justifyContent = Justify.SpaceBetween;
            week.style.marginTop = 6;
            calCard.Add(week);
            for (int d = 0; d < 7; d++)
            {
                int day = w * 7 + d + 1;
                var dl = new Label(day <= 31 ? day.ToString() : "");
                dl.style.width = 85; dl.style.height = 38; dl.style.fontSize = 14;
                dl.style.unityFontDefinition = Fd(); dl.style.unityTextAlign = TextAnchor.MiddleCenter;

                if (day <= 31)
                {
                    bool isCurrentDay = (day == GameData.Day || (GameData.Day > 31 && day == 1));
                    dl.style.color = isCurrentDay ? Color.white : InkPrimary;
                    if (isCurrentDay)
                    {
                        dl.style.backgroundColor = AccentSystem;
                        dl.style.borderTopLeftRadius = 8; dl.style.borderTopRightRadius = 8;
                        dl.style.borderBottomLeftRadius = 8; dl.style.borderBottomRightRadius = 8;
                    }
                }
                else
                {
                    dl.style.color = Color.clear;
                }
                week.Add(dl);
            }
        }
        body.Add(calCard);

        var scheduleCard = BuildAppCard("今日排程备忘");
        scheduleCard.Add(BuildLightRow("08:00 晨间第一班客运列车出库", "线路巡道工已完成万能道尺轨距巡检", null, false));
        scheduleCard.Add(BuildLightRow("12:30 乌金岭矿区沙币结算单核验", "嘉颖徐铁路基金派员进驻", null, false));
        body.Add(scheduleCard);
    }

    // ============ App: 天气 ============
    public static void BuildWeatherApp(VisualElement win)
    {
        var body = BuildAppBody(win, "气象监视", "雾峰村气象站 · 实时气象与水文参数");

        var weatherCard = BuildAppCard();
        weatherCard.style.alignItems = Align.Center;
        weatherCard.style.paddingTop = 20; weatherCard.style.paddingBottom = 20;

        var temp = new Label("18°C");
        temp.style.fontSize = 54; temp.style.color = InkPrimary;
        temp.style.unityFontStyleAndWeight = FontStyle.Bold;
        temp.style.unityFontDefinition = Fd();
        weatherCard.Add(temp);

        var cond = new Label("☁ 多云 · 东北风 2级 · 湿度 62%");
        cond.style.fontSize = 15; cond.style.color = InkSecondary;
        cond.style.unityFontDefinition = Fd(); cond.style.marginTop = 6;
        weatherCard.Add(cond);
        body.Add(weatherCard);

        var impactCard = BuildAppCard("铁路运营与沙能影响评估");
        impactCard.Add(BuildLightRow("轨面附着系数", "良好 · 干燥无沙尘暴影响", null, false));
        impactCard.Add(BuildLightRow("沙能载具影响", "沙粒流转阻力 0.12 · 竞争对手运力平稳", null, false));
        impactCard.Add(BuildLightRow("未来24小时预警", "明日午后有薄雾，建议提早开启0721机车大灯", null, false));
        body.Add(impactCard);

        var forecastCard = BuildAppCard("多日天气预报");
        forecastCard.Add(BuildLightRow("周一（今日）", "多云 18°C / 9°C · 优", null, false));
        forecastCard.Add(BuildLightRow("周二（明日）", "晴间多云 21°C / 11°C · 优", null, false));
        forecastCard.Add(BuildLightRow("周三（后日）", "小雨转阴 15°C / 8°C · 轨面湿滑注意制动", null, false));
        body.Add(forecastCard);
    }

    // ============ App: 计算器 ============
    public static void BuildCalculatorApp(VisualElement win)
    {
        var body = BuildAppBody(win, "运价计算器", "客货运里程运价核算与沙币换算");

        var card = BuildAppCard();
        card.style.width = 420; card.style.alignSelf = Align.Center;

        var display = new Label("0");
        display.style.fontSize = 36; display.style.color = InkPrimary;
        display.style.unityFontStyleAndWeight = FontStyle.Bold;
        display.style.unityFontDefinition = Fd();
        display.style.height = 54; display.style.unityTextAlign = TextAnchor.MiddleRight;
        display.style.paddingRight = 10;
        display.style.marginBottom = 12;
        display.style.backgroundColor = SurfaceApp;
        display.style.borderTopLeftRadius = 8; display.style.borderTopRightRadius = 8;
        display.style.borderBottomLeftRadius = 8; display.style.borderBottomRightRadius = 8;
        card.Add(display);

        string[][] rows = {
            new[]{"7","8","9","÷"},
            new[]{"4","5","6","×"},
            new[]{"1","2","3","-"},
            new[]{"0",".","=","+"},
        };
        foreach (var row in rows)
        {
            var btnRow = new VisualElement(); btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop = 6; btnRow.style.justifyContent = Justify.SpaceBetween;
            card.Add(btnRow);
            foreach (var b in row)
            {
                var btn = new Button(() => { }) { text = b };
                btn.style.width = 88; btn.style.height = 46; btn.style.fontSize = 18;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter; btn.style.unityFontDefinition = Fd();
                btn.style.backgroundColor = b == "=" ? AccentSystem : SurfaceHover;
                btn.style.color = b == "=" ? Color.white : InkPrimary;
                btn.style.borderTopLeftRadius = 8; btn.style.borderTopRightRadius = 8;
                btn.style.borderBottomLeftRadius = 8; btn.style.borderBottomRightRadius = 8;
                btn.style.borderTopWidth = 0; btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0; btn.style.borderRightWidth = 0;
                btnRow.Add(btn);
            }
        }
        body.Add(card);
    }

    // ============ App: 米家（0721遥测）============
    public static void BuildMijiaApp(VisualElement win)
    {
        var body = BuildAppBody(win, "机车遥测", "米家物联网 · 0721型「沙子飞猪号」机车网关");

        var statusCard = BuildAppCard("动力单元遥测总览");
        statusCard.Add(BuildLightRow("沙能储箱余量", "1,200 / 1,500 kg (80%)", null, false));
        statusCard.Add(BuildLightRow("动力电池组蓄电量", "78% · 正常放电", null, false));
        statusCard.Add(BuildLightRow("预估安全运行里程", "340 km", null, false));
        statusCard.Add(BuildLightRow("本月累计沙耗", "312 kg 先民高纯度动力沙", null, false));
        body.Add(statusCard);

        var deviceCard = BuildAppCard("机载智能节点");
        deviceCard.Add(BuildLightRow("万能道尺无线遥测模块", "已连接 · 精度 ±0.5mm", () => ShowToast("道尺信号正常")));
        deviceCard.Add(BuildLightRow("机车行车记录仪", "循环录像中 · 存储剩余 18.4GB", () => ShowToast("录像文件保护已开启")));
        deviceCard.Add(BuildLightRow("轴承振动监视传感器", "全轴正常 · 无偏磨", () => ShowToast("机修工张工巡检记录已同步")));
        body.Add(deviceCard);
    }

    // ============ App: 安全中心 ============
    public static void BuildSecurityApp(VisualElement win)
    {
        var body = BuildAppBody(win, "安全中心", "阿里郎终端安全与内网防护系统");

        var scoreCard = BuildAppCard();
        scoreCard.style.alignItems = Align.Center;
        scoreCard.style.paddingTop = 16; scoreCard.style.paddingBottom = 16;

        var shield = new Label("🛡");
        shield.style.fontSize = 44; shield.style.unityFontDefinition = Fd();
        scoreCard.Add(shield);

        var score = new Label("100 分 · 系统处于安全状态");
        score.style.fontSize = 18; score.style.color = new Color(0.2f, 0.65f, 0.3f, 1f);
        score.style.unityFontStyleAndWeight = FontStyle.Bold;
        score.style.unityFontDefinition = Fd(); score.style.marginTop = 6;
        scoreCard.Add(score);

        var scanBtn = new Button(() => ShowToast("系统已全面扫描：未发现安全漏洞")) { text = "一键优化与垃圾清理" };
        scanBtn.style.marginTop = 12; scanBtn.style.height = 36;
        scanBtn.style.paddingLeft = 18; scanBtn.style.paddingRight = 18;
        scanBtn.style.backgroundColor = AccentSystem; scanBtn.style.color = Color.white;
        scanBtn.style.borderTopLeftRadius = 18; scanBtn.style.borderTopRightRadius = 18;
        scanBtn.style.borderBottomLeftRadius = 18; scanBtn.style.borderBottomRightRadius = 18;
        scanBtn.style.fontSize = 14; scanBtn.style.unityFontDefinition = Fd();
        scoreCard.Add(scanBtn);
        body.Add(scoreCard);

        var listCard = BuildAppCard("安全防御组件");
        listCard.Add(BuildLightRow("病毒与木马查杀", "病毒库版本：2076.01.10 最新", () => ShowToast("病毒库为最新版")));
        listCard.Add(BuildLightRow("应用权限防护", "22 个应用已通过安全沙箱审核", () => ShowToast("权限配置安全")));
        listCard.Add(BuildLightRow("骚扰拦截与通信防火墙", "外部未知频段电波已被屏蔽", () => ShowToast("防火墙运行中")));
        body.Add(listCard);
    }

    // ============ App: 浏览器 ============
    public static void BuildBrowserApp(VisualElement win)
    {
        var body = BuildAppBody(win, "光明浏览器", "阿里郎光明内网 · 铁路文库与门户");

        var searchCard = BuildAppCard();
        var searchRow = new VisualElement();
        searchRow.style.flexDirection = FlexDirection.Row;
        searchRow.style.alignItems = Align.Center;
        searchRow.style.backgroundColor = SurfaceApp;
        searchRow.style.paddingLeft = 12; searchRow.style.paddingRight = 12;
        searchRow.style.height = 38;
        searchRow.style.borderTopLeftRadius = 19; searchRow.style.borderTopRightRadius = 19;
        searchRow.style.borderBottomLeftRadius = 19; searchRow.style.borderBottomRightRadius = 19;
        searchCard.Add(searchRow);

        var searchTxt = new Label("🔒 https://jiayingxu.railway-foundation.org");
        searchTxt.style.fontSize = 13; searchTxt.style.color = InkSecondary; searchTxt.style.unityFontDefinition = Fd();
        searchRow.Add(searchTxt);
        body.Add(searchCard);

        var homeCard = BuildAppCard("推荐门户与馆藏");
        homeCard.Add(BuildLightRow("🏛 嘉颖徐铁路基金官方站点", "全球铁路文化遗产与沙能冲击研究论文库", () => ShowToast("已连接嘉颖徐基金知识库")));
        homeCard.Add(BuildLightRow("📖 金日成综合大学铁道系期刊", "0721型机车先民内燃机图纸与热效率分析", () => ShowToast("论文文献已加载")));
        homeCard.Add(BuildLightRow("🗺 雾峰至乌金岭矿区综合勘察志", "地质构造、废弃线路复旧方案纪实", () => ShowToast("勘察志阅读就绪")));
        body.Add(homeCard);
    }

    // ============ App: 应用商店 ============
    public static void BuildAppStoreApp(VisualElement win)
    {
        var body = BuildAppBody(win, "应用市场", "精选工具与经过审计的应用");

        var listCard = BuildAppCard("已上架应用");
        listCard.Add(BuildLightRow("铁路工程计算器 v2.1", "已安装 · 专用于道岔曲率与轨距计算", () => ShowToast("应用已是最新版")));
        listCard.Add(BuildLightRow("列车时刻表 v1.5", "已安装 · 雾峰村线专用发车排班表", () => ShowToast("应用已是最新版")));
        listCard.Add(BuildLightRow("轨道检测工具 v3.0", "已安装 · 万能道尺蓝牙配对应用", () => ShowToast("应用已是最新版")));
        listCard.Add(BuildLightRow("铁路英语词典 v1.0", "已安装 · 先民机车术语英汉对照", () => ShowToast("应用已是最新版")));
        body.Add(listCard);
    }

    // ============ App: 阿里郎商店 ============
    public static void BuildArirangStoreApp(VisualElement win)
    {
        var body = BuildAppBody(win, "阿里郎商店", "国营物资订购与官方数字签名应用市场");

        var listCard = BuildAppCard("认证应用列表");
        listCard.Add(BuildLightRow("阿里郎新闻 v1.0", "官方媒体每日通报 · ★★★★★", () => ShowToast("已核验数字证书")));
        listCard.Add(BuildLightRow("主体百科 v2.0", "先民百科与工农知识库 · ★★★★★", () => ShowToast("已核验数字证书")));
        listCard.Add(BuildLightRow("朝鲜音乐 v1.3", "革命进行曲与铁路工人联欢曲 · ★★★★★", () => ShowToast("已核验数字证书")));
        listCard.Add(BuildLightRow("白头教育 v3.0", "铁路工程技术培训与考试系统 · ★★★★★", () => ShowToast("已核验数字证书")));
        body.Add(listCard);
    }

    // ============ App: 未来网 ============
    public static void BuildMiraeNetApp(VisualElement win)
    {
        var body = BuildAppBody(win, "未来网", "mirae.kp · 科教文卫国家内网接入端");

        var listCard = BuildAppCard("国家网络节点");
        listCard.Add(BuildLightRow("金日成综合大学 · 校园网接入", "林彪悍的研究生学术档案与导师信箱", () => ShowToast("导师信箱当前无新邮件")));
        listCard.Add(BuildLightRow("沙能会社门户 · USET企业网", "沙本位能源每日挂牌交易牌价与行情", () => ShowToast("沙币牌价挂牌中")));
        listCard.Add(BuildLightRow("平壤铁道省技术总局", "铁路支线特许经营准入核验系统", () => ShowToast("特许准入证书有效")));
        body.Add(listCard);
    }

    // ============ App: 白头疫苗 ============
    public static void BuildSiliVaccineApp(VisualElement win)
    {
        var body = BuildAppBody(win, "白头疫苗", "内网终极防御系统 · 驱动级沙盒引擎");

        var statCard = BuildAppCard("防护引擎状态");
        statCard.Add(BuildLightRow("防护模式", "实时防御已开启（锁定不可关闭）", null, false));
        statCard.Add(BuildLightRow("特征码库大小", "0 KB（内网完全免疫未知外来攻击）", null, false));
        statCard.Add(BuildLightRow("扫描引擎校验", "通过 · 阿里郎OS系统级深度挂钩", () => ShowToast("防护引擎运转正常")));
        body.Add(statCard);
    }

    // ============ App: 文件管理 ============
    public static void BuildFileManagerApp(VisualElement win)
    {
        var body = BuildAppBody(win, "文件管理", "内部存储空间 128GB · 剩余可用 92.4GB");

        var catCard = BuildAppCard("文件分类");
        catCard.Add(BuildLightRow("📁 文档 (12 项)", "包含站务日志、道尺检修表与班组排班表", () => ShowToast("文档目录就绪")));
        catCard.Add(BuildLightRow("🖼 图片 (38 项)", "车站风景、通车剪彩与相册剧照", () => ShowToast("图片目录就绪")));
        catCard.Add(BuildLightRow("🎵 音频 (8 项)", "机车鸣笛声、铁道广播与随身听曲目", () => ShowToast("音频目录就绪")));
        catCard.Add(BuildLightRow("💾 存档 (6 槽位)", "本地游戏进度快照与剧情回顾标记", () => ShowToast("存档目录正常")));
        body.Add(catCard);
    }

    // ============ App: 地图 ============
    public static void BuildMapApp(VisualElement win)
    {
        var body = BuildAppBody(win, "铁路地图", "雾峰支线与周边干线走向规划图");

        var mapCard = BuildAppCard("铁路线路总览");
        var map = new Label(
            "  雾峰村 ─── 8km ─── 茶山站 ─── 7km ─── 松桥站 ─── 8km ─── 矿区站\n" +
            "  │\n" +
            "  └── 45km ─── 青溪镇（规划中）\n" +
            "       └── 80km ─── 云渡港（待解锁）");
        map.style.fontSize = 13; map.style.color = InkPrimary;
        map.style.unityFontDefinition = Fd();
        map.style.whiteSpace = WhiteSpace.Pre;
        map.style.paddingTop = 10; map.style.paddingBottom = 10;
        mapCard.Add(map);
        body.Add(mapCard);

        var infoCard = BuildAppCard("区域沙能渗透监视");
        infoCard.Add(BuildLightRow("雾峰村", "沙能渗透率 15% · 传统铁路基本盘坚固", null, false));
        infoCard.Add(BuildLightRow("乌金岭矿区", "沙能渗透率 20% · 矿商倾向重载货运", null, false));
        infoCard.Add(BuildLightRow("青溪镇", "沙能渗透率 25% · 沙能公路客运竞争激烈", null, false));
        body.Add(infoCard);
    }

    // ============ App: 占位符 ============
    public static void BuildPlaceholderApp(VisualElement win, string name)
    {
        var body = BuildAppBody(win, name, "系统官方规划应用");
        var card = BuildAppCard();
        card.style.alignItems = Align.Center;
        card.style.paddingTop = 32; card.style.paddingBottom = 32;

        var icon = new Label("⏳");
        icon.style.fontSize = 40; icon.style.unityFontDefinition = Fd();
        card.Add(icon);

        var t = new Label($"{name} · 即将推出");
        t.style.fontSize = 18; t.style.color = InkPrimary;
        t.style.unityFontStyleAndWeight = FontStyle.Bold;
        t.style.unityFontDefinition = Fd(); t.style.marginTop = 10;
        card.Add(t);

        var sub = new Label("该模块正由阿里郎工程师适配中，请随着经营天数推进解锁。");
        sub.style.fontSize = 13; sub.style.color = InkSecondary;
        sub.style.unityFontDefinition = Fd(); sub.style.marginTop = 6;
        card.Add(sub);

        body.Add(card);
    }


    }
}
