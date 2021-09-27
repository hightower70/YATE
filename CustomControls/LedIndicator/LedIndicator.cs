using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomControls
{
  [TemplatePart(Name = LitPartName, Type = typeof(FrameworkElement))]
  [TemplatePart(Name = DarkPartName, Type = typeof(FrameworkElement))]
  public class LedIndicator : Control
  {
    #region · Constants ·
    private const string LitPartName = "PART_Lit";
    private const string DarkPartName = "PART_Dark";
    #endregion

    #region · Members ·
    private FrameworkElement m_element_lit;
    private FrameworkElement m_element_dark;
    #endregion

    #region · Constructors And Destructors ·


    #endregion

    #region · Properties ·

    public bool Value
    {
      get
      {
        var value = GetValue(ValueProperty);
        return value != null && (bool)value;
      }
      set
      {
        SetValue(ValueProperty, value);
      }
    }

    public Brush Color
    {
      get
      {
        var value = GetValue(ColorProperty);
        if (value != null) return (Brush)value;
        else return default(Brush);
      }
      set
      {
        SetValue(ColorProperty, value);
      }
    }

    #endregion

    #region · Non-Public Methods ·

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var indicator = d as LedIndicator;

      if (indicator == null)
        return;

      indicator.UpdateState();
    }


    private void UpdateState()
    {
      if(Value)
      {
        if (m_element_lit != null)
          m_element_lit.Visibility = Visibility.Visible;

        if (m_element_dark != null)
          m_element_dark.Visibility = Visibility.Hidden;
      }
      else
      {
        if (m_element_lit != null)
          m_element_lit.Visibility = Visibility.Hidden;

        if (m_element_dark != null)
          m_element_dark.Visibility = Visibility.Visible;
      }
    }
    #endregion


    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      m_element_dark = GetTemplateChild(DarkPartName) as FrameworkElement;
      m_element_lit = GetTemplateChild(LitPartName) as FrameworkElement;

      UpdateState();
    }

    #region · Dependency Properties ·

    public static readonly DependencyProperty ValueProperty =
      DependencyProperty.Register("Value", typeof(bool), typeof(LedIndicator), new PropertyMetadata(false, OnValuePropertyChanged));

    public static readonly DependencyProperty ColorProperty =
      DependencyProperty.Register("Color", typeof(Brush), typeof(LedIndicator), new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
    #endregion
  }
}

