using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DragonMaidClock
{
    [Serializable]
    public sealed class SleepEntry
    {
        public long start;
        public long end;
        public bool onDuty;
        public bool simulated;
    }

    public sealed class SleepJournal
    {
        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public bool onDuty;
            public long started;
            // Append in completion order. A simulated night can end before a real recording.
            public List<SleepEntry> entries;
        }

        private const string FileName = "sleep-journal.json";
        private const long MaxUnixTime = 253402300799L;
        private readonly string path;
        private SaveData data;

        public bool OnDuty => data.onDuty;
        public bool IsRecording => data.started > 0;
        public long Started => data.started;
        public SleepEntry Latest => data.entries.Count == 0 ? null : data.entries[data.entries.Count - 1];

        private SleepJournal(string path)
        {
            this.path = path;
        }

        public static SleepJournal Load()
        {
            string path = Path.Combine(Application.persistentDataPath, FileName);
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-journalPath");
            if (index >= 0 && index + 1 < args.Length)
                path = Path.GetFullPath(args[index + 1]);

            var journal = new SleepJournal(path);
            foreach (string candidate in new[] { path, path + ".tmp", path + ".bak" })
            {
                if (TryRead(candidate, out journal.data))
                    return journal;
            }

            journal.Save(false, 0, SampleWeek(DateTime.Today));
            return journal;
        }

        public void SetDuty(bool onDuty)
        {
            if (!IsRecording && onDuty != OnDuty)
                Save(onDuty, 0, data.entries);
        }

        public void Start()
        {
            if (!IsRecording)
                Save(OnDuty, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), data.entries);
        }

        public SleepEntry Finish(bool simulate)
        {
            if (!IsRecording)
                return Latest;

            var now = DateTimeOffset.Now;
            long end = now.ToUnixTimeSeconds();
            long start = Math.Min(Started, end);
            if (simulate)
            {
                // The preview belongs to today's wake date and must never end in the future.
                long morning = new DateTimeOffset(now.LocalDateTime.Date.AddHours(7).AddMinutes(18)).ToUnixTimeSeconds();
                end = Math.Min(end, morning);
                start = end - (7 * 60 + 36) * 60;
            }

            var entry = new SleepEntry { start = start, end = end, onDuty = OnDuty, simulated = simulate };
            var entries = new List<SleepEntry>(data.entries) { entry };
            Save(OnDuty, 0, entries);
            return entry;
        }

        // For ties, use the most recently completed entry first. The weekly chart
        // can take the first entry per local wake date without double-counting a day.
        public List<SleepEntry> Recent()
        {
            return Enumerable.Reverse(data.entries).OrderByDescending(entry => entry.end).ToList();
        }

        private void Save(bool onDuty, long started, List<SleepEntry> entries)
        {
            var next = new SaveData { version = 1, onDuty = onDuty, started = started, entries = entries };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            string backup = path + ".bak";
            File.WriteAllText(temporary, JsonUtility.ToJson(next));
            if (File.Exists(path))
            {
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Move(path, backup);
            }
            File.Move(temporary, path);
            data = next;
        }

        private static bool TryRead(string candidate, out SaveData saved)
        {
            saved = null;
            if (!File.Exists(candidate))
                return false;

            try
            {
                saved = JsonUtility.FromJson<SaveData>(File.ReadAllText(candidate));
                return saved != null && saved.version == 1 && saved.entries != null &&
                       saved.started >= 0 && saved.started <= MaxUnixTime && saved.entries.All(entry =>
                           entry != null && entry.start > 0 && entry.end >= entry.start &&
                           entry.end <= MaxUnixTime);
            }
            catch (ArgumentException) { return false; }
            catch (IOException) { return false; }
        }

        private static List<SleepEntry> SampleWeek(DateTime today)
        {
            int[] daysAgo = { 6, 5, 3, 2, 1 };
            int[] wakeMinutes = { 445, 462, 419, 452, 430 };
            int[] sleepMinutes = { 438, 466, 401, 453, 474 };
            var entries = new List<SleepEntry>();
            for (int i = 0; i < daysAgo.Length; i++)
            {
                long wake = new DateTimeOffset(today.AddDays(-daysAgo[i]).AddMinutes(wakeMinutes[i])).ToUnixTimeSeconds();
                entries.Add(new SleepEntry
                {
                    start = wake - sleepMinutes[i] * 60,
                    end = wake,
                    onDuty = i != 2,
                    simulated = true
                });
            }
            return entries;
        }
    }
}
