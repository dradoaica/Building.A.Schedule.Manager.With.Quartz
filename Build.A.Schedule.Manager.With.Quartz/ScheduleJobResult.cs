#region Usings
using Quartz;
#endregion

namespace Build.A.Schedule.Manager.With.Quartz
{
    public class ScheduleJobResult
    {
        public IJobDetail Job { get; set; }
        public ITrigger Trigger { get; set; }
        public bool Success { get; set; }
    }
}
