/*****************************************************************************/
/* Zoom and pan border                                                       */
/*                                                                           */
/* Copyright (c) Bay Zoltán Nonprofit Ltd. for Applied Research              */
/*****************************************************************************/
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CustomControls
{
	public class ZoomAndPanBorder : Border, INotifyPropertyChanged
	{
		#region · Data members · 
		private double ZoomStep = 0.2;
		private double MinZoom = 0.1;

		private UIElement m_child = null;
		private Point m_origin;
		private Point m_start;
		private TranslateTransform m_translate_transform;
		private ScaleTransform m_scale_transform;
		private MouseButton m_current_pan_button;
		#endregion

		#region · Properties ·

		/// <summary>
		/// Current offset in X direction
		/// </summary>
		public double OriginX
		{
			get { return m_translate_transform.X; }
			set { m_translate_transform.X = value; OnPropertyChangedMember(); }
		}

		/// <summary>
		/// Current offset in Y direction
		/// </summary>
		public double OriginY
		{
			get { return m_translate_transform.Y; }
			set { m_translate_transform.Y = value; OnPropertyChangedMember(); }
		}

		/// <summary>
		/// Current zoom
		/// </summary>
		public double Zoom
		{
			get { return m_scale_transform.ScaleX; }
			set { m_scale_transform.ScaleX = m_scale_transform.ScaleY = value; OnPropertyChangedMember(); }
		}

		/// <summary>
		/// Child elements
		/// </summary>
		public override UIElement Child
		{
			get { return base.Child; }
			set
			{
				if (value != null && value != this.Child)
					this.Initialize(value);
				base.Child = value;
			}
		}
		#endregion

		#region · Public members ·

		/// <summary>
		/// Initializes this control
		/// </summary>
		/// <param name="element"></param>
		public void Initialize(UIElement element)
		{
			this.m_child = element;
			if (m_child != null)
			{
				TransformGroup group = new TransformGroup();

				m_scale_transform = new ScaleTransform();
				group.Children.Add(m_scale_transform);

				m_translate_transform = new TranslateTransform();
				group.Children.Add(m_translate_transform);

				m_child.RenderTransform = group;
				m_child.RenderTransformOrigin = new Point(0.0, 0.0);
				this.MouseWheel += child_MouseWheel;
				this.MouseMove += child_MouseMove;
				this.MouseDown += ZoomAndPanBorder_MouseDown;
				this.MouseUp += ZoomAndPanBorder_MouseUp;
			}
		}

		/// <summary>
		/// Increases zoom factor
		/// </summary>
		public void ZoomIn()
		{
			double zoom_step = ZoomStep;

			double center_x = (this.ActualWidth / 2 - m_translate_transform.X) /  m_scale_transform.ScaleX;
			double center_y = (this.ActualHeight / 2 - m_translate_transform.Y) / m_scale_transform.ScaleY;

			double scale = m_scale_transform.ScaleX * 1.5;

			Zoom = scale;

			OriginX = this.ActualWidth / 2 - center_x * scale;
			OriginY = this.ActualHeight / 2 - center_y * scale;
		}

		/// <summary>
		/// Decreases zoom factor
		/// </summary>
		public void ZoomOut()
		{
			double zoom_step = -ZoomStep;

			double center_x = (this.ActualWidth / 2 - m_translate_transform.X) / m_scale_transform.ScaleX;
			double center_y = (this.ActualHeight / 2 - m_translate_transform.Y) / m_scale_transform.ScaleY;

			double scale = m_scale_transform.ScaleX / 1.5;

			if (scale < MinZoom)
				scale = MinZoom;

			Zoom = m_scale_transform.ScaleY = scale;

			OriginX = this.ActualWidth / 2 - center_x * scale;
			OriginY = this.ActualHeight / 2 - center_y * scale;
		}

		/// <summary>
		/// Resets zoom and origin
		/// </summary>
		public void Reset()
		{
			if (m_child != null)
			{
				// reset zoom
				Zoom = 1.0;

				// reset pan
				OriginX = 0;
				OriginY = 0;
			}
		}
		#endregion

		#region · Child Events ·

		private void child_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (m_child != null)
			{
				double zoom_step = e.Delta > 0 ? ZoomStep : -ZoomStep;

				if (!(e.Delta > 0) && (m_scale_transform.ScaleX < MinZoom || m_scale_transform.ScaleY < MinZoom))
					return;

				Point relative = e.GetPosition(m_child);

				UpdateZoom(relative, zoom_step);
			}
		}

		private void UpdateZoom(Point in_relative_position, double in_zoom_step)
		{
			double absoluteX;
			double absoluteY;

			absoluteX = in_relative_position.X * m_scale_transform.ScaleX + m_translate_transform.X;
			absoluteY = in_relative_position.Y * m_scale_transform.ScaleY + m_translate_transform.Y;

			double new_zoom = m_scale_transform.ScaleX + in_zoom_step * m_scale_transform.ScaleX;
			Zoom = new_zoom;

			OriginX = absoluteX - in_relative_position.X * m_scale_transform.ScaleX;
			OriginY = absoluteY - in_relative_position.Y * m_scale_transform.ScaleY;
		}

		private void ZoomAndPanBorder_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (m_child != null)
			{
				if (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right)
				{
					m_current_pan_button = e.ChangedButton;
					m_start = e.GetPosition(this);
					m_origin = new Point(m_translate_transform.X, m_translate_transform.Y);
					this.Cursor = Cursors.Hand;
					m_child.CaptureMouse();
				}
			}
		}

		private void ZoomAndPanBorder_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (m_child != null)
			{
				if (m_child.IsMouseCaptured && e.ChangedButton == m_current_pan_button)
				{
					m_child.ReleaseMouseCapture();
					this.Cursor = Cursors.Arrow;
				}
			}
		}

		private void child_MouseMove(object sender, MouseEventArgs e)
		{
			if (m_child != null)
			{
				if (m_child.IsMouseCaptured)
				{
					Vector v = m_start - e.GetPosition(this);

					m_translate_transform.X = m_origin.X - v.X;
					m_translate_transform.Y = m_origin.Y - v.Y;

					OnPropertyChanged("OriginX");
					OnPropertyChanged("OriginY");
				}
			}
		}

		#endregion

		#region · OnPropertyChanged Events ·

		private void OnPropertyChangedMember([CallerMemberName] string propertyName = null)
		{
			if (PropertyChanged != null && propertyName != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
