using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;

namespace CustomControls
{
	public class ImageButton : Button
	{
		#region Data members
		private Image m_image = null;
		private TextBlock m_text_block = null;
		#endregion

		public ImageButton()
		{
			StackPanel panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			panel.Margin = new System.Windows.Thickness(1);

			m_image = new Image();
			m_image.Width = 16;
			m_image.Height = 16;
			RenderOptions.SetBitmapScalingMode(m_image, BitmapScalingMode.NearestNeighbor);
			panel.Children.Add(m_image);
			m_image.Visibility = System.Windows.Visibility.Collapsed;

			m_text_block = new TextBlock();
			panel.Children.Add(m_text_block);

			this.Content = panel;
		}

		public string Text
		{
			get
			{
				if (m_text_block != null)
					return m_text_block.Text;
				else
					return String.Empty;
			}
			set
			{
				if (m_text_block != null)
				{
					m_text_block.Text = value;
					m_text_block.Margin = new Thickness(5,2,5,2);
					m_image.Margin = new Thickness(2,2,0,2);
				}
				else
				{
					m_text_block.Margin = new Thickness();
				}

			}
		}

		public ImageSource Image
		{
			get
			{
				if (m_image != null)
					return m_image.Source;
				else
					return null;
			}
			set
			{
				if (m_image != null)
				{
					m_image.Source = value;
					m_image.Visibility = System.Windows.Visibility.Visible;
				}
				else
				{
					m_image.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}

		public double ImageWidth
		{
			get
			{
				if (m_image != null)
					return m_image.Width;
				else
					return 0;
			}
			set
			{
				if (m_image != null)
					m_image.Width = value;
			}
		}

		public double ImageHeight
		{
			get
			{
				if (m_image != null)
					return m_image.Height;
				else
					return 0;
			}
			set
			{
				if (m_image != null)
					m_image.Height = value;
			}
		}

		protected override void OnClick()
		{
			this.Focus();

			if (IsEnabled)
				base.OnClick();
		}

		public static bool IsValid(DependencyObject parent)
		{
			// Validate all the bindings on the parent
			bool valid = true;
			LocalValueEnumerator localValues = parent.GetLocalValueEnumerator();
			while (localValues.MoveNext())
			{
				LocalValueEntry entry = localValues.Current;
				if (BindingOperations.IsDataBound(parent, entry.Property))
				{
					Binding binding = BindingOperations.GetBinding(parent, entry.Property);
					if (binding.ValidationRules.Count > 0)
					{
						BindingExpression expression = BindingOperations.GetBindingExpression(parent, entry.Property);
						expression.UpdateSource();

						if (expression.HasError)
						{
							valid = false;
						}
					}
				}
			}

			// Validate all the bindings on the children
			System.Collections.IEnumerable children = LogicalTreeHelper.GetChildren(parent);
			foreach (object obj in children)
			{
				if (obj is DependencyObject)
				{
					DependencyObject child = (DependencyObject)obj;
					if (!IsValid(child)) { valid = false; }
				}
			}
			return valid;
		}
	}
}
