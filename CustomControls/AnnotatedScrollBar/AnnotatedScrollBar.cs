using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CustomControls
{
  public class AnnotatedScrollBar : ScrollBar
  {

    private const double MarkerThickness = 2;
    private const double MarkerWidth = 3;

    private class Annotation
    {
      public double Position;

      public Annotation(double in_position)
      {
        Position = in_position;
      }
    }


    private Canvas m_canvas;
    private Track m_track;

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (this.Template != null)
      {
        // get PARTs
        m_canvas = Template.FindName("PART_Annotation", this) as Canvas;
        m_track = Template.FindName("PART_Track", this) as Track;
      }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
      base.OnRenderSizeChanged(sizeInfo);

      double max_pos = Maximum + ViewportSize;

      // update annotation position
      for (int i = 0; i < m_canvas.Children.Count; i++)
      {
        Line line = m_canvas.Children[i] as Line;
        Annotation annotation = line.Tag as Annotation;

        line.Y1 = line.Y2 = annotation.Position / max_pos * m_track.ActualHeight;
      }
    }

    public void AddAnnotation(double in_position, bool in_wide, Brush in_color)
    {
      if (Maximum <= 0)
        return;

      double max_pos = Maximum + ViewportSize;

      if (in_position >= max_pos)
        in_position = max_pos - 1;

      Line line = new Line();
      line.X1 = 0;

      if (in_wide)
        line.X2 = this.ActualWidth;
      else
        line.X2 = MarkerWidth;

      double pos = in_position / max_pos * m_track.ActualHeight;

      line.Y1 = pos;
      line.Y2 = pos;

      line.StrokeThickness = MarkerThickness;
      line.Stroke = in_color;

      line.Tag = new Annotation(in_position);

      m_canvas.Children.Add(line);
    }

    public void RemoveAnnotation(double in_position, bool in_wide, Brush in_color)
    {
      for (int i = 0; i < m_canvas.Children.Count; i++)
      {
        Line line = m_canvas.Children[i] as Line;
        Annotation annotation = line.Tag as Annotation;

        if(annotation.Position == in_position && line.Stroke == in_color)
        {
          m_canvas.Children.RemoveAt(i);
          break;
        }
      }
    }
  }
}