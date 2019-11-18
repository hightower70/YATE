using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace CustomControls
{
	[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
	[TemplatePart(Name = "PART_ListBox", Type = typeof(ListBox))]
	[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
	public class AutoCompleteTextBox : Control
	{
		#region Data members

		private TextBox m_text_box;
		private Popup m_popup;
		private ListBox m_list_box;

		private IEnumerable m_items_source = null;
		private int m_drop_down_count = 5;
		private bool m_suppress_change_event = false;
		private string m_lookup_field_name = null;
		private string m_lookup_key_name = null;

    private KeyConverter m_key_converter = new KeyConverter();

		#endregion

		#region Constructor

		public AutoCompleteTextBox()
		{
			this.GotFocus += new RoutedEventHandler(AutoCompleteTextBox_GotFocus);
		}

		void AutoCompleteTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (m_text_box != null && (m_popup == null || !m_popup.IsOpen))
				m_text_box.Focus();
		}

		static AutoCompleteTextBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(typeof(AutoCompleteTextBox)));
		}

		#endregion

		#region Properties



		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextProperty =
				DependencyProperty.Register("Text", typeof(string), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata { BindsTwoWayByDefault = true });


		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
		}


		/// <summary>
		/// 
		/// </summary>
		[Description("Number of element in the dropdown listbox"), Category("Other"), DefaultValue(5), Browsable(true)]
		public int DropDownCount
		{
			get { return m_drop_down_count; }
			set { m_drop_down_count = value; }
		}

		/// <summary>
		/// Name of the lookup field LookupField
		/// </summary>
		[Description("Name of the LookupKey field"), Category("Other"), DefaultValue(null), Browsable(true)]
		public string LookupKeyName
		{
			get { return m_lookup_key_name; }
			set { m_lookup_key_name = value; }
		}

		/// <summary>
		/// Name of the lookup field LookupField
		/// </summary>
		[Description("Name of the LookupField"), Category("Other"), DefaultValue(null), Browsable(true)]
		public string LookupFieldName
		{
			get { return m_lookup_field_name; }
			set { m_lookup_field_name = value; } 
		}

    #endregion

    #region LookupKeyValue Dependency Property

		public static readonly DependencyProperty LookupKeyValueProperty = DependencyProperty.Register("LookupKeyValue", typeof(int), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata { BindsTwoWayByDefault = true});

		public int LookupKeyValue
    {
        get { return (int)GetValue(LookupKeyValueProperty); }
        set { SetValue(LookupKeyValueProperty, value); }
    }

    #endregion

		#region ItemsSource Dependency Property

		public IEnumerable ItemsSource
		{
			get { return (IEnumerable)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public static readonly DependencyProperty ItemsSourceProperty =
				ItemsControl.ItemsSourceProperty.AddOwner(
						typeof(AutoCompleteTextBox),
						new UIPropertyMetadata(null, OnItemsSourceChanged));

		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AutoCompleteTextBox actb = d as AutoCompleteTextBox;
			if (actb == null) return;
			actb.OnItemsSourceChanged(e.NewValue as IEnumerable);
		}

		protected void OnItemsSourceChanged(IEnumerable in_items_source)
		{
			m_items_source = in_items_source;

			UpdateListBoxContent();

			if (m_list_box != null && m_list_box.Items.Count == 0) 
				InternalClosePopup();
		}

		#endregion

		#region ItemTemplate Dependency Property

		public DataTemplate ItemTemplate
		{
			get { return (DataTemplate)GetValue(ItemTemplateProperty); }
			set { SetValue(ItemTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ItemTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemTemplateProperty =
				ItemsControl.ItemTemplateProperty.AddOwner(
						typeof(AutoCompleteTextBox),
						new UIPropertyMetadata(null, OnItemTemplateChanged));

		private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AutoCompleteTextBox actb = d as AutoCompleteTextBox;
			if (actb == null) return;
			actb.OnItemTemplateChanged(e.NewValue as DataTemplate);
		}

		private void OnItemTemplateChanged(DataTemplate p)
		{
			if (m_list_box == null) return;
			m_list_box.ItemTemplate = p;
		}

		#endregion

		#region ItemContainerStyle Dependency Property

		public Style ItemContainerStyle
		{
			get { return (Style)GetValue(ItemContainerStyleProperty); }
			set { SetValue(ItemContainerStyleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ItemContainerStyle.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemContainerStyleProperty =
				ItemsControl.ItemContainerStyleProperty.AddOwner(
						typeof(AutoCompleteTextBox),
						new UIPropertyMetadata(null, OnItemContainerStyleChanged));

		private static void OnItemContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AutoCompleteTextBox actb = d as AutoCompleteTextBox;
			if (actb == null) return;
			actb.OnItemContainerStyleChanged(e.NewValue as Style);
		}

		private void OnItemContainerStyleChanged(Style p)
		{
			if (m_list_box == null) return;
			m_list_box.ItemContainerStyle = p;
		}

		#endregion

		#region ItemTemplateSelector Dependency Property

		public DataTemplateSelector ItemTemplateSelector
		{
			get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
			set { SetValue(ItemTemplateSelectorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ItemTemplateSelector.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemTemplateSelectorProperty =
				ItemsControl.ItemTemplateSelectorProperty.AddOwner(typeof(AutoCompleteTextBox), new UIPropertyMetadata(null, OnItemTemplateSelectorChanged));

		private static void OnItemTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AutoCompleteTextBox actb = d as AutoCompleteTextBox;
			if (actb == null) return;
			actb.OnItemTemplateSelectorChanged(e.NewValue as DataTemplateSelector);
		}

		private void OnItemTemplateSelectorChanged(DataTemplateSelector p)
		{
			if (m_list_box == null) return;
			m_list_box.ItemTemplateSelector = p;
		}

		#endregion
		
		#region Non-public members

		/// <summary>
		/// Updates ListBox content
		/// </summary>
		private void UpdateListBoxContent()
		{
			
			int item_count;

			if (m_items_source == null || m_list_box == null || m_text_box == null)
				return;

			m_list_box.Items.Clear();

      // get current item from m_text_cache
      string current_item = m_text_box.Text;

      // lookup in listbox
			item_count = 0;
			foreach (object item in m_items_source)
			{
				string item_to_compare = item.ToString();

				if (item_to_compare.StartsWith(current_item, StringComparison.CurrentCultureIgnoreCase))
				{
					m_list_box.Items.Add(item);
					item_count++;
					if (item_count >= m_drop_down_count)
						break;
				}
			}
		}

		private void InternalClosePopup()
		{
			if (m_popup != null)
				m_popup.IsOpen = false;
		}

		private void InternalOpenPopup()
		{
			m_popup.IsOpen = true;
			if (m_list_box != null)
			{
				m_list_box.Items.Refresh();
				m_list_box.SelectedIndex = -1;
			}
		}

		public void ShowPopup()
		{
			if (m_list_box == null || m_popup == null) 
				InternalClosePopup();
			else 
				if (m_list_box.Items.Count == 0) 
					InternalClosePopup();
				else
					InternalOpenPopup();
		}
								
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (this.Template != null)
			{
				// get PARTs

				// popup
				m_popup = Template.FindName("PART_Popup", this) as Popup;

				// ListBox
				ListBox list_box = Template.FindName("PART_ListBox", this) as ListBox;
				if (list_box != m_list_box)
				{
					// remove event handlers
					if (m_list_box != null)
					{
						m_list_box.PreviewKeyDown -= new KeyEventHandler(ListBox_PreviewKeyDown);
						m_list_box.PreviewMouseUp -= new MouseButtonEventHandler(ListBox_MouseUp);
						m_list_box.PreviewMouseDown -= new MouseButtonEventHandler(ListBox_MouseDown);
					}

					// add event handlers
					if (list_box != null)
					{
						list_box.PreviewKeyDown += new KeyEventHandler(ListBox_PreviewKeyDown);
						list_box.PreviewMouseUp += new MouseButtonEventHandler(ListBox_MouseUp);
						list_box.PreviewMouseDown += new MouseButtonEventHandler(ListBox_MouseDown);
					}

					m_list_box = list_box;

					OnItemsSourceChanged(ItemsSource);
					OnItemTemplateChanged(ItemTemplate);
					OnItemContainerStyleChanged(ItemContainerStyle);
					OnItemTemplateSelectorChanged(ItemTemplateSelector);
				}

				// RichTextBox
				TextBox text_box = Template.FindName("PART_TextBox", this) as TextBox;
				if (text_box != m_text_box)
				{
					if (text_box != m_text_box)
					{
						// remove event handlers
						if (m_text_box != null)
						{
							m_text_box.LostFocus -= new RoutedEventHandler(TextBox_LostFocus);
							m_text_box.PreviewKeyDown -= new KeyEventHandler(RichTextBox_PreviewKeyDown);
							m_text_box.TextChanged -= new TextChangedEventHandler(TextBox_TextChanged);
						}

						// add event handlers
						if (text_box != null)
						{
							text_box.LostFocus += new RoutedEventHandler(TextBox_LostFocus);
							text_box.PreviewKeyDown += new KeyEventHandler(RichTextBox_PreviewKeyDown);
							text_box.TextChanged += new TextChangedEventHandler(TextBox_TextChanged);

							if (IsFocused)
							{
								m_suppress_change_event = true;
								text_box.Focus();
								m_suppress_change_event = false;
							}
						}

						m_text_box = text_box;
					}
				}
			}
		}

		private void TextBox_TextChanged(object sender, EventArgs e)
		{
			if (m_popup != null && string.IsNullOrEmpty(m_text_box.Text))
			{
				InternalClosePopup();
			}
			else
			{
				if (m_list_box != null)
				{
					if (m_popup != null)
					{
						UpdateListBoxContent();

						if (m_list_box.Items.Count == 0)
						{
							InternalClosePopup();
							LookupKeyValue = -1;
						}
						else
						{

							InternalOpenPopup();
						}
					}
				}
			}
		}


		void ListBox_MouseDown(object sender, MouseButtonEventArgs e)
		{									
			if (!m_list_box.IsFocused)
			{
				m_suppress_change_event = true;
				m_list_box.Focus();
				m_suppress_change_event = false;
				e.Handled = true;
			}	
		}
	
		void ListBox_MouseUp(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dep = (DependencyObject)e.OriginalSource;
			while ((dep != null) && !(dep is ListBoxItem))
			{
				dep = VisualTreeHelper.GetParent(dep);
			}

			if (dep == null)
				return;

			var item = m_list_box.ItemContainerGenerator.ItemFromContainer(dep);
			if (item == null)
				return;

			this.Text = m_text_box.Text = item.ToString();

			InternalClosePopup();
			m_text_box.Focus();

			e.Handled = true;
		}

		private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					e.Handled = true;

					if (m_list_box != null && m_list_box.SelectedItem != null)
					{
						m_text_box.Text = m_list_box.SelectedItem.ToString();
						this.Text = m_text_box.Text;
						InternalClosePopup();
						m_text_box.Focus();
					}
					break;

				case Key.Escape:
					InternalClosePopup();
					m_text_box.Focus();
					e.Handled = true;
					break;
			}
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			if (m_suppress_change_event)
				return;

			if (m_popup != null)
			{
				InternalClosePopup();
			}

			// update items
			//UpdateItems();
		}

		private void RichTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Down:
					if (m_list_box != null)
					{
						m_suppress_change_event = true;
						m_list_box.Focus();
						m_suppress_change_event = false;
					}
					break;

				case Key.Escape:
					if (m_list_box != null)
					{
						InternalClosePopup();
						m_text_box.Focus();
						e.Handled = true;
					}
					break;

				case Key.Enter:
					this.Text = m_text_box.Text;
					if(m_list_box!=null)
					{
						InternalClosePopup();
						m_text_box.Focus();
					}
					break;

			}
		}

		#endregion

		#region · NotifyPropertyCahanged ·


		public event PropertyChangedEventHandler PropertyChanged;

		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}


		#endregion
	}
}
