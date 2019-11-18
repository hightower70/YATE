/*****************************************************************************/
/* Title control                                                             */
/*                                                                           */
/* Copyright (c) Bay Zoltán Nonprofit Ltd. for Applied Research              */
/*****************************************************************************/
using System.Windows;
using System.Windows.Controls;

namespace CustomControls
{
	public class Title : UserControl
	{
		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextProperty =
				DependencyProperty.Register("Text", typeof(string), typeof(Title), new PropertyMetadata(""));
	}
}
