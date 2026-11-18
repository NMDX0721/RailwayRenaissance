using System;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.SystemShell
{
    /// <summary>
    /// 平板的连接状态（游戏世界内的设定）：Wi-Fi、移动网络（高丽联）、蓝牙、飞行模式、省电。
    /// 这些开关保存偏好并驱动控制中心的状态图标；飞行模式会让另外三项在界面上呈关闭，退出后恢复各自原来的设置。
    /// </summary>
    public static class TabletConnectivity
    {
        public const string CarrierName = "高丽联";
        public const string CarrierNetwork = "3G";
        public const string WifiName = "铁路职工网";

        private static bool s_loaded;
        private static bool s_wifi = true, s_mobile = true, s_bluetooth, s_airplane, s_saver;
        public static event Action Changed;

        public static bool Airplane { get { Load(); return s_airplane; } }
        public static bool WifiSwitch { get { Load(); return s_wifi; } }
        public static bool MobileSwitch { get { Load(); return s_mobile; } }
        public static bool BluetoothSwitch { get { Load(); return s_bluetooth; } }
        public static bool BatterySaver { get { Load(); return s_saver; } }

        /// <summary>界面上实际算作“已连接”的状态：飞行模式下全部为关。</summary>
        public static bool WifiOn => WifiSwitch && !Airplane;
        public static bool MobileOn => MobileSwitch && !Airplane;
        public static bool BluetoothOn => BluetoothSwitch && !Airplane;

        public static void SetWifi(bool v) { Load(); Set(ref s_wifi, v, "TabletConnWifi"); }
        public static void SetMobile(bool v) { Load(); Set(ref s_mobile, v, "TabletConnMobile"); }
        public static void SetBluetooth(bool v) { Load(); Set(ref s_bluetooth, v, "TabletConnBluetooth"); }
        public static void SetAirplane(bool v) { Load(); Set(ref s_airplane, v, "TabletConnAirplane"); }

        /// <summary>省电：真实生效——亮度压到 60% 以下，调用方再减弱动效；关闭时不改回，由用户自己调。</summary>
        public static void SetBatterySaver(bool v)
        {
            Load();
            Set(ref s_saver, v, "TabletConnSaver");
            if (v && TabletDisplayPreferences.Brightness > 0.6f) TabletDisplayPreferences.SetBrightness(0.6f);
        }

        private static void Set(ref bool field, bool v, string key)
        {
            if (field == v) return;
            field = v;
            try { PlayerPrefs.SetInt(key, v ? 1 : 0); PlayerPrefs.Save(); } catch { }
            Changed?.Invoke();
        }

        private static void Load()
        {
            if (s_loaded) return;
            s_loaded = true;
            try
            {
                s_wifi = PlayerPrefs.GetInt("TabletConnWifi", 1) == 1;
                s_mobile = PlayerPrefs.GetInt("TabletConnMobile", 1) == 1;
                s_bluetooth = PlayerPrefs.GetInt("TabletConnBluetooth", 0) == 1;
                s_airplane = PlayerPrefs.GetInt("TabletConnAirplane", 0) == 1;
                s_saver = PlayerPrefs.GetInt("TabletConnSaver", 0) == 1;
            }
            catch { }
        }
    }
}
