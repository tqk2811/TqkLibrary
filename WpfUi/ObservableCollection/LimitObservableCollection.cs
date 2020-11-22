using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfUi.ObservableCollection
{
  public class LimitObservableCollection<T> : ObservableCollection<T>
  {
    public int Limit { get; set; } = 100;
    public bool IsInsertTop { get; set; } = false;

    protected override void InsertItem(int index, T item)
    {
      if (this.Count == Limit) base.RemoveAt(IsInsertTop ? this.Count - 1 : 0);
      base.InsertItem(IsInsertTop ? 0 : this.Count, item);
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
      throw new NotSupportedException();// base.MoveItem(oldIndex, newIndex);
    }

    protected override void SetItem(int index, T item)
    {
      throw new NotSupportedException();// base.SetItem(IsInsertTop ? 0 : this.Count, item);
    }
  }
}
