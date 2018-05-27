#region Usings
using Quartz;
using System;
using System.Threading.Tasks;
#endregion

namespace Build.A.Schedule.Manager.With.Quartz
{
    public class TestJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => Execute());
        }

        public void Execute()
        {
            Console.WriteLine($"Test job execution at:{DateTime.Now}");
        }
    }
}
