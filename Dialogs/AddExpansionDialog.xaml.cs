using System.Media;
using System.Windows;
using System.Windows.Input;
using YATECommon.Expansions;
using static YATE.Managers.ExecutionManager;

namespace YATE.Dialogs
{
  /// <summary>
  /// Interaction logic for AddExpansionDialog.xaml
  /// </summary>
  public partial class AddExpansionDialog : Window
	{
    public ExpansionManager ExpansionManager { get; private set; }

    public ExpansionInfo SelectedExpansion { get; private set; }
    public int SelectedSlotIndex { get; set; }

    public AddExpansionDialog(Window in_owner, ExpansionManager in_expansion_manager)
		{
      SelectedExpansion = null;
      SelectedSlotIndex = -1;

      Owner = in_owner;
      ExpansionManager = in_expansion_manager;

			InitializeComponent();

      ExpansionManager.SetupRefreshAvailableExpansionInfo();
      lbExpansions.DataContext = ExpansionManager;
    }

    private void bSlotSelect_Click(object sender, RoutedEventArgs e)
    {
      CardSelected();
    }

    private void bBack_Click(object sender, RoutedEventArgs e)
    {
      gAvailableSlots.Visibility = Visibility.Collapsed;
      gAvailableExpansions.Visibility = Visibility.Visible;
    }

    private void lbCards_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      CardSelected();
    }

    private void bExpansionAdd_Click(object sender, RoutedEventArgs e)
		{
      ExpansionSelected();
    }

		private void lbExpansions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
      ExpansionSelected();
		}

    private void ExpansionSelected()
    {
      object selected_item = lbExpansions.SelectedItem;

      if (selected_item == null)
        return;

      SelectedExpansion = selected_item as ExpansionInfo;

      // if the selected expansion is a card then select slot as well
      if (SelectedExpansion.Type == ExpansionManager.ExpansionType.Card)
      {
        gAvailableExpansions.Visibility = Visibility.Collapsed;
        gAvailableSlots.Visibility = Visibility.Visible;

        ExpansionManager.SetupRefreshCardInfo();
        lbCards.DataContext = ExpansionManager;
      }
      else
      {
        DialogResult = true;
        Close();
      }
    }

    private void CardSelected()
    {
      if (lbCards.SelectedItems == null)
        return;

      if ( string.IsNullOrEmpty((lbCards.SelectedItem as ExpansionSetupCardInfo).ModuleName))
      {
        SelectedSlotIndex = (lbCards.SelectedItem as ExpansionSetupCardInfo).SlotIndex;

        DialogResult = true;
        Close();
      }
      else
      {
        SystemSounds.Beep.Play();
      }
    }
  }
}
