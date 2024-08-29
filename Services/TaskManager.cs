using NuGet.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ComiServ.Services;

public enum TaskTypes
{
    Scan,
    GetCover,
    MakeThumbnail,
}
public abstract class BaseTaskItem
{
    public readonly TaskTypes Type;
    public readonly string Name;
    public readonly CancellationToken Token;
    protected BaseTaskItem(TaskTypes type, string name, CancellationToken? token = null)
    {
        Type = type;
        Name = name;
        Token = token ?? CancellationToken.None;
    }
}
//task needs to use the token parameter rather than its own token, because it gets merged with the master token
public class SyncTaskItem
    : BaseTaskItem
{
    public readonly Action<CancellationToken?> Action;
    public SyncTaskItem(TaskTypes type, string name, Action<CancellationToken?> action, CancellationToken? token = null)
        : base(type, name, token)
    {
        Action = action;
    }
}
public class AsyncTaskItem
    : BaseTaskItem
{
    public readonly Func<CancellationToken?, Task?> AsyncAction;
    public AsyncTaskItem(TaskTypes type, string name, Func<CancellationToken?, Task?> asyncAction, CancellationToken? token = null)
        : base(type, name, token)
    {
        AsyncAction = asyncAction;
    }
}
public interface ITaskManager : IDisposable
{
    public void StartTask(SyncTaskItem taskItem);
    public void StartTask(AsyncTaskItem taskItem);
    public void ScheduleTask(BaseTaskItem taskItem, TimeSpan interval);
    public string[] GetTasks(int limit);
    public void CancelAll();
}
public class TaskManager(ILogger<ITaskManager>? logger)
    : ITaskManager
{
    private readonly ConcurrentDictionary<Task, BaseTaskItem> ActiveTasks = [];
    private CancellationTokenSource MasterToken { get; set; } = new();
    private readonly ILogger<ITaskManager>? _logger = logger;
    private readonly ConcurrentDictionary<System.Timers.Timer, BaseTaskItem> Scheduled = [];
    public void StartTask(SyncTaskItem taskItem)
    {
        //_logger?.LogTrace($"Start Task: {taskItem.Name}");
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
    public void StartTask(AsyncTaskItem taskItem)
    {
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(MasterToken.Token, taskItem.Token);
        var newTask = Task.Run(() => taskItem.AsyncAction(tokenSource.Token),
            tokenSource.Token);
        if (!ActiveTasks.TryAdd(newTask, taskItem))
        {
            //TODO better exception
            throw new Exception("failed to add task");
        }
        //TODO should master token actually cancel followup?
        newTask.ContinueWith(ManageFinishedTasks, MasterToken.Token);
    }
    public void ScheduleTask(SyncTaskItem taskItem, TimeSpan interval)
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
    public void ScheduleTask(BaseTaskItem taskItem, TimeSpan interval)
    {
        var timer = new System.Timers.Timer(interval);
        var token = CancellationTokenSource.CreateLinkedTokenSource(MasterToken.Token, taskItem.Token);
        Scheduled.TryAdd(timer, taskItem);
        token.Token.Register(() =>
        {
            timer.Stop();
            Scheduled.TryRemove(timer, out var _);
        });
        if (taskItem is AsyncTaskItem ati)
            timer.Elapsed += async (_, _) =>
            {
                var task = ati.AsyncAction(token.Token);
                if (task != null)
                    await task;
            };
        else if (taskItem is SyncTaskItem sti)
            timer.Elapsed += (_, _) => sti.Action(token.Token);
        timer.Start();
    }
    public string[] GetTasks(int limit)
    {
        return ActiveTasks.Select(p => p.Value.Name).Take(limit).ToArray();
    }

    public void CancelAll()
    {
        MasterToken.Cancel();
        MasterToken.Dispose();
        MasterToken = new CancellationTokenSource();
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
                        _logger?.LogTrace("Removed Task: {TaskName}", pair.Value.Name);
                    }
                }
            }
        }
    }
    public void Dispose()
    {
        MasterToken?.Dispose();
        GC.SuppressFinalize(this);
    }
}
