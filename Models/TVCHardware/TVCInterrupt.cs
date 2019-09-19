using System.Runtime.CompilerServices;

namespace TVCHardware
{
	public class TVCInterrupt
	{
		private bool m_cursor_sound_it_flipflop = false;

		private TVComputer m_tvc;

		public TVCInterrupt(TVComputer in_tvc)
		{
			m_tvc = in_tvc;

			m_tvc.Ports.AddPortWriter(0x07, PortWrite07H);
			m_tvc.Ports.AddPortReader(0x59, PortRead59H);
		}

		public void GenerateCursorSoundInterrupt()
		{
			m_cursor_sound_it_flipflop = true;
		}

		// PORT 59H
		// ========
		//
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		// |               P E N D I N G  I T  R E Q U E S T               |
		// +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
		// |   -   |   -   |  -    |Cursor | Card3 | Card2 | Card1 | Card0 |
		// |       |       |       | Sound |       |       |       |       |
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		private void PortRead59H(ushort in_address, ref byte inout_data)
		{
			if (m_cursor_sound_it_flipflop)
				inout_data = (byte)(inout_data & ~0x10);
		}

		// PORT 07H
		// ========
		//
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		// |             C L E A R  C U R S O R / S O U N D  I T           |
		// +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
		// |   -   |   -   |  -    |   -   |   -   |   -   |   -   |   -   |
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		private void PortWrite07H(ushort in_address, byte in_data)
		{
			m_cursor_sound_it_flipflop = false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsIntActive()
		{
			return m_cursor_sound_it_flipflop;
		}
	}
}
