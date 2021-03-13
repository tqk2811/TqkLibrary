using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TqkLibrary.WpfUi.UserControls
{
  //https://stackoverflow.com/questions/9218258/is-there-a-timepicker-control-in-wpf-net-4
  public sealed partial class TimePicker : UserControl
  {
    public TimeSpan Value
    {
      get { return (TimeSpan)GetValue(ValueProperty); }
      set { SetValue(ValueProperty, value); }
    }
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
      "Value",
      typeof(TimeSpan),
      typeof(TimePicker),
      new FrameworkPropertyMetadata(DateTime.Now.TimeOfDay, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnValueChanged)));

    public int Hours
    {
      get { return (int)GetValue(HoursProperty); }
      set { SetValue(HoursProperty, value); }
    }
    public static readonly DependencyProperty HoursProperty = DependencyProperty.Register(
      "Hours", 
      typeof(int),
      typeof(TimePicker),
      new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTimeChanged)));

    public int Minutes
    {
      get { return (int)GetValue(MinutesProperty); }
      set { SetValue(MinutesProperty, value); }
    }
    public static readonly DependencyProperty MinutesProperty = DependencyProperty.Register(
      "Minutes", 
      typeof(int), 
      typeof(TimePicker),
      new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTimeChanged)));

    public int Seconds
    {
      get { return (int)GetValue(SecondsProperty); }
      set { SetValue(SecondsProperty, value); }
    }

    public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(
      "Seconds", 
      typeof(int), 
      typeof(TimePicker),
      new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTimeChanged)));


    public int Milliseconds
    {
      get { return (int)GetValue(MillisecondsProperty); }
      set { SetValue(MillisecondsProperty, value); }
    }

    public static readonly DependencyProperty MillisecondsProperty = DependencyProperty.Register(
      "Milliseconds", 
      typeof(int), 
      typeof(TimePicker),
      new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTimeChanged)));

    


    public TimePicker()
    {
      InitializeComponent();
    }

    private void hh_MouseWheel(object sender, MouseWheelEventArgs e)
    {

    }

    private void mm_MouseWheel(object sender, MouseWheelEventArgs e)
    {

    }

    private void ss_MouseWheel(object sender, MouseWheelEventArgs e)
    {

    }

    private void ff_MouseWheel(object sender, MouseWheelEventArgs e)
    {

    }

    private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      TimePicker control = obj as TimePicker;
      var newTime = (TimeSpan)e.NewValue;

      control.Hours = newTime.Hours;
      control.Minutes = newTime.Minutes;
      control.Seconds = newTime.Seconds;
      control.Milliseconds = newTime.Milliseconds;
    }

    private static void OnTimeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      TimePicker control = obj as TimePicker;
      control.Value = new TimeSpan(0, control.Hours, control.Minutes, control.Seconds, control.Milliseconds);
    }

   



    

    private Tuple<int, int> GetMaxAndCurentValues(String name)
    {
      int maxValue = 0;
      int currValue = 0;

      switch (name)
      {
        case "ff":
          maxValue = 1000;
          currValue = Milliseconds;
          break;

        case "ss":
          maxValue = 60;
          currValue = Seconds;
          break;

        case "mm":
          maxValue = 60;
          currValue = Minutes;
          break;

        case "hh":
          maxValue = 24;
          currValue = Hours;
          break;
      }

      return new Tuple<int, int>(maxValue, currValue);
    }

    private void UpdateTimeValue(String name, int delta)
    {
      var values = GetMaxAndCurentValues(name);
      int maxValue = values.Item1;
      int currValue = values.Item2;

      // Set new value
      int newValue = currValue + delta;

      if (newValue == maxValue)
      {
        newValue = 0;
      }
      else if (newValue < 0)
      {
        newValue += maxValue;
      }


      switch (name)
      {
        case "ff":
          Milliseconds = newValue;

          break;

        case "ss":
          Seconds = newValue;
          break;

        case "mm":
          Minutes = newValue;
          break;

        case "hh":
          Hours = newValue;
          break;
      }
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
      try
      {
        int delta = 0;
        string name = ((TextBox)sender).Name;

        if (args.Key == Key.Up) { delta = 1; }
        else if (args.Key == Key.Down) { delta = -1; }

        UpdateTimeValue(name, delta);
      }
      catch { }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
      try
      {
        TextBox textBox = sender as TextBox;
        UpdateTimeValue(textBox.Name, e.Delta / Math.Abs(e.Delta));
      }
      catch { }

    }

    private bool IsTextAllowed(string name, string text)
    {
      try
      {
        foreach (char c in text.ToCharArray())
        {
          if (char.IsDigit(c) || char.IsControl(c)) continue;
          else return false;
        }

        var values = GetMaxAndCurentValues(name);
        int maxValue = values.Item1;

        int newValue = Convert.ToInt32(text);

        if (newValue < 0 || newValue >= (int)maxValue)
        {
          return false;
        }

      }
      catch
      {
        return false;
      }


      return true;
    }

    // Use the OnPreviewTextInput to respond to key presses 
    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      try
      {
        var tb = (TextBox)sender;
        e.Handled = !IsTextAllowed(tb.Name, tb.Text + e.Text);
      }
      catch { }
    }

    // Use the DataObject.Pasting Handler  
    private void OnTextPasting(object sender, DataObjectPastingEventArgs e)
    {
      try
      {
        string name = ((TextBox)sender).Name;

        if (e.DataObject.GetDataPresent(typeof(string)))
        {
          string text = (string)e.DataObject.GetData(typeof(string));
          if (!IsTextAllowed(name, text)) e.CancelCommand();
        }
        else e.CancelCommand();
      }
      catch { }
    }
  }
}