using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;
using Cronos;

namespace Extensions.BaseClass
{
    // https://docs.microsoft.com/en-us/dotnet/core/extensions/timer-service
    public interface ITimedService<T>
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
    
    public struct SchedulingOptions
    {
        public string Schedule { get; set; } = "0 5 ? * *";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(23);
        public TimeSpan RestartDelay { get; set; } = TimeSpan.FromHours(1);
    }

    public abstract class TimerBase<T> : IHostedService, IAsyncDisposable, ITimedService<T>
    {
        protected readonly ILogger _logger;
        protected readonly IConfiguration _configuration;
        protected readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _appLifetime;
        protected readonly string _name;
        protected readonly Task _completedTask = Task.CompletedTask;
        protected readonly CancellationToken _ct;
        protected CancellationTokenSource _cts;
        protected SchedulingOptions? _scheduling;
        protected string _schedule;
        private TimeSpan _timeout;
        protected TimeSpan _restartDelay;
        protected DateTime _scheduledStart;
        private DateTime _scheduledFinish;
        protected TimeSpan _startDelay;
        protected TimeSpan _finishDelay;
        private uint _executionCount = 0;

        public TimerBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _name = typeof(T)?.Name ?? nameof(TimerBase<T>);
            //serviceProvider.SetLoggerFactory();
            _logger = LogProvider.GetLogger(_name); //LogUtil.GetLogger(_name);
            _appLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _ct = _appLifetime.ApplicationStopping;
        }

        protected abstract void GetScheduler(string schedule);
        public virtual async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_scheduling is SchedulingOptions cronScheduling)
            {
                _schedule = cronScheduling.Schedule;
                _timeout = cronScheduling.Timeout;
                _restartDelay = cronScheduling.RestartDelay;
            }

            var minRestartDelay = TimeSpan.FromSeconds(1);
            if (_restartDelay < minRestartDelay)
            {
                _logger.LogWarning("{0} restart delay ({1:c}) cannot be less than {2:c}.", _name, _timeout, minRestartDelay);
                _restartDelay = minRestartDelay;
            }

            if (_timeout < TimeSpan.Zero)
            {
                _logger.LogWarning("{0} run time ({1:c}) cannot be negative.", _name, _timeout);
                _timeout = TimeSpan.Zero;
            }

            GetScheduler(_schedule);

            bool readyToRun = await ScheduleFirstJob();

            if (!readyToRun && !_appLifetime.ApplicationStopping.IsCancellationRequested)
                _appLifetime.StopApplication();
        }

        private async Task<bool> ScheduleFirstJob()
        {
            bool readyToRun = ScheduleJob();

            var dateTimeNow = DateTime.Now;
            if (_scheduledStart > dateTimeNow)
            {
                var relativeStart = _scheduledStart.GetRelativeTime();
                if (dateTimeNow.GetRelativeTime() > relativeStart)
                {
                    _scheduledStart = relativeStart.GetAbsoluteTime();
                    _scheduledFinish = _scheduledStart.Add(_timeout);
                    _logger.LogTrace($"Updated scheduled start time ({_scheduledStart}) and finish time ({_scheduledFinish}).");
                    _startDelay = _scheduledStart.GetDelayFromNow();
                    _finishDelay = _scheduledFinish.GetDelayFromNow();
                    _logger.LogTrace("Updated start delay time ({0:c}) and finish time ({1:c}).", _startDelay, _finishDelay);
                }
            }

            if (readyToRun)
            {
                readyToRun &= CheckIsReadyToRun();
                await StartTimerAsync();
            }

            return readyToRun;
        }

        protected abstract DateTimeOffset? GetScheduledStart();
        protected virtual bool ScheduleJob()
        {
            //_logger.LogTrace("{0} is scheduling the next job.", _name);
            var scheduledStart = GetScheduledStart();
            if (scheduledStart is not null)
            {
                _scheduledStart = scheduledStart.Value.DateTime;
                _scheduledFinish = _scheduledStart.Add(_timeout);
                _startDelay = _scheduledStart.GetDelayFromNow();
                _finishDelay = _timeout;
                //_logger.LogTrace($"{_name} Cron scheduled start time = {_scheduledStart}, run time = {_cronRunTime:c}, so scheduled finish time = {_scheduledFinish}.");
            }
            else
            {
                _logger.LogWarning("{0} start and finish times have not been set, check the Cron expression.", _name);
                _startDelay = Timeout.InfiniteTimeSpan;
                _finishDelay = TimeSpan.Zero;
            }
            return CheckIsReadyToRun();
        }

        private bool CheckIsReadyToRun()
        {
            bool readyToRun = false;
            if (_ct.IsCancellationRequested)
                _logger.LogDebug("{0} has been cancelled.", _name);
            else if (_startDelay < TimeSpan.Zero)
                _logger.LogWarning("{0} start delay ({1}) cannot be negative.", _name, _startDelay);
            else if (_finishDelay < TimeSpan.Zero)
                _logger.LogWarning("{0} finish delay ({1}) cannot be negative.", _name, _finishDelay);
            else
                readyToRun = true;
            return readyToRun;
        }

        protected abstract Task StartTimerAsync();

        protected virtual async Task ScheduleNextJobAsync()
        {
            if (ScheduleJob())
                await StartTimerAsync();
        }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken);

        protected virtual async Task ExecuteJobAsync()
        {
            var done = new CancellationTokenSource(_finishDelay);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_ct, done.Token);
            var invokeCount = Interlocked.Increment(ref _executionCount);
            string startTime = DateTime.Now.ToString(TimeMetrics.DateTimeFormat);
            string finishTime = _scheduledFinish.ToString(TimeMetrics.DateTimeFormat);
            _logger.LogDebug("{0}_{1} started at {2}, scheduled to work for {3:c} until {4}.",
                _name, invokeCount, startTime, _finishDelay, finishTime);
            try
            {
                await ExecuteAsync(_cts.Token);
                _logger.LogDebug("{0}_{1} finished at {2}.",
                    _name, invokeCount, DateTime.Now.ToString(TimeMetrics.DateTimeFormat));
            }
            catch (OperationCanceledException) // includes TaskCanceledException
            {
                _logger.LogDebug("{0}_{1} cancelled at {2}.",
                    _name, invokeCount, DateTime.Now.ToString(TimeMetrics.DateTimeFormat));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{0} failed to complete, trying again in {1:c}.", _name, _restartDelay);
                startTime = DateTime.Now.Add(_restartDelay).ToString(TimeMetrics.DateTimeFormat); // fixes the culture format
                _logger.LogInformation("{0} is waiting for the restart time of {1}.", _name, startTime);
                await Task.Delay(_restartDelay, _cts.Token);
                await ExecuteJobAsync();
            }
        }

        public virtual Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("{0} is stopping.", _name);
            if (!_appLifetime.ApplicationStopping.IsCancellationRequested)
                _appLifetime.StopApplication();
            return _completedTask;
        }

        public abstract ValueTask DisposeAsync();
    }
    
    public abstract class TimedService<T> : TimerBase<T>
    {
        private Timer? _timer;
        protected CronExpression? _cronExpression;

        public TimedService(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override void GetScheduler(string schedule)
        {
            try
            {
                if (string.IsNullOrEmpty(_schedule))
                    _logger.LogWarning("CronScheduling in AppSettings.json has not been specified.");
                else if (_schedule.Split(' ').Length > 5)
                    _cronExpression = CronExpression.Parse(_schedule, CronFormat.IncludeSeconds);
                else
                    _cronExpression = CronExpression.Parse(_schedule);
            }
            catch (CronFormatException ex)
            {
                _logger.LogError(ex, "Failed to parse Cron expression: {0}.", _schedule);
            }
        }

        protected override DateTimeOffset? GetScheduledStart()
        {
            return _cronExpression?.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        }

        protected override Task StartTimerAsync()
        {
            _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            if (_startDelay > TimeSpan.Zero)
            {
                string startTime = _scheduledStart.ToString(TimeMetrics.DateTimeFormat); // fixes the culture format
                _logger.LogDebug("{0} is waiting {1:c} for the start time of {2}.", _name, _startDelay, startTime);
            }
            _timer?.Change(_startDelay, _finishDelay);
            return _completedTask;
        }

        private async void TimerCallback(object? stateInfo)
        {
            //var autoEvent = stateInfo as AutoResetEvent?;
            _timer?.Dispose();
            _timer = null;
            await ExecuteJobAsync();
            await ScheduleNextJobAsync();
        }

        public override Task StopAsync(CancellationToken cancellationToken = default)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_timer is IAsyncDisposable timer)
                await timer.DisposeAsync();
            _timer = null;
        }
    }
    
    public abstract class TimerService<T> : TimerBase<T>, IAsyncDisposable
    {
        private System.Timers.Timer? _timer;
        protected CronExpression? _cronExpression;

        public TimerService(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override void GetScheduler(string schedule)
        {
            try
            {
                if (string.IsNullOrEmpty(_schedule))
                    _logger.LogWarning("CronScheduling in AppSettings.json has not been specified.");
                else if (_schedule.Split(' ').Length > 5)
                    _cronExpression = CronExpression.Parse(_schedule, CronFormat.IncludeSeconds);
                else
                    _cronExpression = CronExpression.Parse(_schedule);
            }
            catch (CronFormatException ex)
            {
                _logger.LogError(ex, "Failed to parse Cron expression: {0}.", _schedule);
            }
        }

        protected override DateTimeOffset? GetScheduledStart()
        {
            return _cronExpression?.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        }

        protected override async Task StartTimerAsync()
        {
            _timer = new System.Timers.Timer(_finishDelay.TotalMilliseconds);
            //_timer.Elapsed += async (sender, args) => await ScheduleNextJobAsync();
            _timer.Elapsed += TimerCallback;
            if (_startDelay > TimeSpan.Zero)
            {
                string startTime = _scheduledStart.ToString(TimeMetrics.DateTimeFormat); // fixes the culture format
                _logger.LogDebug("{0} is waiting {1:c} for the start time of {2}.", _name, _startDelay, startTime);
                await Task.Delay(_startDelay, _ct);
            }
            _timer.Start();
            await ExecuteJobAsync();
        }

        private async void TimerCallback(object? sender, ElapsedEventArgs e)
        {
            _timer?.Dispose();
            _timer = null;
            await base.ScheduleNextJobAsync();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            return base.StopAsync(cancellationToken);
        }

        public override async ValueTask DisposeAsync()
        {
            //if (_timer is not null) _timer.Elapsed -= async (sender, args) => await ScheduleNext();
            if (_timer is IAsyncDisposable timer)
                await timer.DisposeAsync();
            _timer = null;
        }
    }
}
