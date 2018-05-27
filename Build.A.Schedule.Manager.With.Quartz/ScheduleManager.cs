#region Usings
using log4net;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
#endregion

namespace Build.A.Schedule.Manager.With.Quartz
{
    public class ScheduleManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ScheduleManager));
        private readonly object _obj = new object();
        private IScheduler _scheduler;
        private bool _isStarted = false;
        private static volatile ScheduleManager _instance;
        private static readonly object _syncRoot = new Object();
        public bool IsInStandby { get { return _isStarted && _scheduler.InStandbyMode; } }
        public bool IsStarting { get; private set; } = false;
        public bool IsStoping { get; private set; } = false;
        public static ScheduleManager Instance
        {
            get
            {
                if (_instance == null)
                    lock (_syncRoot)
                        if (_instance == null)
                            _instance = new ScheduleManager();

                return _instance;
            }
        }

        private ScheduleManager() { }

        public void Start()
        {
            lock (_obj)
                if (!_isStarted)
                {
                    IsStarting = true;
                    try
                    {
                        _log.Info("Start schedule manager...");

                        var stdSchedulerProperties = new NameValueCollection
                        {
                            { "quartz.threadPool.threadCount", "10" },
                            { "quartz.jobStore.misfireThreshold", "60000" }
                        };
                        var stdSchedulerFactory = new StdSchedulerFactory(stdSchedulerProperties);
                        _scheduler = stdSchedulerFactory.GetScheduler().Result;
                        _scheduler.Start();
                        _isStarted = true;

                        _log.Info("Schedule manager started");
                    }
                    catch (Exception e)
                    {
                        _isStarted = false;

                        _log.Error("Schedule manager start attempt failed", e);
                        throw;
                    }
                    finally
                    {
                        IsStarting = false;
                    }
                }
        }
        public void Stop()
        {
            lock (_obj)
                if (_isStarted)
                {
                    IsStoping = true;
                    try
                    {
                        _log.Info("Stop schedule manager...");

                        _scheduler.Shutdown(true /* wait until all terminate */);

                        _log.Info("Schedule manager stoped");
                    }
                    catch (Exception e)
                    {
                        _log.Info("Schedule manager stop attempt failed", e);
                        throw;
                    }
                    finally
                    {
                        _isStarted = false;
                        IsStoping = false;
                    }
                }
        }
        public void ToggleStandby()
        {
            if (!_isStarted)
                return;

            if (_scheduler.InStandbyMode)
                _scheduler.Start();
            else
                _scheduler.Standby();
        }
        public ScheduleJobResult ScheduleJob<T>(string jobId, string jobGroup, string cronExpression, Dictionary<string, string> jobData = null, string timeZone = null)
            where T : IJob
        {
            var ret = new ScheduleJobResult();

            // a. build job
            JobBuilder jobBuilder = JobBuilder.Create<T>()
                .WithIdentity(jobId, jobGroup);

            if (jobData != null)
                foreach (KeyValuePair<string, string> pair in jobData)
                    jobBuilder.UsingJobData(pair.Key, pair.Value);

            IJobDetail job = jobBuilder.Build();
            ret.Job = job;
            //

            // b. build trigger
            TimeZoneInfo timeZoneInfo = string.IsNullOrWhiteSpace(timeZone) ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            TriggerBuilder triggerBuilder = TriggerBuilder.Create()
                .WithIdentity(jobId, jobGroup)
                .WithCronSchedule(cronExpression, x => x.InTimeZone(timeZoneInfo));

            if (jobData != null)
                foreach (KeyValuePair<string, string> pair in jobData)
                    jobBuilder.UsingJobData(pair.Key, pair.Value);

            ITrigger trigger = triggerBuilder.Build();
            ret.Trigger = trigger;
            //

            // c. schedule job
            if (trigger.GetNextFiringTimes(DateTimeOffset.Now).FirstOrDefault() == default(DateTime))
            {
                ret.Success = false;

                _log.Info($"Job WILL NEVER START for \"{jobId}\"");
            }
            else
            {
                _scheduler.ScheduleJob(job, trigger).Wait();
                ret.Success = true;

                _log.Debug($"Job scheduled OK for \"{jobId}\"");
            }

            return ret;
        }
        public ScheduleJobResult ScheduleJob<T>(string jobId, string jobGroup, ITrigger trigger, Dictionary<string, string> jobData = null)
            where T : IJob
        {
            var ret = new ScheduleJobResult();

            // a. build job
            JobBuilder jobBuilder = JobBuilder.Create<T>()
                .WithIdentity(jobId, jobGroup);

            if (jobData != null)
                foreach (KeyValuePair<string, string> pair in jobData)
                    jobBuilder.UsingJobData(pair.Key, pair.Value);

            IJobDetail job = jobBuilder.Build();
            ret.Job = job;
            //

            // b. use trigger 
            ret.Trigger = trigger;
            //

            // c. schedule job
            if (trigger.GetNextFiringTimes(DateTimeOffset.Now).FirstOrDefault() == default(DateTime))
            {
                ret.Success = false;

                _log.Info($"Job WILL NEVER START for \"{jobId}\"");
            }
            else
            {
                _scheduler.ScheduleJob(job, trigger).Wait();
                ret.Success = true;

                _log.Debug($"Job scheduled OK for \"{jobId}\"");
            }

            return ret;
        }
        public void UnscheduleTask(ScheduleJobResult scheduleJobResult)
        {
            _scheduler.UnscheduleJob(scheduleJobResult.Trigger.Key).Wait();
        }
        public void RunNow<T>(string jobId, Dictionary<string, string> jobData = null)
            where T : IJob
        {
            string uid = Guid.NewGuid().ToString();

            // a. build job
            JobBuilder jobBuilder = JobBuilder.Create<T>()
                .WithIdentity($"{jobId}_{uid}", "OneTimeJobs");

            if (jobData != null)
                foreach (KeyValuePair<string, string> pair in jobData)
                    jobBuilder.UsingJobData(pair.Key, pair.Value);

            IJobDetail job = jobBuilder.Build();
            //

            // b. build trigger
            TriggerBuilder triggerBuilder = TriggerBuilder.Create()
                .WithIdentity($"{jobId}_{uid}", "OneTimeTriggers")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).WithRepeatCount(1));

            if (jobData != null)
                foreach (KeyValuePair<string, string> pair in jobData)
                    jobBuilder.UsingJobData(pair.Key, pair.Value);

            ITrigger trigger = triggerBuilder.Build();
            //

            // c. schedule job
            _scheduler.ScheduleJob(job, trigger).Wait();
        }
    }
}
