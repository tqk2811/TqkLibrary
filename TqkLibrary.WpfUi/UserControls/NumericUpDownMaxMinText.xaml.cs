using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TqkLibrary.WpfUi.UserControls
{
  public sealed partial class NumericUpDownMaxMinText : UserControl
  {
    public static readonly DependencyProperty InputWidthProperty = DependencyProperty.Register(
      nameof(InputWidth),
      typeof(int),
      typeof(NumericUpDownMaxMinText),
      new FrameworkPropertyMetadata(40, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty NumValueMinProperty = DependencyProperty.Register(
      nameof(NumValueMin),
      typeof(double?),
      typeof(NumericUpDownMaxMinText),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty NumValueMaxProperty = DependencyProperty.Register(
      nameof(NumValueMax),
      typeof(double?),
      typeof(NumericUpDownMaxMinText),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
      nameof(Max),
      typeof(double),
      typeof(NumericUpDownMaxMinText),
      new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
      nameof(Min),
      typeof(double),
      typeof(NumericUpDownMaxMinText),
      new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text),
      typeof(string),
      typeof(NumericUpDownMaxMinText),
      new FrameworkPropertyMetadata("Numeric Up Down:", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
     nameof(Step),
     typeof(double),
     typeof(NumericUpDownMaxMinText),
     new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty AllowNullProperty = DependencyProperty.Register(
      nameof(AllowNull),
      typeof(bool),
      typeof(NumericUpDownMaxMinText),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public int InputWidth
    {
      get { return (int)GetValue(InputWidthProperty) + 20; }
      set { SetValue(InputWidthProperty, value - 20); }
    }
    public string Text
    {
      get { return (string)GetValue(TextProperty); }
      set { SetValue(TextProperty, value); }
    }

    public double? NumValueMin
    {
      get { return (double?)GetValue(NumValueMinProperty); }
      set { SetValue(NumValueMinProperty, value); }
    }

    public double? NumValueMax
    {
      get { return (double?)GetValue(NumValueMaxProperty); }
      set { SetValue(NumValueMaxProperty, value); }
    }

    public double Max
    {
      get { return (double)GetValue(MaxProperty); }
      set { SetValue(MaxProperty, value); }
    }

    public double Min
    {
      get { return (double)GetValue(MinProperty); }
      set { SetValue(MinProperty, value); }
    }

    public double Step
    {
      get { return (double)GetValue(StepProperty); }
      set { SetValue(StepProperty, value); }
    }
    public bool AllowNull
    {
      get { return (bool)GetValue(AllowNullProperty); }
      set { SetValue(AllowNullProperty, value); }
    }

    public NumericUpDownMaxMinText()
    {
      InitializeComponent();
    }
  }
}