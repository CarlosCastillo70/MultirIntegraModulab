using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MultirIntegraModulab.Service.Models
{
    /// <summary>
    /// Context mock per executar Jobs manualment (RunOnStartup)
    /// </summary>
    public class JobExecutionContextMock : IJobExecutionContext
    {
        private readonly JobDataMap _dataMap;

        public JobExecutionContextMock(string workflowFile, Dictionary<string, WorkflowParameter> parameters)
        {
            _dataMap = new JobDataMap();
            _dataMap.Put("workflowFile", workflowFile);

            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    if (kvp.Value != null && kvp.Value.Expression != null)
                    {
                        _dataMap.Put(kvp.Key, kvp.Value.Expression);
                    }
                }
            }
        }

        public IJobDetail JobDetail => new JobDetailMock(_dataMap);
        public IScheduler Scheduler => throw new NotImplementedException();
        public ITrigger Trigger => throw new NotImplementedException();
        public ICalendar Calendar => throw new NotImplementedException();
        public bool Recovering => false;
        public TriggerKey RecoveringTriggerKey => throw new NotImplementedException();
        public int RefireCount => 0;
        public JobDataMap MergedJobDataMap => _dataMap;
        public IJob JobInstance => throw new NotImplementedException();
        public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
        public DateTimeOffset? ScheduledFireTimeUtc => DateTimeOffset.UtcNow;
        public DateTimeOffset? PreviousFireTimeUtc => null;
        public DateTimeOffset? NextFireTimeUtc => null;
        public string FireInstanceId => Guid.NewGuid().ToString();
        public object Result { get; set; }
        public TimeSpan JobRunTime => TimeSpan.Zero;
        public CancellationToken CancellationToken => CancellationToken.None;

        public object Get(object key) => _dataMap.Get(key?.ToString());
        public void Put(object key, object objectValue) => _dataMap.Put(key?.ToString(), objectValue);
    }

    /// <summary>
    /// JobDetail mock per al context d'execució
    /// </summary>
    public class JobDetailMock : IJobDetail
    {
        private readonly JobDataMap _dataMap;

        public JobDetailMock(JobDataMap dataMap)
        {
            _dataMap = dataMap;
        }

        public JobKey Key => new JobKey("MockJob");
        public string Description => "Mock Job Detail";
        public Type JobType => typeof(object);
        public bool Durable => true;
        public bool RequestsRecovery => false;
        public JobDataMap JobDataMap => _dataMap;
        public bool PersistJobDataAfterExecution => false;
        public bool ConcurrentExecutionDisallowed => false;

        public IJobDetail Clone()
        {
            var newMap = new JobDataMap();
            foreach (var key in _dataMap.Keys)
            {
                newMap.Put(key, _dataMap.Get(key));
            }
            return new JobDetailMock(newMap);
        }

        public JobBuilder GetJobBuilder()
        {
            throw new NotImplementedException();
        }
    }
}
