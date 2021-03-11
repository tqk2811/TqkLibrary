using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TqkLibrary.WpfUi.UserControls
{
  public sealed partial class NumericUpDownText : UserControl
  {
    public static readonly DependencyProperty InputWidthProperty = DependencyProperty.Register(
      nameof(InputWidth),
      typeof(int),
      typeof(NumericUpDownText),
      new FrameworkPropertyMetadata(45, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text),
      typeof(string),
      typeof(NumericUpDownText),
      new FrameworkPropertyMetadata("Numeric Up Down:", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Text
    {
      get { return (string)GetValue(TextProperty); }
      set { SetValue(TextProperty, value); }
    }

    public int InputWidth
    {
      get { return (int)GetValue(InputWidthProperty) + 20; }
      set { SetValue(InputWidthProperty, value - 20); }
    }

    #region NUD
    public static readonly DependencyProperty NumValueProperty = DependencyProperty.Register(
      nameof(NumValue),
      typeof(double?),
      typeof(NumericUpDownText),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
      nameof(Max),
      typeof(double),
      typeof(NumericUpDownText),
      new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
      nameof(Min),
      typeof(double),
      typeof(NumericUpDownText),
      new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
      nameof(Step),
      typeof(double),
      typeof(NumericUpDownText),
      new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty AllowNullProperty = DependencyProperty.Register(
      nameof(AllowNull),
      typeof(bool),
      typeof(NumericUpDownText),
      new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public double Step
    {
      get { return (double)GetValue(StepProperty); }
      set { SetValue(StepProperty, value); }
    }

    public double? NumValue
    {
      get { return (double?)GetValue(NumValueProperty); }
      set { SetValue(NumValueProperty, value); }
    }

    public bool AllowNull
    {
      get { return (bool)GetValue(AllowNullProperty); }
      set { SetValue(AllowNullProperty, value); }
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
    #endregion

    public NumericUpDownText()
    {
      InitializeComponent();
    }

  }
}