#region Usings
using Quartz;
using System;
using System.Collections.Generic;
#endregion

namespace Build.A.Schedule.Manager.With.Quartz
{
    public static class Extensions
    {
        public static IEnumerable<DateTime> GetNextFiringTimes(this ITrigger trigger, DateTimeOffset? after = null, DateTimeOffset? before = null)
        {
            var temp = trigger.Clone();

            after = after ?? DateTimeOffset.Now;
            var next = temp.GetFireTimeAfter(after);
            before = before ?? next.Value.AddYears(1);

            while (next.HasValue && next.Value < before)
            {
                var dt = next.Value.LocalDateTime;
                yield return dt;
                next = temp.GetFireTimeAfter(next.Value);
            }
        }
    }
}
