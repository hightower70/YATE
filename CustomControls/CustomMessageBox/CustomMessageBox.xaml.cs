using System.Windows;

namespace CustomControls
{
	/// <summary>
	/// Interaction logic for MessageBox.xaml
	/// </summary>
	public partial class CustomMessageBox : Window
	{
		#region Data members
		private MessageBoxResult m_result;
		#endregion

		public CustomMessageBox()
		{
			InitializeComponent();
			m_result = MessageBoxResult.None;
		}

		public CustomMessageBox(Window in_owner)
		{
			InitializeComponent();

			Owner = in_owner;
			m_result = MessageBoxResult.None;
		}

		public MessageBoxResult ShowMessageBox(string in_title, string in_message, MessageBoxButton in_button, MessageBoxImage in_image)
		{

			// set icon visibility
			switch (in_image)
			{
				case MessageBoxImage.Error:
					iErrorIcon.Visibility = Visibility.Visible;
					break;

				case MessageBoxImage.Information:
					iInformationIcon.Visibility = Visibility.Visible;
					break;

				case MessageBoxImage.Question:
					iQuestionIcon.Visibility = Visibility.Visible;
					break;

				case MessageBoxImage.Warning:
					iWarningIcon.Visibility = Visibility.Visible;
					break;
			}

			// set button visibility
			switch (in_button)
			{
				case MessageBoxButton.OK:
					OkButton.Visibility = Visibility.Visible;
					break;

				case MessageBoxButton.OKCancel:
					OkCancelButton.Visibility = Visibility.Visible;
					break;

				case MessageBoxButton.YesNo:
					YesNo.Visibility = Visibility.Visible;
					break;

				case MessageBoxButton.YesNoCancel:
					YesNoCancel.Visibility = Visibility.Visible;
					break;
			}

			// update dialog content
			tbMessage.Text = in_message;
			Title = in_title;

			// show dialog
			ShowDialog();

			return m_result;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			m_result = MessageBoxResult.OK;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			m_result = MessageBoxResult.Cancel;
			Close();
		}

		private void YesButton_Click(object sender, RoutedEventArgs e)
		{
			m_result = MessageBoxResult.Yes;
			Close();
		}

		private void NoButton_Click(object sender, RoutedEventArgs e)
		{
			m_result = MessageBoxResult.No;
			Close();
		}

		private void DisableButtonPanels()
		{
			if (OkButton.Visibility == Visibility.Visible)
				OkButton.IsEnabled = false;

			if (OkCancelButton.Visibility == Visibility.Visible)
				OkCancelButton.IsEnabled = false;

			if (YesNo.Visibility == Visibility.Visible)
				YesNo.IsEnabled = false;

			if (YesNoCancel.Visibility == Visibility.Visible)
				YesNoCancel.IsEnabled = false;
		}

	}
}
