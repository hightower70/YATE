using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Annotations;
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

    private class Annotation : IEquatable<Annotation>
    {
      public int Position;
      public bool Wide;
      public Brush Color;
      public Line Marker;

      public Annotation(int in_position, bool in_wide, Brush in_color)
      {
        Position = in_position;
        Wide = in_wide;
        Color = in_color;
        Marker = null;
      }

      public bool Equals(Annotation other)
      {
        return Position == other.Position && Wide == other.Wide && Color.Equals(other.Color);
      }
    }

    private List<Annotation> m_annotation = new List<Annotation>();
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
      if (m_canvas.Children.Count != m_annotation.Count)
      {
        // annotation markers cont doen't match with the annotation count -> create markers
        m_canvas.Children.Clear();

        for (int i = 0; i < m_annotation.Count; i++)
          AddAnnotationMarker(m_annotation[i]);
      }
      else
      {
        for (int i = 0; i < m_annotation.Count; i++)
        {
          Line line = m_annotation[i].Marker;

          line.Y1 = line.Y2 = m_annotation[i].Position / max_pos * m_track.ActualHeight;
        }
      }
    }

    public void AddAnnotation(int in_position, bool in_wide, Brush in_color)
    {
      Annotation annotation = new Annotation(in_position, in_wide, in_color);
      m_annotation.Add(annotation);

      if (m_canvas != null && m_track != null)
        AddAnnotationMarker(annotation);
    }

    public void RemoveAnnotation(int in_position, bool in_wide, Brush in_color)
    {
      int annotation_index = m_annotation.IndexOf(new Annotation(in_position, in_wide, in_color));

      if (annotation_index >= 0)
      {
        Line line = m_annotation[annotation_index].Marker;

        m_annotation.RemoveAt(annotation_index);

        if (line != null)
        {
          m_canvas.Children.Remove(line);
          /*
          for (int i = 0; i < m_canvas.Children.Count; i++)
          {
            Line line = m_canvas.Children[i] as Line;
            Annotation annotation = line.Tag as Annotation;

            if (annotation.Position == in_position && line.Stroke == in_color)
            {
              m_canvas.Children.RemoveAt(i);
              break;
            }
          } */
        }
      }
    }


    private void AddAnnotationMarker(Annotation in_annotation)
    {
      double pos = 0;

      if (Maximum > 0)
      {
        double max_pos = Maximum + ViewportSize;
        pos = in_annotation.Position / max_pos * m_track.ActualHeight;
      }

      Line line = new Line();
      line.X1 = 0;

      if (in_annotation.Wide)
        line.X2 = this.ActualWidth;
      else
        line.X2 = MarkerWidth;

      line.Y1 = pos;
      line.Y2 = pos;

      line.StrokeThickness = MarkerThickness;
      line.Stroke = in_annotation.Color;
      in_annotation.Marker = line;

      m_canvas.Children.Add(line);
    }


  }
}