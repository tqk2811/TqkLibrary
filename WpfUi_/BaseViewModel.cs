using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfUi
{
  public class BaseViewModel : INotifyPropertyChanged
  {
    #region INotifyPropertyChanged
    protected void NotifyPropertyChange([CallerMemberName] string name = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
  }
}
