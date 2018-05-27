#region Usings
using log4net.Config;
using System;
using System.Threading.Tasks;
#endregion

namespace Build.A.Schedule.Manager.With.Quartz
{
    class Program
    {
        static void Main(string[] args)
        {
            // enable log4net
            XmlConfigurator.Configure();

            ScheduleManager.Instance.Start();

            // schedule an every 15 seconds test job
            ScheduleManager.Instance.ScheduleJob<TestJob>("testJob", "testGroup", "0/15 * * * * ?");

            // some sleep to show what's happening
            Task.Delay(TimeSpan.FromSeconds(60)).Wait();

            ScheduleManager.Instance.Stop();
        }
    }
}
