/*****************************************************************************/
/* Wait cursor display class                                                 */
/*                                                                           */
/* Copyright (c) Bay Zoltán Nonprofit Ltd. for Applied Research              */
/*****************************************************************************/
using System;
using System.Windows.Input;

namespace CustomControls
{
	public class WaitCursor : IDisposable
	{
		#region · Data members ·
		private Cursor m_previous_cursor;
		#endregion

		#region · Constructor ·
		public WaitCursor()
		{
			m_previous_cursor = Mouse.OverrideCursor;

			Mouse.OverrideCursor = Cursors.Wait;
		}
		#endregion

		#region · IDisposable Members ·

		public void Dispose()
		{
			Mouse.OverrideCursor = m_previous_cursor;
		}

		#endregion
	}
}
