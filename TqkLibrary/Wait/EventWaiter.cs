using System;
using System.Reflection;
using System.Threading;

namespace TqkLibrary.Wait
{
  public class EventWaiter
  {
    private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
    private EventInfo _event = null;
    private object _eventContainer = null;
    private Action action = null;
    private bool Flag = false;

    public EventWaiter(object eventContainer, string eventName)
    {
      _eventContainer = eventContainer;
      _event = eventContainer.GetType().GetEvent(eventName);
      action = new Action(Fire);
    }

    public void WaitForEvent()
    {
      WaitForEvent(TimeSpan.MaxValue);
    }
    public void WaitForEvent(TimeSpan timeout)
    {
      _event.AddEventHandler(_eventContainer, action);
      Flag = true;
      _autoResetEvent.WaitOne(timeout);
    }
    public void Cancel()
    {
      if (Flag)
      {
        Flag = false;
        _event.RemoveEventHandler(_eventContainer, action);
        _autoResetEvent.Set();
      }
    }

    void Fire()
    {
      _autoResetEvent.Set();
    }
  }
}
