using System;
using System.IO;
using UnityEngine;

namespace DragonMaidClock
{
    public sealed partial class DragonSleepApp : MonoBehaviour
    {
        private enum Page { Home, Duty, Report, Trend }
        private static readonly string[] Titles = { "睡前记录", "值夜选择", "早晨报告", "7 天趋势" };
        private static readonly string[] NavIcons = { "moon", "cup", "sun", "chart" };
        private static readonly string[] Descriptions =
        {
            "睡前开始记录，醒来后回来收好这一晚。", "为今晚选一位守灯的人。", "翻开这一晚的记录与早餐便笺。", "点选日期，回看近一周的作息。"
        };
        private static readonly string[] IconNames =
        {
            "moon","cup","sun","chart","clock","arrow","leaf","check","book","star","play","stop","drop","chevron","calendar","person"
        };
        private static Color Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);
        private static readonly Color Deep = Rgb(20, 25, 43), Card = Rgb(33, 41, 64),
            Edge = Rgb(56, 65, 89), Ink = Rgb(249, 245, 232), Muted = Rgb(183, 192, 209),
            Gold = Rgb(244, 206, 143), Mint = Rgb(153, 212, 196), Dim = Rgb(40, 49, 73);

        private SleepJournal journal;
        private SleepEntry viewed;
        private Texture2D nightArt, morningArt, icons;
        private Font sans, numbers;
        private GUIStyle label;
        private Page page;
        private bool draftDuty;
        private int selectedDay = 6;
        private Vector2 scroll;
        private string storageError;
        private bool touchInBody;
        private float touchStart, touchScroll;

        private void Awake()
        {
            useGUILayout = false;
            Application.targetFrameRate = 30;
            nightArt = Resources.Load<Texture2D>("Art/NightWatch");
            morningArt = Resources.Load<Texture2D>("Art/Morning");
            icons = Resources.Load<Texture2D>("Art/JournalIcons");
            sans = Resources.Load<Font>("Fonts/JournalSans");
            if (sans == null)
                sans = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "Arial" }, 22);
            numbers = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Noto Serif", "DejaVu Serif", "serif" }, 80);
            LoadJournal();
        }

        private void LoadJournal()
        {
            SaveAction(() =>
            {
                journal = SleepJournal.Load();
                draftDuty = journal.OnDuty;
            });
        }

        private void SaveAction(Action action)
        {
            try { action(); storageError = null; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Could not save or open sleep journal: " + e.Message);
                storageError = "无法读写本地记录，请检查存储空间后重试。";
            }
        }

        private void Go(Page destination)
        {
            page = destination;
            scroll = Vector2.zero;
            touchInBody = false;
            GUI.FocusControl(null);
            if (page == Page.Duty) draftDuty = journal.OnDuty;
        }

        private void OnGUI()
        {
            if (label == null)
                label = new GUIStyle { font = sans, clipping = TextClipping.Clip, richText = false };

            Rect safe = Screen.safeArea;
            if (safe.width < 1 || safe.height < 1) safe = new Rect(0, 0, Screen.width, Screen.height);
            bool mobile = safe.width / safe.height < 1.12f;
            float scale = mobile ? Mathf.Min(safe.width / 390f, safe.height / 560f)
                                 : Mathf.Min(safe.width / 1100f, safe.height / 730f);
            if (scale <= 0) return;
            Box(new Rect(0, 0, Screen.width, Screen.height), Deep);
            var original = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0),
                                       Quaternion.identity, new Vector3(scale, scale, 1));
            float canvasW = safe.width / scale, h = safe.height / scale;
            float w = mobile ? Mathf.Min(canvasW - 32, 440) : 1008;
            float x = (canvasW - w) / 2;
            float headerH = mobile ? 66 : 85;
            DrawHeader(x, w, headerH, mobile);
            if (journal == null)
            {
                Label(new Rect(x, headerH + 20, w, 45), storageError, 15, Gold);
                if (Button(new Rect(x, headerH + 74, 170, 43), "重新打开记录", true)) LoadJournal();
                GUI.matrix = original;
                return;
            }

            float titleY = headerH + (mobile ? 10 : 16);
            Label(new Rect(x, titleY, w, mobile ? 42 : 54), Titles[(int)page], mobile ? 29 : 37,
                Ink, TextAnchor.MiddleLeft, true);
            string description = page == Page.Home && journal.IsRecording ? "记录已保存在本机，醒来后点结束即可。" : Descriptions[(int)page];
            Label(new Rect(x + 1, titleY + (mobile ? 43 : 53), w - 2, 25), description, mobile ? 13 : 15, Muted);
            float top = titleY + (mobile ? 83 : 96);
            float navY = h - (mobile ? 66 : 82);
            float bottom = navY - (storageError == null ? 12 : 36);
            float height = page == Page.Home ? (mobile ? 598 : 434) :
                page == Page.Duty ? (mobile ? 645 : 400) :
                page == Page.Report ? ReportHeight(w, mobile) : TrendHeight(w, mobile);
            Rect viewport = new Rect(x, top, w, Mathf.Max(10, bottom - top));
            TouchScroll(viewport, scale, safe, Mathf.Max(0, height - viewport.height));
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, w, height),
                false, false, GUIStyle.none, GUIStyle.none);
            switch (page)
            {
                case Page.Home: DrawHome(w, mobile); break;
                case Page.Duty: DrawDuty(w, mobile); break;
                case Page.Report: DrawReport(w, mobile); break;
                case Page.Trend: DrawTrend(w, mobile); break;
            }
            GUI.EndScrollView();
            if (storageError != null)
                Label(new Rect(x, navY - 34, w, 29), storageError, 13, Gold);
            DrawNavigation(x, navY, w, mobile);
            GUI.matrix = original;
        }

        private void TouchScroll(Rect viewport, float scale, Rect safe, float maximum)
        {
            if (Input.touchCount == 0 || Event.current.type != EventType.Repaint || maximum <= 0) return;
            Touch touch = Input.GetTouch(0);
            float y = (safe.yMax - touch.position.y) / scale;
            if (!touchInBody && touch.phase == TouchPhase.Began)
            {
                touchInBody = viewport.Contains(new Vector2((touch.position.x - safe.x) / scale, y));
                touchStart = y;
                touchScroll = scroll.y;
            }
            if (touchInBody && touch.phase == TouchPhase.Moved && Mathf.Abs(y - touchStart) > 5)
            {
                scroll.y = Mathf.Clamp(touchScroll + touchStart - y, 0, maximum);
                GUIUtility.hotControl = 0;
            }
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) touchInBody = false;
        }

        private void DrawHeader(float x, float w, float h, bool mobile)
        {
            float y = mobile ? 11 : 20;
            float mark = mobile ? 40 : 46;
            Box(new Rect(x, y, mark, mark), Gold, mark * 0.5f);
            Icon(new Rect(x + 6, y + 6, mark - 12, mark - 12), "moon", Deep);
            Label(new Rect(x + mark + 12, y - 2, w - mark - 115, 30), "龙女仆睡眠钟", mobile ? 19 : 22, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(x + mark + 13, y + 24, 195, 17), "DRAGON MAID SLEEP CLOCK", 10, Gold);
            DateTime now = DateTime.Now;
            string date = now.ToString(mobile ? "MM / dd" : "yyyy  /  MM  /  dd");
            Label(new Rect(x + w - 156, y + 1, 156, 26), date, mobile ? 15 : 18, Ink, TextAnchor.MiddleRight);
            Label(new Rect(x + w - 110, y + 26, 110, 18), "星期" + InsightWeekdays[(int)now.DayOfWeek], 12, Muted, TextAnchor.MiddleRight);
            Rule(x, h - 1, w);
        }

        private void DrawNavigation(float x, float y, float w, bool mobile)
        {
            Rule(x, y, w);
            float navW = mobile ? w : 690;
            float left = x + (w - navW) / 2;
            float step = navW / 4;
            for (int i = 0; i < 4; i++)
            {
                bool active = i == (int)page;
                float sx = left + i * step;
                Rect hit = new Rect(sx + 2, y + 6, step - 4, mobile ? 54 : 60);
                if (active)
                {
                    Box(new Rect(sx + (mobile ? 5 : 10), y + 8, step - (mobile ? 10 : 20), mobile ? 50 : 49), Dim, 12);
                    Box(new Rect(sx + step / 2 - 14, y, 28, 2), Gold, 1);
                }
                Color c = active ? Gold : Muted;
                float iw = mobile ? 20 : 22;
                Icon(new Rect(mobile ? sx + (step - iw) / 2 : sx + 35, y + (mobile ? 12 : 21), iw, iw), NavIcons[i], c);
                Label(new Rect(mobile ? sx : sx + 61, y + (mobile ? 33 : 15), mobile ? step : step - 64, mobile ? 20 : 33),
                    Titles[i], mobile ? 12 : 16, c, mobile ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, active);
                if (Hit(hit) && (!active || i == (int)Page.Report))
                {
                    if (i == (int)Page.Report) viewed = journal.Latest;
                    if (i == (int)Page.Trend) selectedDay = 6;
                    Go((Page)i);
                }
            }
        }

        private void DrawHome(float w, bool mobile)
        {
            float leftW = mobile ? w : 496;
            float heroH = mobile ? 229 : 328;
            DrawTimer(leftW, heroH, mobile);
            float x = mobile ? 0 : leftW + 20, y = mobile ? heroH + 12 : 0;
            float artW = mobile ? w : w - x, artH = mobile ? 241 : heroH;
            Panel(new Rect(x, y, artW, artH));
            float imageH = artH - (mobile ? 66 : 83);
            Art(new Rect(x + 1, y + 1, artW - 2, imageH), nightArt);
            Badge(x + 14, y + 13, journal.OnDuty ? "今夜值夜" : "可选值夜", true);
            Label(new Rect(x + 18, y + imageH + 4, artW - 95, mobile ? 29 : 38),
                "小红 · 缇露露", mobile ? 18 : 22, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(x + 19, y + imageH + (mobile ? 31 : 43), artW - 80, 25), "厨房担当  /  睡前备水与早安早餐", mobile ? 12 : 14, Muted);
            Box(new Rect(x + artW - 53, y + imageH + (mobile ? 14 : 23), 34, 34), Dim, 17);
            Icon(new Rect(x + artW - 45, y + imageH + (mobile ? 22 : 31), 18, 18), "arrow", Gold);
            if (Hit(new Rect(x, y, artW, artH))) Go(Page.Duty);
            DrawRecent(w, y + artH + (mobile ? 12 : 18), mobile);
        }

        private void DrawTimer(float w, float h, bool mobile)
        {
            bool active = journal.IsRecording;
            float pad = mobile ? 18 : 26;
            Panel(new Rect(0, 0, w, h));
            Icon(new Rect(pad, mobile ? 17 : 29, 21, 21), active ? "moon" : "clock", Gold);
            Label(new Rect(pad + 30, mobile ? 12 : 22, 149, 32), active ? "本次已记录" : "本地时间", 15, Muted);
            Badge(w - pad - 78, mobile ? 14 : 25, active ? "记录中" : "未开始", false, active);
            long elapsed = Math.Max(0L, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - journal.Started);
            string clockText = active ? TimeSpan.FromSeconds(elapsed).ToString(elapsed >= 86400 ? @"d\.hh\:mm\:ss" : @"hh\:mm\:ss")
                                      : DateTime.Now.ToString("HH:mm");
            float fontSize = active ? (mobile ? 40 : 55) : (mobile ? 55 : 79);
            Rect clockRect = new Rect(pad - 1, mobile ? 43 : 60, w - pad * 2, mobile ? 62 : 98);
            Label(clockRect, clockText, (int)fontSize, Ink, TextAnchor.MiddleLeft, false, numbers);
            if (!active)
            {
                float starX = w - pad - (mobile ? 56 : 80), starY = mobile ? 62 : 91;
                Icon(new Rect(starX, starY, mobile ? 24 : 38, mobile ? 24 : 38), "star", Gold);
                Icon(new Rect(starX + 28, starY - 5, 13, 13), "star", Muted);
                Icon(new Rect(starX + 28, starY + 30, 8, 8), "star", Gold);
            }
            string note = active ? "开始于 " + InsightLocal(journal.Started).ToString("MM.dd  HH:mm") :
                journal.OnDuty ? "缇露露：“水杯放好啦，今晚安心休息吧。”" : "今晚自己记录，也可以请缇露露值夜。";
            Label(new Rect(pad, mobile ? 102 : 161, w - 2 * pad, 28), note, mobile ? 13 : 15, Muted);
            float buttonY = mobile ? 140 : 214;
            if (Button(new Rect(pad, buttonY, w - 2 * pad, mobile ? 41 : 48), active ? "结束记录，查看报告" : "开始睡眠记录", true, active ? "stop" : "moon"))
            {
                if (active) FinishNight(false); else SaveAction(journal.Start);
            }
            Rect more = new Rect(pad, buttonY + (mobile ? 45 : 55), w - 2 * pad, mobile ? 35 : 40);
            string link = active ? "以模拟一夜结束本次记录" : journal.OnDuty ? "查看今晚的值夜安排" : "选择今晚的值夜人";
            Label(more, link + "  ›", 14, Gold, TextAnchor.MiddleCenter);
            if (Hit(more))
            {
                if (active) FinishNight(true); else Go(Page.Duty);
            }
        }

        private void FinishNight(bool simulate)
        {
            SaveAction(() =>
            {
                viewed = journal.Finish(simulate);
                selectedDay = 6;
                Go(Page.Report);
            });
        }

        private void DrawRecent(float w, float y, bool mobile)
        {
            float h = mobile ? 82 : 84;
            Panel(new Rect(0, y, w, h));
            float inset = mobile ? 14 : 22;
            Box(new Rect(inset, y + 20, 43, 43), Dim, 13);
            Icon(new Rect(inset + 10, y + 30, 23, 23), "book", Gold);
            float tx = inset + 56;
            Label(new Rect(tx, y + 10, w - tx - 32, 28), "最近一晚", 17, Ink, TextAnchor.MiddleLeft, true);
            var last = journal.Latest;
            string details = last == null ? "完成一晚记录后即可回顾" :
                InsightLocal(last.end).ToString("MM.dd") + "  ·  " + InsightDuration(last.end - last.start)
                + (last.simulated ? "  ·  模拟" : "  ·  手动");
            Label(new Rect(tx, y + 39, w - tx - 25, 25), details, mobile ? 12 : 14, Muted);
            if (!mobile) Label(new Rect(w - 175, y + 22, 120, 39), "查看早晨报告", 15, Gold, TextAnchor.MiddleRight);
            Icon(new Rect(w - 41, y + 32, 19, 19), "chevron", Gold);
            if (Hit(new Rect(0, y, w, h))) { viewed = last; Go(Page.Report); }
        }

        private void DrawDuty(float w, bool mobile)
        {
            float artW = mobile ? w : 455, artH = mobile ? 239 : 388;
            Panel(new Rect(0, 0, artW, artH));
            float photoH = mobile ? 172 : 278;
            Art(new Rect(1, 1, artW - 2, photoH), nightArt);
            Label(new Rect(19, photoH + 4, artW - 38, mobile ? 31 : 42), "小红 · 缇露露", mobile ? 18 : 24, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(19, photoH + (mobile ? 33 : 49), artW - 38, 27), "“给你留了一杯水，晚安。”", mobile ? 14 : 16, Gold);
            float x = mobile ? 0 : artW + 18, y = mobile ? artH + 12 : 0;
            float pw = mobile ? w : w - x, ph = mobile ? 354 : artH;
            Panel(new Rect(x, y, pw, ph));
            float pad = mobile ? 16 : 25;
            Label(new Rect(x + pad, y + 13, pw - 2 * pad, 38), "今晚的值夜安排", 21, Ink, TextAnchor.MiddleLeft, true);
            bool locked = journal.IsRecording;
            Label(new Rect(x + pad, y + 51, pw - 2 * pad, 25), locked ? "记录进行中，本晚沿用开始时的安排。" : "选好后，回到睡前页开始记录。", 14, Muted);
            float rowH = mobile ? 71 : 78, y1 = y + (mobile ? 88 : 99);
            DutyChoice(new Rect(x + pad, y1, pw - pad * 2, rowH), true, locked);
            DutyChoice(new Rect(x + pad, y1 + rowH + 10, pw - pad * 2, rowH), false, locked);
            if (Button(new Rect(x + pad, y + ph - (mobile ? 64 : 72), pw - 2 * pad, 46), locked ? "返回睡前记录" : "确认并返回睡前", true, locked ? "moon" : "check"))
                SaveAction(() => { if (!locked) journal.SetDuty(draftDuty); Go(Page.Home); });
        }

        private void DutyChoice(Rect r, bool duty, bool locked)
        {
            bool selected = draftDuty == duty;
            Box(r, selected ? Gold : Edge, 12);
            Box(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), selected ? Rgb(54, 53, 64) : Dim, 11);
            Icon(new Rect(r.x + 15, r.center.y - 11, 22, 22), duty ? "cup" : "moon", selected ? Gold : Muted);
            Label(new Rect(r.x + 49, r.y + 8, r.width - 83, 27), duty ? "请缇露露值夜" : "自己记录", 17, Ink, TextAnchor.MiddleLeft, true);
            Label(new Rect(r.x + 49, r.y + 37, r.width - 73, 24), duty ? "睡前备水，早晨收下早餐便笺。" : "安静计时，醒来查看自己的记录。", 12, Muted);
            Box(new Rect(r.xMax - 34, r.center.y - 9, 18, 18), selected ? Gold : Edge, 9);
            if (selected) Icon(new Rect(r.xMax - 32, r.center.y - 7, 14, 14), "check", Deep);
            else Box(new Rect(r.xMax - 32, r.center.y - 7, 14, 14), Dim, 7);
            if (!locked && Hit(r)) draftDuty = duty;
        }

        private void Badge(float x, float y, string text, bool image, bool active = false)
        {
            Box(new Rect(x, y, 78, 25), image ? new Color(0.08f,0.10f,0.17f,0.89f) : Dim, 12);
            Color c = image ? Gold : active ? Mint : Muted;
            Box(new Rect(x + 10, y + 10, 5, 5), c, 2.5f);
            Label(new Rect(x + 18, y, 56, 25), text, 12, c, TextAnchor.MiddleCenter);
        }

        private void Label(Rect r, string text, int size, Color c, TextAnchor align = TextAnchor.MiddleLeft,
                           bool bold = false, Font font = null)
        {
            label.font = font != null ? font : sans;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = align;
            label.normal.textColor = c;
            GUI.Label(r, text ?? "", label);
        }

        private void Box(Rect r, Color color, float radius = 0)
        {
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, radius);
        }

        private void Rule(float x, float y, float width) => Box(new Rect(x, y, width, 1), Edge);

        private void Panel(Rect r)
        {
            Box(r, Edge, 16);
            Box(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), Card, 15);
        }

        private void Art(Rect r, Texture2D texture, float radius = 15)
        {
            if (texture != null)
                GUI.DrawTexture(r, texture, ScaleMode.ScaleAndCrop, true, 0, Color.white, 0, radius);
        }

        private void Icon(Rect r, string name, Color c)
        {
            int i = Array.IndexOf(IconNames, name);
            if (icons == null || i < 0) return;
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTextureWithTexCoords(r, icons, new Rect(i / (float)IconNames.Length, 0, 1f / IconNames.Length, 1));
            GUI.color = old;
        }

        private bool Hit(Rect r) => GUI.Button(r, GUIContent.none, GUIStyle.none);

        private bool Button(Rect r, string text, bool primary = false, string icon = null)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Color bg = primary ? Gold : Dim;
            if (hover) bg = Color.Lerp(bg, Ink, 0.10f);
            Box(r, bg, 10);
            Color fg = primary ? Deep : Ink;
            if (icon != null)
            {
                // Centre the icon and caption as one group.
                label.font = sans;
                label.fontSize = 16;
                label.fontStyle = FontStyle.Bold;
                float tw = label.CalcSize(new GUIContent(text)).x;
                float left = r.center.x - (tw + 28) / 2;
                Icon(new Rect(left, r.center.y - 10, 20, 20), icon, fg);
                Label(new Rect(left + 28, r.y, tw + 2, r.height), text, 16, fg, TextAnchor.MiddleLeft, true);
            }
            else Label(r, text, 16, fg, TextAnchor.MiddleCenter, true);
            return Hit(r);
        }
    }
}
