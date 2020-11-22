using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;

namespace WpfUi
{
  public class SaveObservableCollection<T1,T2> : ObservableCollection<T1> 
    where T2 : class 
    where T1 : class, IViewModel<T2>
  {
    public string SavePath { get; set; }
    public bool IsAutoSave { get; set; } = true;

    readonly Timer timer;
    bool IsLoaded = false;

    public SaveObservableCollection(int interval = 500)
    {
      timer = new Timer(interval);
      timer.AutoReset = false;
      timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      using (StreamWriter sw = new StreamWriter(SavePath, false)) sw.Write(JsonConvert.SerializeObject(this.Select(x => x.Data).ToList()));
    }
    public void Save()
    {
      if(IsLoaded)
      {
        timer.Stop();
        timer.Start();
      }
    }

    public void Load(Func<T2,T1> func)//Func - Action - Predicate - ....
    {
      if (func == null) throw new ArgumentNullException(nameof(func));

      IsLoaded = false;
      this.Clear();
      List<T2> t2s = new List<T2>();
      if (File.Exists(SavePath))
      {
        try
        {
          using (StreamReader sr = new StreamReader(SavePath))
          {
            List<T2> list = JsonConvert.DeserializeObject<List<T2>>(sr.ReadToEnd());
            t2s.AddRange(list);
          }
        }
        catch (Exception) { }
      }
      t2s.ForEach(x => this.Add(func.Invoke(x)));
      IsLoaded = true;
    }
    public void Load(string SavePath, Func<T2, T1> func)
    {
      this.SavePath = SavePath;
      Load(func);
    }


    #region ObservableCollection
    protected override void InsertItem(int index, T1 item)
    {
      item.Change += Item_Change;
      base.InsertItem(index, item);
    }

    protected override void ClearItems()
    {
      foreach (var item in this) item.Change -= Item_Change;
      base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
      this[index].Change -= Item_Change;
      base.RemoveItem(index);
    }

    protected override void SetItem(int index, T1 item)
    {
      this[index].Change -= Item_Change; 
      item.Change += Item_Change;
      base.SetItem(index, item);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
      if (IsAutoSave) Save();
      base.OnCollectionChanged(e);
    }
    #endregion

    private void Item_Change(object obj, T2 data)
    {
      if (IsAutoSave) Save();
    }

    protected void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      OnPropertyChanged(new PropertyChangedEventArgs(name));
    }
  }

  public delegate void ChangeCallBack<T>(object obj, T data);
  public interface IViewModel<T> where T : class
  {
    T Data { get; }
    event ChangeCallBack<T> Change;
  }
}
