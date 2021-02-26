using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace TqkLibrary.WpfUi.Converters
{
  public class ListViewAlternationConverter : IValueConverter
  {
    //public Brush Odd { get; set; }
    //public Brush Even { get; set; }
    public ListViewAlternationConverter()
    {
      //BrushConverter brushConverter = new BrushConverter();
      //Odd = (Brush)brushConverter.ConvertFrom("#ffffff");
      //Even = (Brush)brushConverter.ConvertFrom("#ebebeb");
    }


    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int) return !(((int)value) % 2 == 1);
      return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
