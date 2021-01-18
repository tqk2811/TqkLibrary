using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TqkLibrary.WpfUi.UserControls
{
  public delegate void PressEnter();

  public sealed partial class NumericUpDown : UserControl
  {
    public static readonly DependencyProperty NumValueProperty = DependencyProperty.Register(
      nameof(NumValue),
      typeof(double?),
      typeof(NumericUpDown),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
      nameof(Max),
      typeof(double),
      typeof(NumericUpDown),
      new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
      nameof(Min),
      typeof(double),
      typeof(NumericUpDown),
      new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
      nameof(Step),
      typeof(double),
      typeof(NumericUpDown),
      new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty AllowNullProperty = DependencyProperty.Register(
      nameof(AllowNull),
      typeof(bool),
      typeof(NumericUpDown),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private System.Timers.Timer timer;
    private System.Timers.Timer timer2;

    public event PressEnter PressEnter;

    public NumericUpDown()
    {
      InitializeComponent();
    }

    private void root_Loaded(object sender, RoutedEventArgs e)
    {
      timer = new System.Timers.Timer(1000) { AutoReset = false };
      timer.Elapsed += Timer_Elapsed;

      timer2 = new System.Timers.Timer(100) { AutoReset = false };
      timer2.Elapsed += Timer2_Elapsed;
    }

    private void root_Unloaded(object sender, RoutedEventArgs e)
    {
      timer.Dispose();
      timer = null;
      timer2.Dispose();
      timer2 = null;
    }

    public double? NumValue
    {
      get { return (double?)GetValue(NumValueProperty); }
      set
      {
        double? temp = value;
        if (value > Max) temp = Max;
        else if (value < Min) temp = Min;
        SetValue(NumValueProperty, temp);
      }
    }

    public double Max
    {
      get { return (double)GetValue(MaxProperty); }
      set
      {
        SetValue(MaxProperty, value);
        if (NumValue > value) NumValue = value;
      }
    }

    public double Min
    {
      get { return (double)GetValue(MinProperty); }
      set
      {
        SetValue(MinProperty, value);
        if (NumValue < value) NumValue = value;
      }
    }

    public double Step
    {
      get
      {
        double step = (double)GetValue(StepProperty);
        if (step == 0) return 1;
        else return step;
      }
      set
      {
        double num = value;
        if (num == 0) num = 1;
        SetValue(StepProperty, num);
      }
    }

    public bool AllowNull
    {
      get { return (bool)GetValue(AllowNullProperty); }
      set { SetValue(AllowNullProperty, value); }
    }

    private void StepNum()
    {
      if (flag_up) NumValue += Step;
      else NumValue -= Step;
    }

    private bool flag_up { get; set; } = false;

    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      timer2.Start();
    }

    private void Timer2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      Dispatcher.Invoke(() =>
      {
        StepNum();
        timer2.Start();
      });
    }

    private void TxtNum_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (double.TryParse(txtNum.Text, out double num))
      {
        if (num != NumValue)
        {
          bool flag = false;
          if (num < Min)
          {
            num = Min;
            flag = true;
          }
          else if (num > Max)
          {
            num = Max;
            flag = true;
          }
          if (flag) txtNum.Text = num.ToString(CultureInfo.InvariantCulture);
          else NumValue = num;
        }
      }
      else if (string.IsNullOrEmpty(txtNum.Text) && AllowNull)
      {
        txtNum.Text = string.Empty;
      }
      else txtNum.Text = NumValue.Value.ToString(CultureInfo.InvariantCulture);
    }

    private void EventMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      flag_up = sender.Equals(up);
      StepNum();

      (sender as Grid).Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xC1, 0xB0, 0xE0));//FFC1B0E0
      timer.Start();
    }

    private void EventMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      (sender as Grid).Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x67, 0x3A, 0xB7));//FF673AB7
      timer.Stop();
      timer2.Stop();
    }

    private void EventMouseLeave(object sender, MouseEventArgs e)
    {
      timer.Stop();
      timer2.Stop();
    }

    private void txtNum_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      timer.Stop();
      timer2.Stop();
    }

    private void txtNum_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Up || e.Key == Key.Down)
      {
        flag_up = e.Key == Key.Up;
        StepNum();
        timer.Start();
      }
      else if (e.Key == Key.Enter)
      {
        PressEnter?.Invoke();
      }
    }

    private void txtNum_PreviewKeyUp(object sender, KeyEventArgs e)
    {
      timer.Stop();
      timer2.Stop();
    }

    public void Dispose()
    {
      timer.Dispose();
      timer2.Dispose();
    }

    private void root_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      Keyboard.Focus(txtNum);
    }
  }
}