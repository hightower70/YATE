///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// CRT like pixel shader filter for TV screen emulation
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace YATE.Effects
{
	/// <summary>CRT like filter effect</summary>
	public class CRTFilter : ShaderEffect
	{
		#region · Dependency Properties ·

		/// <summary>Input bitmap</summary>
		public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(CRTFilter), 0, SamplingMode.Auto);

		/// <summary>Width of the input bitmap</summary>
		public static readonly DependencyProperty WidthProperty =	DependencyProperty.Register("Width", typeof(float), typeof(CRTFilter), new UIPropertyMetadata(1.0f, PixelShaderConstantCallback(0)));

		/// <summary>Height of the input bitmap</summary>
		public static readonly DependencyProperty HeightProperty = DependencyProperty.Register("Height", typeof(float), typeof(CRTFilter), new UIPropertyMetadata(1.0f, PixelShaderConstantCallback(1)));

		/// <summary>Even line filtering parameter. The filter is single line 3 pixel wide neighbour filter. The x,y,z components of the value are the multiplers of the previous current and next pixel the w components is the divisor of the weighted pixel sum.</summary>
		public static readonly DependencyProperty EvenParamProperty = DependencyProperty.Register("EvenParam", typeof(Point4D), typeof(CRTFilter), new UIPropertyMetadata(new Point4D(0, 16, 0, 16), PixelShaderConstantCallback(2)));

		/// <summary>Odd line filtering parameter. The filter is single line 3 pixel wide neighbour filter. The x,y,z components of the value are the multiplers of the previous current and next pixel the w components is the divisor of the weighted pixel sum.</summary>
		public static readonly DependencyProperty OddParamProperty = DependencyProperty.Register("OddParam", typeof(Point4D), typeof(CRTFilter), new UIPropertyMetadata(new Point4D(0, 16, 0, 16), PixelShaderConstantCallback(3)));

		#endregion

		#region · Properties ·

		/// <summary>
		/// Input bitmat
		/// </summary>
		public virtual System.Windows.Media.Brush Input
		{
			get { return ((System.Windows.Media.Brush)(this.GetValue(InputProperty)));}
			set { this.SetValue(InputProperty, value); }
		}

		/// <summary>
		/// Width of the bitmap
		/// </summary>
		public float Width
		{
			get { return (float)GetValue(WidthProperty); }
			set { SetValue(WidthProperty, value); }
		}

		/// <summary>
		/// Height of the bitmap
		/// </summary>
		public float Height
		{
			get { return (float)GetValue(HeightProperty); }
			set {	SetValue(HeightProperty, value); }
		}

		/// <summary>
		/// Even line filtering parameter. The filter is single line 3 pixel wide neighbour filter. The x,y,z components of the value are the multiplers of the previous current and next pixel the w components is the divisor of the weighted pixel sum.
		/// </summary>
		public Point4D EvenParam
		{
			get { return (Point4D)GetValue(EvenParamProperty); }
			set { SetValue(EvenParamProperty, value); }
		}

		/// <summary>
		/// Odd line filtering parameter. The filter is single line 3 pixel wide neighbour filter. The x,y,z components of the value are the multiplers of the previous current and next pixel the w components is the divisor of the weighted pixel sum.
		/// </summary>
		public Point4D OddParam
		{
			get { return (Point4D)GetValue(OddParamProperty); }
			set { SetValue(OddParamProperty, value); }
		}

		#endregion

		#region · Constructor ·

		/// <summary>
		/// Default constructor
		/// </summary>
		public CRTFilter()
		{
			PixelShader pixelShader = new PixelShader();
			pixelShader.UriSource = new Uri("/YATE;component/Effects/CRTFilter.ps", UriKind.Relative);
			this.PixelShader = pixelShader;
			this.UpdateShaderValue(InputProperty);
			this.UpdateShaderValue(WidthProperty);
			this.UpdateShaderValue(HeightProperty);
			this.UpdateShaderValue(OddParamProperty);
			this.UpdateShaderValue(EvenParamProperty);
		}

		#endregion
	}
}


