using System;
using System.Collections.Generic;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    // A small word cutter for Chinese lyrics, used to group characters into stage units and to weigh estimated word times.
    // It is a longest-match pass over a built-in list of common words and lyric words, then a few grammar rules for what the list does not know:
    // 的/了/着/吗 stick to the word before, 不/没/别 to the word after, and what is left is paired up. It is not a full dictionary.
    public static class MusicChineseWords
    {
        const int LongestWord = 4;
        const string Trailing = "的了着过吗呢吧啊呀啦嘛哦哟哇呐";
        const string Leading = "不没别";
        static readonly HashSet<string> Words = new HashSet<string>(
            ("我们 你们 他们 她们 它们 自己 大家 什么 怎么 怎样 为何 为什么 因为 所以 如果 假如 但是 可是 然后 还是 或者 虽然 已经 终于 依然 仍然 总是 永远 一直 从来 突然 忽然 慢慢 轻轻 静静 悄悄 默默 深深 远远 渐渐 缓缓 " +
             "世界 时间 时光 岁月 年华 青春 少年 少女 孩子 爱人 恋人 朋友 母亲 父亲 妈妈 爸爸 故乡 家乡 远方 天空 大地 星空 星光 月光 阳光 夕阳 黄昏 清晨 夜晚 黑夜 白天 白云 蓝天 大海 海风 海浪 河流 山川 高山 森林 花朵 花开 花落 雨水 雪花 风雨 风景 " +
             "梦想 梦境 梦里 梦中 回忆 记忆 思念 想念 感觉 心跳 心里 心中 心底 心情 内心 灵魂 生命 生活 人生 命运 幸福 快乐 悲伤 孤独 寂寞 温柔 眼泪 泪水 微笑 笑容 拥抱 亲吻 告别 离别 再见 重逢 相遇 相爱 相信 等待 守候 陪伴 " +
             "喜欢 爱情 爱着 爱你 恨你 想你 看见 听见 看到 听到 知道 明白 懂得 忘记 记得 记住 失去 拥有 得到 放弃 坚持 努力 勇敢 自由 飞翔 奔跑 前进 出发 到达 回来 回去 离开 走过 路过 经过 度过 穿过 越过 " +
             "列车 火车 铁路 铁轨 车站 站台 车厢 车窗 汽笛 远行 旅途 旅行 旅程 路上 路口 道路 方向 终点 起点 归途 信号 隧道 桥梁 钢铁 蒸汽 煤烟 " +
             "一个 一切 一起 一样 一直 一生 一天 一次 每个 每天 每一 没有 不会 不能 不要 不再 不是 不会 不曾 不见 不知 不管 不想 " +
             "现在 今天 明天 昨天 今晚 今夜 曾经 以后 以前 之后 之前 从前 未来 过去 当年 此刻 这里 那里 哪里 这样 那样 这个 那个 这些 那些 " +
             "美丽 漂亮 温暖 寒冷 明亮 黑暗 光明 光芒 光亮 影子 阴影 颜色 声音 歌声 笑声 哭泣 沉默 安静 热闹 简单 复杂 容易 " +
             "我的 你的 他的 她的 我是 你是 我要 我想 我爱 我在 你在 我会 你会 就像 好像 仿佛 犹如 如同 一如 不如 " +
             "天涯 海角 天堂 地狱 人间 红尘 江湖 故事 传说 童话 诗歌 歌曲 旋律 琴声 钟声 脚步 背影 身影 面孔 眼睛 双手 双眼 目光 额头 嘴角 头发 肩膀 怀抱 手心 掌心").Split(' '));

        /// <summary>Lengths of the words that cover the run of Han characters, in order; they add up to the run's length.</summary>
        public static List<int> Cut(string run)
        {
            var spans = new List<int>();
            int i = 0;
            while (i < run.Length)
            {
                int take = 0;
                for (int length = Math.Min(LongestWord, run.Length - i); length >= 2; length--)
                    if (Words.Contains(run.Substring(i, length))) { take = length; break; }
                if (take == 0) take = 1;
                spans.Add(take); i += take;
            }
            // What the list did not know: particles join the word before, negations the word after.
            var merged = new List<int>(); int at = 0;
            for (int s = 0; s < spans.Count; s++)
            {
                int length = spans[s]; char first = run[at];
                bool single = length == 1;
                if (single && Trailing.IndexOf(first) >= 0 && merged.Count > 0 && merged[merged.Count - 1] <= 3) merged[merged.Count - 1] += 1;
                else merged.Add(length);
                at += length;
            }
            spans = merged; merged = new List<int>(); at = 0;
            for (int s = 0; s < spans.Count; s++)
            {
                int length = spans[s];
                if (length == 1 && Leading.IndexOf(run[at]) >= 0 && s + 1 < spans.Count && spans[s + 1] <= 2) { merged.Add(1 + spans[s + 1]); at += 1 + spans[s + 1]; s++; continue; }
                merged.Add(length); at += length;
            }
            // Single characters left next to each other are paired, as before.
            var result = new List<int>(); int pending = 0;
            foreach (int length in merged)
            {
                if (length == 1) { pending++; if (pending == 2) { result.Add(2); pending = 0; } continue; }
                if (pending == 1) { result.Add(1); pending = 0; }
                result.Add(length);
            }
            if (pending == 1) { if (result.Count > 0 && result[result.Count - 1] <= 2) result[result.Count - 1] += 1; else result.Add(1); }
            return result;
        }
    }
}
