using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CustomControls
{
  /// <summary>
  /// Interaction logic for EditableLabel.xaml
  /// </summary>
  public partial class EditableLabel : UserControl
  {
    public delegate string EditToLabelConverterDelegate(string text);

    // 'Text' dependency property
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(EditableLabel), new PropertyMetadata(OnTextChangedCallBack));

    public string Text
    {
      get { return (string)GetValue(TextProperty); }
      set { SetValue(TextProperty, value); }
    }

    // 'ResultText' dependency property
    internal static readonly DependencyPropertyKey ResultTextProperty =
    DependencyProperty.RegisterReadOnly("ResultText", typeof(string), typeof(EditableLabel), new FrameworkPropertyMetadata());

    public string ResultText
    {
      get { return (string)GetValue(ResultTextProperty.DependencyProperty); }
      private set { SetValue(ResultTextProperty.DependencyProperty, value); }
    }

    // 'TextToResultTextConversion' event
    public static readonly RoutedEvent TextToResultTextConversionEvent = EventManager.RegisterRoutedEvent
               ("TextToResultText", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>), typeof(EditableLabel));

    public event RoutedEventHandler TextToResultTextConversion
    {
      add { this.AddHandler(TextToResultTextConversionEvent, value); }
      remove { this.RemoveHandler(TextToResultTextConversionEvent, value); }
    }

    public EditToLabelConverterDelegate EditToLabelConverter { get; set; }

    private string m_original_text;

    public EditableLabel()
    {
      InitializeComponent();
      m_original_text = null;

      //Disable AllowDrop on textbox to prevent accidental input when double-clicking label to edit
      txtEdit.AllowDrop = false;

      //Label is visible and not editable initally
      txtEdit.Visibility = Visibility.Hidden;
    }

    private void lblView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      txtEdit.Visibility = Visibility.Visible;
      txtEdit.AllowDrop = false;
      m_original_text = txtEdit.Text;
      txtEdit.Focus();
    }

    private void txtEdit_KeyDown(object sender, KeyEventArgs e)
    {
      //User presses Enter key: end edit and set label text to match textbox
      if (e.Key == Key.Enter)
      {
        txtEdit.Visibility = Visibility.Hidden;
        lblView.Content = ConvertEditToLabel(txtEdit.Text);
      }

      //User presses Tab key: end edit and set label text to match textbox
      if (e.Key == Key.Tab)
      {
        txtEdit.Visibility = Visibility.Hidden;
        lblView.Content = ConvertEditToLabel(txtEdit.Text);
      }
                                       
      //User presses ESCAPE key: cancel edit and return don't change label text
      if (e.Key == Key.Escape)
      {
        txtEdit.Text = m_original_text;
        txtEdit.Visibility = Visibility.Hidden;
      }     
    }

    private void TxtEdit_LostFocus(object sender, RoutedEventArgs e)
    {
      txtEdit.Visibility = Visibility.Hidden;
      lblView.Content = ConvertEditToLabel(txtEdit.Text);
    }

    private string ConvertEditToLabel(string in_text)
    {
      if (EditToLabelConverter != null)
      {
        return EditToLabelConverter(in_text);
      }
      else
      {
        return in_text;
      }
    }

    private static void OnTextChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
      EditableLabel el = sender as EditableLabel;
      if (el != null)
      {
        el.OnTextChanged((string)e.NewValue);
      }
    }

    protected virtual void OnTextChanged(string in_new_value)
    {
      txtEdit.Text = in_new_value;
      lblView.Content = ConvertEditToLabel(in_new_value);
    }
  }
}
