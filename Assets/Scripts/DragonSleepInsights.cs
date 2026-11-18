using System;
using UnityEngine;

namespace DragonMaidClock
{
    public sealed partial class DragonSleepApp
    {
        private static readonly string[] InsightWeekdays = { "日", "一", "二", "三", "四", "五", "六" };
        private readonly SleepEntry[] insightWeek = new SleepEntry[7];
        private DateTime insightToday;
        private SleepEntry insightLastSeen;
        private int insightCount;
        private long insightAverage;
        private TimeSpan insightEarliest, insightLatest;

        private static DateTime InsightLocal(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }

        private static string InsightDuration(long seconds)
        {
            if (seconds < 60) return "不足 1 分钟";
            long minutes = seconds / 60;
            if (minutes < 60) return minutes + " 分钟";
            if (minutes % 60 == 0) return minutes / 60 + " 小时";
            return minutes / 60 + " 小时 " + minutes % 60 + " 分";
        }

        private void InsightMark(float x, float y, string text)
        {
            Box(new Rect(x, y, 72, 25), Color.Lerp(Card, Gold, 0.12f), 12);
            Label(new Rect(x, y, 72, 25), text, 12, Gold, TextAnchor.MiddleCenter);
        }

        private float ReportHeight(float w, bool mobile)
        {
            return mobile ? 610 : 388;
        }

        private void DrawReport(float w, bool mobile)
        {
            var entry = viewed ?? journal.Latest;
            if (entry == null)
            {
                Panel(new Rect(0, 0, w, 203));
                Icon(new Rect(22, 23, 26, 26), "sun", Gold);
                Label(new Rect(22, 61, w - 44, 35), "第一份早晨报告", 23, Ink, TextAnchor.MiddleLeft, true);
                Label(new Rect(22, 102, w - 44, 27), "结束一次睡眠记录，就能在这里回顾。", 15, Muted);
                if (Button(new Rect(22, 143, 164, 40), "去睡前记录", true, "moon")) Go(Page.Home);
                return;
            }

            DateTime start = InsightLocal(entry.start);
            DateTime end = InsightLocal(entry.end);
            float leftW = mobile ? w : Mathf.Round((w - 16) * 0.545f);
            float rightX = mobile ? 0 : leftW + 16;
            float rightY = mobile ? 277 : 0;
            float rightW = mobile ? w : w - rightX;
            float topH = mobile ? 145 : 170;
            float timesY = topH + 12;
            float timesH = mobile ? 108 : 134;

            Panel(new Rect(0, 0, leftW, topH));
            Icon(new Rect(20, 19, 20, 20), "clock", Gold);
            Label(new Rect(50, 15, leftW - 160, 28), "本次记录时长", 15, Muted);
            InsightMark(leftW - 91, 17, entry.simulated ? "模拟记录" : "手动计时");
            Label(new Rect(20, mobile ? 48 : 54, leftW - 40, 59),
                InsightDuration(entry.end - entry.start), mobile ? 32 : 40, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(22, topH - 41, leftW - 44, 25),
                end.ToString("yyyy 年 M 月 d 日") + "  ·  星期" + InsightWeekdays[(int)end.DayOfWeek], 14, Muted);

            Panel(new Rect(0, timesY, leftW, timesH));
            Box(new Rect(leftW / 2, timesY + 19, 1, timesH - 38), Edge);
            InsightTime(20, timesY, leftW / 2 - 40, timesH, "睡前开始", "moon", start, mobile);
            InsightTime(leftW / 2 + 20, timesY, leftW / 2 - 40, timesH, "起床结束", "sun", end, mobile);

            float artH = mobile ? 155 : 186;
            float noteH = mobile ? 260 : 316;
            Panel(new Rect(rightX, rightY, rightW, noteH));
            Art(new Rect(rightX + 1, rightY + 1, rightW - 2, artH), morningArt, 15);
            float noteY = rightY + artH + 12;
            Icon(new Rect(rightX + 19, noteY + 1, 20, 20), "cup", Gold);
            Label(new Rect(rightX + 48, noteY - 3, rightW - 66, 27),
                entry.onDuty ? "缇露露的早餐便笺" : "给自己的早餐便笺", 18, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(rightX + 20, noteY + 28, rightW - 40, 23),
                entry.onDuty ? "“早安！吐司烤好了，来厨房坐坐吧。”" : "给自己烤片吐司，再倒一杯喜欢的热饮。", 14, Ink);
            Label(new Rect(rightX + 20, noteY + 55, rightW - 40, 21),
                entry.onDuty ? "本次值夜  ·  小红 · 缇露露" : "本次值夜  ·  自己记录", 13, Muted);

            float buttonY = mobile ? rightY + noteH + 14 : timesY + timesH + 17;
            if (Button(new Rect(0, buttonY, mobile ? w : Mathf.Min(230, leftW), 45), "查看 7 天趋势", true, "chart"))
            {
                int ago = (DateTime.Today - end.Date).Days;
                selectedDay = Mathf.Clamp(6 - ago, 0, 6);
                Go(Page.Trend);
            }
        }

        private void InsightTime(float x, float y, float w, float h, string title, string icon, DateTime time, bool mobile)
        {
            Icon(new Rect(x, y + 13, 18, 18), icon, Gold);
            Label(new Rect(x + 26, y + 8, w - 19, 28), title, 14, Muted);
            Label(new Rect(x, y + (mobile ? 34 : 39), w, 42), time.ToString("HH:mm"),
                mobile ? 25 : 32, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(x, y + h - 32, w, 22), time.ToString("yyyy.MM.dd"), 13, Muted);
        }

        private void InsightReadWeek()
        {
            DateTime today = DateTime.Today;
            if (insightToday == today && ReferenceEquals(journal.Latest, insightLastSeen)) return;
            insightToday = today;
            insightLastSeen = journal.Latest;
            Array.Clear(insightWeek, 0, insightWeek.Length);
            insightCount = 0;
            insightEarliest = TimeSpan.FromDays(1);
            insightLatest = TimeSpan.Zero;
            long total = 0;
            foreach (var entry in journal.Recent())
            {
                DateTime wake = InsightLocal(entry.end);
                int index = 6 - (int)(today - wake.Date).TotalDays;
                if (index < 0) break;
                if (index > 6 || insightWeek[index] != null) continue;
                insightWeek[index] = entry;
                insightCount++;
                total += entry.end - entry.start;
                if (wake.TimeOfDay < insightEarliest) insightEarliest = wake.TimeOfDay;
                if (wake.TimeOfDay > insightLatest) insightLatest = wake.TimeOfDay;
            }
            insightAverage = insightCount > 0 ? total / insightCount : 0;
        }

        private float TrendHeight(float w, bool mobile)
        {
            return mobile ? 682 : 397;
        }

        private void DrawTrend(float w, bool mobile)
        {
            InsightReadWeek();
            float leftW = mobile ? w : Mathf.Round((w - 16) * 0.63f);
            float chartH = mobile ? 258 : 276;
            InsightChart(leftW, chartH, mobile);

            float statsY = chartH + 12;
            float statsH = mobile ? 92 : 102;
            Panel(new Rect(0, statsY, leftW, statsH));
            Box(new Rect(leftW / 2, statsY + 18, 1, statsH - 36), Edge);
            Label(new Rect(18, statsY + 12, leftW / 2 - 27, 25), "平均记录时长", 13, Muted);
            Label(new Rect(18, statsY + 42, leftW / 2 - 25, 32),
                insightCount > 0 ? InsightDuration(insightAverage) : "暂无记录",
                mobile ? 17 : 22, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(leftW / 2 + 17, statsY + 12, leftW / 2 - 27, 25), "最早 / 最晚起床", 13, Muted);
            string wakeRange = insightCount == 0 ? "暂无记录" :
                insightToday.Add(insightEarliest).ToString("HH:mm") + " / " + insightToday.Add(insightLatest).ToString("HH:mm");
            Label(new Rect(leftW / 2 + 17, statsY + 42, leftW / 2 - 25, 32), wakeRange,
                mobile ? 17 : 22, Ink, TextAnchor.MiddleLeft, true);

            float x = mobile ? 0 : leftW + 16;
            float y = mobile ? statsY + statsH + 12 : 0;
            float detailW = mobile ? w : w - x;
            InsightDay(x, y, detailW, mobile ? 291 : statsY + statsH, mobile);
        }

        private void InsightChart(float w, float h, bool mobile)
        {
            Panel(new Rect(0, 0, w, h));
            Label(new Rect(18, 11, w - 121, 31), "每晚记录时长", 19, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(w - 114, 14, 96, 25), "已记录 " + insightCount + " / 7 天", 13, Gold, TextAnchor.MiddleRight);
            Label(new Rect(18, 43, w - 36, 23), "按起床日期，每天取最后结束的一次", 13, Muted);

            float plotX = mobile ? 38 : 48;
            float plotW = w - plotX - 14;
            float plotY = 81;
            float baseY = h - 65;
            float plotH = baseY - plotY;
            double maxHours = 8;
            foreach (var entry in insightWeek)
                if (entry != null) maxHours = Math.Max(maxHours, (entry.end - entry.start) / 3600.0);
            maxHours = Math.Ceiling(maxHours / 2) * 2;

            for (int line = 0; line < 3; line++)
            {
                float at = baseY - line * plotH / 2;
                Box(new Rect(plotX, at, plotW, 1), Edge);
                Label(new Rect(2, at - 11, plotX - 8, 22), (maxHours * line / 2).ToString("0") + "h",
                    11, Muted, TextAnchor.MiddleRight);
            }

            float step = plotW / 7;
            for (int i = 0; i < 7; i++)
            {
                float column = plotX + i * step;
                var hit = new Rect(column, plotY - 7, step, h - plotY + 2);
                if (Hit(hit)) selectedDay = i;
                bool selected = selectedDay == i;
                DateTime day = insightToday.AddDays(i - 6);
                var entry = insightWeek[i];
                float barW = Mathf.Min(mobile ? 17 : 31, step * 0.48f);
                float center = column + step / 2;
                if (selected)
                    Box(new Rect(column + 2, plotY - 5, step - 4, baseY - plotY + 10), Color.Lerp(Card, Gold, 0.10f), 7);
                if (entry == null)
                {
                    Box(new Rect(center - 5, baseY - 3, 10, 2), selected ? Gold : Muted, 1);
                }
                else
                {
                    float barH = Mathf.Max(3, (float)((entry.end - entry.start) / 3600.0 / maxHours) * plotH);
                    Box(new Rect(center - barW / 2, baseY - barH, barW, barH),
                        selected ? Gold : Color.Lerp(Muted, Mint, 0.58f), 4);
                }
                Label(new Rect(column, baseY + 6, step, 20), day.ToString("MM.dd"), mobile ? 11 : 13,
                    selected ? Gold : Ink, TextAnchor.MiddleCenter, selected);
                Label(new Rect(column, baseY + 28, step, 20), i == 6 ? "今天" : "周" + InsightWeekdays[(int)day.DayOfWeek],
                    12, selected ? Gold : Muted, TextAnchor.MiddleCenter);
            }
        }

        private void InsightDay(float x, float y, float w, float h, bool mobile)
        {
            int index = Mathf.Clamp(selectedDay, 0, 6);
            DateTime day = insightToday.AddDays(index - 6);
            var entry = insightWeek[index];
            float inset = mobile ? 18 : 22;
            float innerW = w - inset * 2;
            Panel(new Rect(x, y, w, h));
            Icon(new Rect(x + inset, y + 18, 19, 19), "book", Gold);
            Label(new Rect(x + inset + 28, y + 11, innerW - 28, 32), "当日记录", 15, Muted);
            Label(new Rect(x + inset, y + 50, innerW, 42), day.ToString("M 月 d 日") + "  ·  周" +
                InsightWeekdays[(int)day.DayOfWeek], mobile ? 23 : 25, Ink, TextAnchor.MiddleLeft, true);
            Rule(x + inset, y + 99, innerW);
            if (entry == null)
            {
                Label(new Rect(x + inset, y + 120, innerW, 31), "这天还没有记录", 19, Ink, TextAnchor.MiddleLeft, true);
                Label(new Rect(x + inset, y + 156, innerW, 28), "点选图中其他日期可以回顾那一晚。", 14, Muted);
                if (Button(new Rect(x + inset, y + h - 61, innerW, 43), "去睡前记录", false, "moon")) Go(Page.Home);
                return;
            }

            Label(new Rect(x + inset, y + 111, innerW, 44), InsightDuration(entry.end - entry.start),
                mobile ? 26 : 30, Ink, TextAnchor.MiddleLeft, true);
            string clockRange = InsightLocal(entry.start).ToString("HH:mm") + "  至  " + InsightLocal(entry.end).ToString("HH:mm");
            Label(new Rect(x + inset, y + 160, innerW, 24), "记录时间   " + clockRange, 15, Muted);
            Label(new Rect(x + inset, y + 191, innerW, 24),
                (entry.onDuty ? "缇露露值夜" : "自己记录") + "  ·  " + (entry.simulated ? "模拟记录" : "手动计时"), 14, Muted);
            if (Button(new Rect(x + inset, y + h - 61, innerW, 43), "查看这晚的报告", true, "book"))
            {
                viewed = entry;
                Go(Page.Report);
            }
        }
    }
}
