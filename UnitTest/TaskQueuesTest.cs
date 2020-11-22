using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TqkLibrary.TaskQueue;

namespace UnitTest
{
  class WorkQueue : IQueue
  {
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    public bool IsPrioritize { get; set; } = false;

    public bool ReQueue { get; set; } = false;

    public void Cancel()
    {
      cancellationTokenSource.Cancel();
      cancellationTokenSource.Dispose();
      cancellationTokenSource = new CancellationTokenSource();
    }

    public bool CheckEquals(IQueue queue)
    {
      return this.Equals(queue);
    }

    public Task DoWork()
    {
      return Task.Factory.StartNew(Work, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    void Work()
    {
      Task.Delay(random.Next(100,1000)).Wait();
    }

    static Random random = new Random();
  }


  [TestClass]
  public class TaskQueuesTest
  {
    [TestMethod]
    public void Test()
    {
      TaskQueues taskQueues = new TaskQueues();
      taskQueues.MaxRun = 16;
      taskQueues.OnRunComplete += TaskQueues_OnRunComplete;
      for (int i = 0; i < 100; i++) taskQueues.Add(new WorkQueue());
      mre.WaitOne();
    }
    private static ManualResetEvent mre = new ManualResetEvent(false);
    private void TaskQueues_OnRunComplete()
    {
      mre.Set();
    }
  }
}
