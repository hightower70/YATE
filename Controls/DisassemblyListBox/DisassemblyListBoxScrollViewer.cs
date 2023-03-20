using CustomControls;
using System.Windows.Controls;

namespace YATE.Controls
{
  public class DisassemblyListBoxScrollViewer : ScrollViewer
  {
    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (this.Template != null)
      {
        AnnotatedScrollBar vertical_scrollbar = (AnnotatedScrollBar)GetTemplateChild("PART_VerticalScrollBar");

        if(TemplatedParent is DisassemblyListBox)
        {
          (TemplatedParent as DisassemblyListBox).SetVerticalScrollBar(vertical_scrollbar);
        }
      }
    }
  }
}
