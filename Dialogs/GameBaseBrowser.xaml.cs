using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TVCEmu.Dialogs
{
	/// <summary>
	/// Interaction logic for GameBaseBrowser.xaml
	/// </summary>
	public partial class GameBaseBrowser : Window
	{
		private string m_gamebase_path;
		private string m_gamebase_root;
		private OleDbConnection m_connection;

		private const string m_command_string = "SELECT Difficulty.Difficulty, Publishers.Publisher, Games.Name, Games.Comment, Games.MemoText, Genres.Genre, Languages.[Language], Years.[Year], Programmers.Programmer,  Games.ScrnshotFilename, Games.Filename " +
																									"FROM ((((((Difficulty INNER JOIN " +
																									"Games ON Difficulty.DI_Id = Games.DI_Id) INNER JOIN " +
																									"Genres ON Games.GE_Id = Genres.GE_Id) INNER JOIN " +
																									"Languages ON Games.LA_Id = Languages.LA_Id) INNER JOIN " +
																									"Programmers ON Games.PR_Id = Programmers.PR_Id) INNER JOIN " +
																									"Publishers ON Games.PU_Id = Publishers.PU_Id) INNER JOIN " +
																									"Years ON Games.YE_Id = Years.YE_Id)";

		public string SelectedFileName { get; private set; }

		public GameBaseBrowser()
		{
			SelectedFileName = "";
			m_gamebase_path = @"D:\Projects\Retro\TVCEmu.1\GameBase\Videoton TV Computer.mdb";
			m_gamebase_root = Path.GetDirectoryName(m_gamebase_path);

			m_connection = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source = " + m_gamebase_path);

			InitializeComponent();
			Bind();
		}

		private void Bind()
		{
			m_connection.Open();
			OleDbDataAdapter da = new OleDbDataAdapter(m_command_string, m_connection);
			DataTable dt = new DataTable();
			da.Fill(dt);
			m_connection.Close();

			dtGrid.ItemsSource = dt.DefaultView;
		}

		private void Dtgrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			DataRowView row = (DataRowView)e.AddedItems[0];

			string image_path = Path.Combine(m_gamebase_root, "Pictures", row["ScrnshotFilename"].ToString());
			Uri image_uri = new Uri(image_path);
			iScreenshoot.Source = new BitmapImage(image_uri);
		}

		private void bLoad_Click(object sender, RoutedEventArgs e)
		{
			SelectedFileName = Path.Combine(m_gamebase_root, "Games", ((DataRowView)dtGrid.SelectedItem)["Filename"].ToString());
			DialogResult = true;
		}
	}
}
