using System.Collections.Concurrent;

namespace ComiServ.Background
{
    public enum TaskTypes
    {
        Scan,
        GetCover,
    }
    //task needs to use the token parameter rather than its own token, because it gets merged with the master token
    public class TaskItem(TaskTypes type, string name, Action<CancellationToken?> action, CancellationToken? token = null)
    {
        public readonly TaskTypes Type = type;
        public readonly string Name = name;
        public readonly Action<CancellationToken?> Action = action;
        public readonly CancellationToken Token = token ?? CancellationToken.None;
    }
    public interface ITaskManager : IDisposable
    {
        public void StartTask(TaskItem taskItem);
        public void ScheduleTask(TaskItem taskItem, TimeSpan interval);
        public string[] GetTasks(int limit);
        public void CancelAll();
    }
    public class TaskManager(ILogger<ITaskManager>? logger)
        : ITaskManager
    {
        private readonly ConcurrentDictionary<Task, TaskItem> ActiveTasks = [];
        private readonly CancellationTokenSource MasterToken = new();
        private readonly ILogger<ITaskManager>? _logger = logger;
        private readonly ConcurrentDictionary<System.Timers.Timer,TaskItem> Scheduled = [];
        public void StartTask(TaskItem taskItem)
        {
            _logger?.LogTrace($"Start Task: {taskItem.Name}");
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(MasterToken.Token, taskItem.Token);
            var newTask = Task.Run(() => taskItem.Action(tokenSource.Token),
                tokenSource.Token);
            if (!ActiveTasks.TryAdd(newTask, taskItem))
            {
                //TODO better exception
                throw new Exception("failed to add task");
            }
            //TODO should master token actually cancel followup?
            newTask.ContinueWith(ManageFinishedTasks, MasterToken.Token);
        }
        public void ScheduleTask(TaskItem taskItem, TimeSpan interval)
        {
            //var timer = new Timer((_) => StartTask(taskItem), null, dueTime, period ?? Timeout.InfiniteTimeSpan);
            var timer = new System.Timers.Timer(interval);
            var token = CancellationTokenSource.CreateLinkedTokenSource(MasterToken.Token, taskItem.Token);
            Scheduled.TryAdd(timer, taskItem);
            token.Token.Register(() =>
            {
                timer.Stop();
                Scheduled.TryRemove(timer, out var _);
            });
            timer.Elapsed += (_, _) => taskItem.Action(token.Token);
            timer.Start();
        }
        public string[] GetTasks(int limit)
        {
            return ActiveTasks.Select(p => p.Value.Name).Take(limit).ToArray();
        }

        public void CancelAll()
        {
            MasterToken.Cancel();
        }
        public void ManageFinishedTasks()
        {
            ManageFinishedTasks(null);
        }
        private readonly object _TaskCleanupLock = new();
        protected void ManageFinishedTasks(Task? cause = null)
        {
            //there shouldn't really be concerns with running multiple simultaneously but might as well
            lock (_TaskCleanupLock)
            {
                //cache first because we're modifying the dictionary
                foreach (var pair in ActiveTasks.ToArray())
                {
                    if (pair.Key.IsCompleted)
                    {
                        bool taskRemoved = ActiveTasks.TryRemove(pair.Key, out _);
                        if (taskRemoved)
                        {
                            _logger?.LogTrace($"Removed Task: {pair.Value.Name}");
                        }
                    }
                }
            }
        }
        public void Dispose()
        {
            MasterToken?.Dispose();
        }
    }
}
