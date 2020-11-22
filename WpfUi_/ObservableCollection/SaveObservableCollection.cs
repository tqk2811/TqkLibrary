using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Timers;

namespace WpfUi
{
  public class SaveObservableCollection<T1,T2> : ObservableCollection<T1> where T1 : IViewModel<T2> where T2: class
  {
    readonly Timer timer = new Timer(1000);
    public string SavePath { get; set; }
    public bool IsLoaded { get; set; } = false;
    public SaveObservableCollection()
    {
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
    public List<T2> Load()
    {
      IsLoaded = false;
      List<T2> t2s = new List<T2>();
      if (File.Exists(SavePath)) using (StreamReader sr = new StreamReader(SavePath))
      {
          List<T2> list = JsonConvert.DeserializeObject<List<T2>>(sr.ReadToEnd());
          t2s.AddRange(list);
      }
      return t2s;
    }
  }

  public interface IViewModel<T> where T : class
  {
    T Data { get; }
  }
}
