using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BedrockService
{
    internal sealed class BackupEntry
    {
        public DirectoryInfo Dir { get; }
        public DateTime Time { get; }

        public BackupEntry(DirectoryInfo dir, DateTime time)
        {
            Dir = dir;
            Time = time;
        }
    }

    // Tiered backup retention, modelled on the restic "forget" / borg "prune" policy.
    //
    // Each rule selects backups to KEEP; a backup survives if any rule keeps it, and
    // everything else is returned for deletion. The periodic rules (hourly/daily/...)
    // keep the newest backup within each of the last N time periods, which naturally
    // yields dense-recent / sparse-old retention without overlapping rules conflicting.
    internal static class BackupRetention
    {
        private static readonly Regex durationPattern =
            new Regex(@"^(\d+)\s*([mhdw])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Returns the backups that should be deleted under the policy. If the policy is
        // null or has no active rule, nothing is deleted (everything is kept).
        public static List<T> SelectForDeletion<T>(
            IEnumerable<T> backups, Func<T, DateTime> timestamp, RetentionConfig policy, DateTime now)
        {
            if (policy == null)
            {
                return new List<T>();
            }

            var within = ParseDuration(policy.KeepWithin);
            bool anyRule = within.HasValue
                || policy.KeepLast > 0
                || policy.KeepHourly > 0
                || policy.KeepDaily > 0
                || policy.KeepWeekly > 0
                || policy.KeepMonthly > 0
                || policy.KeepYearly > 0;

            // No rule means "keep everything" — never interpret an empty policy as "delete all".
            if (!anyRule)
            {
                return new List<T>();
            }

            // Newest first: periodic rules walk this order keeping the first (newest)
            // backup they see in each period.
            var ordered = backups.OrderByDescending(timestamp).ToList();
            var keep = new HashSet<T>();

            if (within.HasValue)
            {
                var cutoff = now - within.Value;
                foreach (var b in ordered)
                {
                    if (timestamp(b) >= cutoff)
                    {
                        keep.Add(b);
                    }
                }
            }

            if (policy.KeepLast > 0)
            {
                foreach (var b in ordered.Take(policy.KeepLast))
                {
                    keep.Add(b);
                }
            }

            KeepPeriodic(ordered, timestamp, policy.KeepHourly, t => t.ToString("yyyyMMddHH"), keep);
            KeepPeriodic(ordered, timestamp, policy.KeepDaily, t => t.ToString("yyyyMMdd"), keep);
            KeepPeriodic(ordered, timestamp, policy.KeepWeekly, IsoWeekKey, keep);
            KeepPeriodic(ordered, timestamp, policy.KeepMonthly, t => t.ToString("yyyyMM"), keep);
            KeepPeriodic(ordered, timestamp, policy.KeepYearly, t => t.ToString("yyyy"), keep);

            return ordered.Where(b => !keep.Contains(b)).ToList();
        }

        // Keep the newest backup in each of the most recent `count` distinct periods.
        private static void KeepPeriodic<T>(
            List<T> ordered, Func<T, DateTime> timestamp, int count,
            Func<DateTime, string> periodKey, HashSet<T> keep)
        {
            if (count <= 0)
            {
                return;
            }

            var seen = new HashSet<string>();
            foreach (var b in ordered)
            {
                var key = periodKey(timestamp(b));
                if (!seen.Add(key))
                {
                    continue;
                }
                keep.Add(b);
                if (seen.Count >= count)
                {
                    break;
                }
            }
        }

        // ISO-8601 week key (e.g. "2026-26"). .NET Framework lacks ISOWeek, so use the
        // standard Calendar-based workaround.
        private static string IsoWeekKey(DateTime time)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;
            var day = cal.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            int week = cal.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return time.Year.ToString("D4") + "-" + week.ToString("D2");
        }

        // Parses a simple duration: "<n><unit>" where unit is m/h/d/w. Returns null if blank or malformed.
        public static TimeSpan? ParseDuration(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var match = durationPattern.Match(value.Trim());
            if (!match.Success)
            {
                return null;
            }

            int n = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            switch (char.ToLowerInvariant(match.Groups[2].Value[0]))
            {
                case 'm': return TimeSpan.FromMinutes(n);
                case 'h': return TimeSpan.FromHours(n);
                case 'd': return TimeSpan.FromDays(n);
                case 'w': return TimeSpan.FromDays(7 * n);
                default: return null;
            }
        }
    }
}
