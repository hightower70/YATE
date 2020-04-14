﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TVCEmu.Dialogs
{
	/// <summary>
	/// Interaction logic for AddModuleDialog.xaml
	/// </summary>
	public partial class CardSlotSelectDialog : Window
	{
		public CardSlotSelectDialog()
		{
			InitializeComponent();
		}

		private void bAdd_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void lbModules_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			DialogResult = true;
			Close();
		}

	}
}
