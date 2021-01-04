using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TqkLibrary.Queues.TaskQueues
{
  public interface IQueue
  {
    bool IsPrioritize { get; }
    bool ReQueue { get; }

    /// <summary>
    /// Dont use async
    /// </summary>
    /// <returns></returns>
    Task DoWork();

    bool CheckEquals(IQueue queue);

    void Cancel();
  }

  public delegate void QueueComplete<T>(Task task, T queue) where T : IQueue;

  public delegate void RunComplete();

  public class TaskQueue<T> where T : IQueue
  {
    private readonly List<T> Queues = new List<T>();
    private readonly List<T> Runnings = new List<T>();

    [Browsable(false), DefaultValue((string)null)]
    public Dispatcher Dispatcher { get; set; }

    public event RunComplete OnRunComplete;

    public event QueueComplete<T> OnQueueComplete;

    private int _MaxRun = 0;

    public int MaxRun
    {
      get { return _MaxRun; }
      set
      {
        bool flag = value > _MaxRun;
        _MaxRun = value;
        if (flag && Queues.Count != 0) RunNewQueue();
      }
    }

    public int RunningCount
    {
      get { return Runnings.Count; }
    }

    public int QueueCount
    {
      get { return Queues.Count; }
    }

    public bool RunRandom { get; set; } = false;

    //need lock Queues first
    private void StartQueue(T queue)
    {
      if (null != queue)
      {
        Queues.Remove(queue);
        lock (Runnings) Runnings.Add(queue);
        queue.DoWork().ContinueWith(ContinueTaskResult, queue);
      }
    }

    private void RunNewQueue()
    {
      lock (Queues)//Prioritize
      {
        foreach (var q in Queues.Where(x => x.IsPrioritize)) StartQueue(q);
      }

      if (Queues.Count == 0 && Runnings.Count == 0 && OnRunComplete != null)
      {
        if (Dispatcher != null && !Dispatcher.CheckAccess()) Dispatcher.Invoke(OnRunComplete);
        else OnRunComplete.Invoke();//on completed
        return;
      }

      if (Runnings.Count >= MaxRun) return;//other
      else
      {
        lock (Queues)
        {
          T queue;
          if (RunRandom) queue = Queues.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
          else queue = Queues.FirstOrDefault();
          StartQueue(queue);
        }
        if (Queues.Count > 0 && Runnings.Count < MaxRun) RunNewQueue();
      }
    }

    private void ContinueTaskResult(Task result, object queue_obj) => QueueCompleted(result, (T)queue_obj);

    private void QueueCompleted(Task result, T queue)
    {
      lock (Runnings) Runnings.Remove(queue);
      if (queue.ReQueue) lock (Queues) Queues.Add(queue);
      if (OnQueueComplete != null)
      {
        if (Dispatcher != null && !Dispatcher.CheckAccess()) Dispatcher.Invoke(OnQueueComplete, queue);
        else OnQueueComplete.Invoke(result, queue);
      }
      RunNewQueue();
    }

    public void Add(T queue)
    {
      if (null == queue) throw new ArgumentNullException(nameof(queue));
      lock (Queues) Queues.Add(queue);
      RunNewQueue();
    }

    public void AddRange(IEnumerable<T> queues)
    {
      if (null == queues) throw new ArgumentNullException(nameof(queues));
      lock (Queues) foreach (var queue in queues) Queues.Add(queue);
      RunNewQueue();
    }

    public void Cancel(T queue)
    {
      if (null == queue) throw new ArgumentNullException(nameof(queue));
      lock (Queues) Queues.RemoveAll(o => o.CheckEquals(queue));
      lock (Runnings) Runnings.ForEach(o => { if (o.CheckEquals(queue)) o.Cancel(); });
    }

    public void Cancel(Func<T, bool> func)
    {
      if (null == func) throw new ArgumentNullException(nameof(func));
      lock (Queues)
      {
        Queues.Where(func).ToList().ForEach(x => Queues.RemoveAll(o => o.CheckEquals(x)));
      }
      lock (Runnings)
      {
        Runnings.Where(func).ToList().ForEach(x => Runnings.RemoveAll(o => o.CheckEquals(x)));
      }
    }

    public void Reset(T queue)
    {
      if (null == queue) throw new ArgumentNullException(nameof(queue));
      Cancel(queue);
      Add(queue);
    }

    public void ShutDown()
    {
      MaxRun = 0;
      lock (Queues) Queues.Clear();
      lock (Runnings) Runnings.ForEach(o => o.Cancel());
    }
  }
}