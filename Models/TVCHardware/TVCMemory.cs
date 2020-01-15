using System;
using System.IO;
using System.Runtime.CompilerServices;
using TVCEmu.Models.TVCFiles;
using Z80CPU;

namespace TVCHardware
{
	public class TVCMemory : IZ80Memory
	{

		////////////////////////////////////////////////////////////////
		//          RESET
		// FFFF  +-----------+-----------+-----------+-----------+ FFFFF
		//       |           |           |           |           |
		//       |           |           |           | IOMEM     |
		//       |           |           |           |           |
		// PAGE3 | CART (00) | SYS  (40) | U3   (80) +----- (C0) + E0000
		//       |           |           |           |           |
		//       |           |           |           | EXT       |
		//       |           |           |           |           |
		// C000  +-----------+-----------+-----------+-----------+ C000
		//       |           |           |
		// PAGE2 | VID  (00) | U2   (20) |
		//       |           |           |
		// 8000  +-----------+-----------+
		//       |           |           |
		// PAGE1 | U1   (00) | VID  (04) |     
		//       |           |           |
		// 4000  +-----------+-----------+-----------+-----------+
		//       |           |           |           |           |
		// PAGE0 | SYS  (00) | U0   (10) | CART (08) | U3   (18) |
		//       |           |           |           |           |
		// 0000  +-----------+-----------+-----------+-----------+
		//

		public const int PageCount = 4;
		public const int VideoPageCount = 4;
		public const int IOCardCount = 4;

		public const int Page0StartAddress = 0x0000;
		public const int Page1StartAddress = 0x4000;
		public const int Page2StartAddress = 0x8000;
		public const int Page3StartAddress = 0xc000;

		public const int PageLength = 0x4000;
		public const int SysMemLength = 0x8000;
		public const int ExtMemLength = 0x4000;
		public const int IOMemSize = 0x4000;
		public const int UserMemSize = 0x8000;
		public const int VideoMemSize = 0x8000;


		private delegate byte PageReader(ushort in_address);
		private delegate void PageWriter(ushort in_address, byte in_byte);

		private TVComputer m_tvc;

		private byte[] m_mem_sys;
		private byte[] m_mem_ext;
		private byte[][] m_mem_iomem;
		private byte[] m_mem_video;
		private byte[] m_mem_user;

		private PageReader[] m_page_reader;
		private PageWriter[] m_page_writer;

		private byte m_ext_mem_select;
		private byte m_port03;


		public byte Port02H { get; set; }

		public byte[] VisibleVideoMem
		{
			get { return m_mem_video; }
		}

		public TVCMemory(TVComputer in_tvc)
		{
			m_tvc = in_tvc;

			m_page_reader = new PageReader[PageCount];
			m_page_writer = new PageWriter[PageCount];

			// reserve space for memories
			m_mem_sys = new byte[SysMemLength];
			m_mem_ext = new byte[ExtMemLength];

			m_mem_iomem = new byte[IOCardCount][];
			for (int i = 0; i < IOCardCount; i++)
				m_mem_iomem[i] = new byte[IOMemSize];

			m_mem_user = new byte[PageCount * PageLength];
			m_mem_video = new byte[VideoPageCount * PageLength];

			// register port handlers
			m_tvc.Ports.AddPortWriter(0x02, PortWrite02H);
			m_tvc.Ports.AddPortWriter(0x03, PortWrite03H);
			m_tvc.Ports.AddPortReset(0x02, PortReset02H);
			m_tvc.Ports.AddPortReset(0x03, PortReset03H);
		}

		private void PortReset02H()
		{
			PortWrite02H(0x02, 0);
		}

		private void PortReset03H()
		{
			PortWrite03H(0x03, 0);
		}

		public void ClearMemory()
		{
			// clear video memory
			for (int i = 0; i < m_mem_user.Length; i++)
				m_mem_user[i] = 0;

			// clear main memory
			for (int i = 0; i < m_mem_user.Length; i++)
				m_mem_video[i] = 0;
		}

		public byte Read(ushort in_address, bool in_m1_state = false)
		{
			byte page_index = (byte)(in_address >> 14);
			ushort page_addres = (ushort)(in_address & 0x3fff);

			return m_page_reader[page_index](page_addres);
		}

		public void Write(ushort in_address, byte in_data)
		{
			byte page_index = (byte)(in_address >> 14);
			ushort page_addres = (ushort)(in_address & 0x3fff);

			m_page_writer[page_index](page_addres, in_data);
		}

		public byte DebuggerMemoryRead(ushort in_address)
		{
			byte page_index = (byte)(in_address >> 14);
			ushort page_addres = (ushort)(in_address & 0x3fff);

			return m_page_reader[page_index](page_addres);
		}

		public void SetCPU(Z80CPU.Z80 in_cpu)
		{
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte DebugUserMemoryReadByte(int in_address)
		{
			return m_mem_user[in_address & 0xffff];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DebugUserMemoryWriteByte(int in_address, byte in_data)
		{
			m_mem_user[in_address & 0xffff] = in_data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ushort DebugUserMemoryReadWord(int in_address)
		{
			return (ushort)((DebugUserMemoryReadByte(in_address + 1) << 8) + DebugUserMemoryReadByte(in_address));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DebugUserMemoryWriteWord(int in_address, ushort in_data)
		{
			DebugUserMemoryWriteByte(in_address, LowByte(in_data));
			DebugUserMemoryWriteByte(in_address+1, HighByte(in_data));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte LowByte(ushort in_data)
		{
			return (byte)(in_data & 0xff);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte HighByte(ushort in_data)
		{
			return (byte)(in_data >> 8);
		}

		public ushort VLOMEM
		{
			get { return DebugUserMemoryReadWord(0x1720); }
			set { DebugUserMemoryWriteWord(0x1720, value); }
		}

		public ushort TEXT
		{
			get { return DebugUserMemoryReadWord(0x1722); }
			set { DebugUserMemoryWriteWord(0x1722, value); }
		}

		public ushort CHAIN
		{
			get { return DebugUserMemoryReadWord(0x1724); }
			set { DebugUserMemoryWriteWord(0x1724, value); }
		}

		public ushort TOP
		{
			get { return DebugUserMemoryReadWord(0x1726); }
			set { DebugUserMemoryWriteWord(0x1726, value); }
		}

		// PORT 02H
		// ========
		//
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		// |                M E M O R Y  P A G I N G                       |
		// +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
		// |     Page3		 | Page2 |     Page0     | Page1 |       |       |
		// |    00 CART    | 0 VID |    00 SYS     | 1 U1  |   -   |   -   |
		// |    01 SYS     | 1 U2  |    01 CART    |       |       |       |
		// |    10 U3      |       |    10 U0      |       |       |       |
		// |    11 EXT     |       |               |       |       |       |
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		public void PortWrite02H(ushort in_address, byte in_data)
		{
			Port02H = in_data;

			// Page0
			switch (GetPage0Mapping())
			{
				case 0:
					m_page_reader[0] = MemSysRead;
					m_page_writer[0] = MemNoWrite;
					break;

				case 1:
					m_page_reader[0] = MemCartRead;
					m_page_writer[0] = MemCartWrite;
					break;

				case 2:
					m_page_reader[0] = MemU0Read;
					m_page_writer[0] = MemU0Write;
					break;

				case 3:
					m_page_reader[0] = MemU3Read;
					m_page_writer[0] = MemU3Write;
					break;
			}

			// Page1
			m_page_reader[1] = MemU1Read;
			m_page_writer[1] = MemU1Write;
			/*
			if ((in_data & 0x04) == 0)
			{
				m_active_pages[1] = m_mem_video[0];
				m_writable_page[1] = true;
			}
			else
			{
				m_active_pages[1] = m_mem_user[1];
				m_writable_page[1] = true;
			}
			*/

			// Page2
			switch (GetPage2Mapping())
			{
				case 0:
					m_page_reader[2] = MemVideoRead;
					m_page_writer[2] = MemVideoWrite;
					break;

				case 1:
					m_page_reader[2] = MemU2Read;
					m_page_writer[2] = MemU2Write;
					break;
			}

			// Page 3
			switch (GetPage3Mapping())
			{
				case 0:
					m_page_reader[3] = MemCartRead;
					m_page_writer[3] = MemCartWrite;
					break;

				case 1:
					m_page_reader[3] = MemSysRead;
					m_page_writer[3] = MemNoWrite;
					break;

				case 2:
					m_page_reader[3] = MemU3Read;
					m_page_writer[3] = MemU3Write;
					break;

				case 3:
					m_page_reader[3] = MemExtRead;
					m_page_writer[3] = MemExtWrite;
					break;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetPage0Mapping()
		{
			return (Port02H >> 3) & 0x03;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetPage2Mapping()
		{
			return (Port02H >> 5) & 0x01;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetPage3Mapping()
		{
			return (Port02H >> 6) & 0x03;
		}

		// PORT 03H
		// ========
		//
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		// |                E X T M E M  S E L E C T                       |
		// +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
		// |    00 CST0    |  -    |   -   |   -   |   -   |   -   |   -   |
		// |    01 CST1    |       |       |       |       |       |       |
		// |    10 CST2    |       |       |       |       |       |       |
		// |    11 CST3    |       |       |       |       |       |       |
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		public void PortWrite03H(ushort in_address, byte in_data)
		{
			m_port03 = in_data;
			m_ext_mem_select = (byte)(in_data >> 6);
		}

		public void LoadSystemMemory(string in_sys_name)
		{
			LoadMemoryContent(in_sys_name, 0, m_mem_sys);
		}

		public void LoadExtMemory(string in_ext_name)
		{
			LoadMemoryContent(in_ext_name, 0, m_mem_ext);
		}

		private void LoadMemoryContent(string name, ushort in_address, byte[] in_memory)
		{
			byte[] data = File.ReadAllBytes(name);

			Array.Copy(data, 0, in_memory, in_address, data.Length);
		}

		private void MemNoWrite(ushort in_page_address, byte in_data)
		{
			// no write (readonly)
		}

		private byte MemNoRead(ushort in_page_address)
		{
			return 0xff;
		}

		private byte MemSysRead(ushort in_page_address)
		{
			return m_mem_sys[in_page_address];
		}

		/// <summary>
		/// Page 0 RAM Read
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <returns></returns>
		private byte MemU0Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page0StartAddress];
		}

		/// <summary>
		/// Page 0 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		private void MemU0Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page0StartAddress] = in_data;
		}

		/// <summary>
		/// Page 1 RAM read
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <returns></returns>
		private byte MemU1Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page1StartAddress];
		}

		/// <summary>
		/// Page 1 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		private void MemU1Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page1StartAddress] = in_data;
		}

		/// <summary>
		/// Page 2 RAM read
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <returns></returns>
		private byte MemU2Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page2StartAddress];
		}

		/// <summary>
		/// Pagfe 2 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		private void MemU2Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page2StartAddress] = in_data;
		}

		/// <summary>
		/// Page 2 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <returns></returns>
		private byte MemU3Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page3StartAddress];
		}

		/// <summary>
		/// Page 3 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		private void MemU3Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page3StartAddress] = in_data;
		}

		/// <summary>
		/// Video memory read
		/// </summary>
		/// <param name="in_address"></param>
		/// <returns></returns>
		private byte MemVideoRead(ushort in_address)
		{
			VideoMemAccessCount++;
			return m_mem_video[in_address];
		}

		/// <summary>
		/// Video memory write
		/// </summary>
		/// <param name="in_address"></param>
		/// <param name="in_data"></param>
		private void MemVideoWrite(ushort in_address, byte in_data)
		{
			VideoMemAccessCount++;
			m_mem_video[in_address] = in_data;
		}

		private byte MemExtRead(ushort in_page_address)
		{
			if ((in_page_address & 0x2000) == 0)
			{
				// read IO mem
				if (m_tvc.Cards[m_ext_mem_select] != null)
					return m_tvc.Cards[m_ext_mem_select].CardMemoryRead(in_page_address);

				return 0xff;
			}
			else
			{
				return m_mem_ext[in_page_address & 0x1fff];
			}
		}

		private void MemExtWrite(ushort in_page_address, byte in_data)
		{
			if ((in_page_address & 0x2000) == 0)
			{
				// write IO mem
				if (m_tvc.Cards[m_ext_mem_select] != null)
					m_tvc.Cards[m_ext_mem_select].CardMemoryWrite(in_page_address, in_data);
			}
			else
			{
				// no write to ext memory
			}
		}

		private byte MemCartRead(ushort in_page_address)
		{
			if (m_tvc.Cartridge != null)
				return m_tvc.Cartridge.MemoryRead(in_page_address);

			return 0xff;
		}

		private void MemCartWrite(ushort in_page_address, byte in_data)
		{
			if (m_tvc.Cartridge != null)
				m_tvc.Cartridge.MemoryWrite(in_page_address, in_data);
		}

		public int VideoMemAccessCount { get; set; } = 0;

		private static readonly string[] m_page0_mapping_names = { "SYS", "CART", "U0", "U3" };
		private static readonly string[] m_page1_mapping_names = { "U1" };
		private static readonly string[] m_page2_mapping_names = { "VID", "U2" };
		private static readonly string[] m_page3_mapping_names = { "CART", "SYS", "U3", "EXT" };

		public string GetPageMappingNameString(int in_page_index)
		{

			switch (in_page_index)
			{
				case 0:
					return m_page0_mapping_names[GetPage0Mapping()];

				case 1:
					return m_page1_mapping_names[0];

				case 2:
					return m_page2_mapping_names[GetPage2Mapping()];

				case 3:
					return m_page3_mapping_names[GetPage3Mapping()];

				default:
					return string.Empty;
			}
		}

		public void LoadFromProgramStorage(ProgramStorage in_storage)
		{
			// store file in memory and update pointers
			ushort address = VLOMEM;

			for (int i = 0; i < in_storage.Length; i++)
			{
				DebugUserMemoryWriteByte(address + i, in_storage.Data[i]);
			}

			TEXT = VLOMEM;
			TOP = (ushort)(address + in_storage.Length + 1);
		}

		public void SaveToProgramStorage(ProgramStorage in_storage)
		{
			// store file in memory and update pointers
			ushort address = TEXT;
			ushort length = (ushort)(TOP - TEXT);

			for (int i = 0; i < length; i++)
			{
				in_storage.Data[i] = DebugUserMemoryReadByte(address + i);
			}

			in_storage.Length = length;
		}
	}
}
