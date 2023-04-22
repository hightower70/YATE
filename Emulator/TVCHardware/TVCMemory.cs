using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using YATE.Emulator.Files;
using YATE.Emulator.Z80CPU;
using YATE.Settings;
using YATECommon;
using YATECommon.Helpers;
using YATECommon.Settings;

namespace YATE.Emulator.TVCHardware
{
	public class TVCMemory : IZ80Memory, ITVCMemory
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

		public const int Page0StartAddress = 0x0000;
		public const int Page1StartAddress = 0x4000;
		public const int Page2StartAddress = 0x8000;
		public const int Page3StartAddress = 0xc000;

		public const int PageLength = 0x4000;
		public const int VideoPageLength = 0x4000;
		public const int SysMemLength = 0x4000;
		public const int ExtMemLength = 0x2000;
		public const int IOMemSize = 0x4000;

		private TVComputer m_tvc;

		private byte[] m_mem_sys;
		private byte[] m_mem_ext;
		private byte[][] m_mem_iomem;
		private byte[][] m_mem_video;
		private byte[] m_mem_user;

		private TVCMemoryReadDelegate[] m_page_reader;
		private TVCMemoryWriteDelegate[] m_page_writer;

		private byte m_ext_mem_select;
		private byte m_port03;

		private byte m_port0f;
		private byte[] m_page1_video_mem;
		private byte[] m_page2_video_mem;

		private TVCMemoryType[] m_memory_types;
    
    private IDebuggableMemory[] m_slot_memory;
		private TVCMemoryType m_slot_selected_memory_type;
		private IDebuggableMemory m_slot_selected_memory;

		private TVCMemoryReadDelegate m_u2_reader;
		private TVCMemoryWriteDelegate m_u2_writer;
		private TVCMemoryReadDelegate m_u3_reader;
		private TVCMemoryWriteDelegate m_u3_writer;

		private TVCDebuggableMemory m_debuggable_ram;
		private TVCDebuggableMemory m_debuggable_sys;
    private TVCDebuggableMemory m_debuggable_ext;

    public byte Port02H { get; set; }

		private TVCConfigurationSettings m_settings;

		public byte[] VisibleVideoMem { get; private set; }

		public TVCMemory(TVComputer in_tvc)
		{
			// load configuration
			m_settings = SettingsFile.Default.GetSettings<TVCConfigurationSettings>();

			m_tvc = in_tvc;

			m_page_reader = new TVCMemoryReadDelegate[PageCount];
			m_page_writer = new TVCMemoryWriteDelegate[PageCount];

			m_memory_types = new TVCMemoryType[PageCount];
			m_memory_types[0] = TVCMemoryType.System;
			m_memory_types[1] = TVCMemoryType.RAM;
			m_memory_types[2] = TVCMemoryType.Video;
			m_memory_types[3] = TVCMemoryType.Cart;

			// init RAM
      m_mem_user = new byte[PageCount * PageLength];
      m_debuggable_ram = new TVCDebuggableMemory(m_mem_user, TVCMemoryType.RAM, 0);

      // init system ROM
      m_mem_sys = new byte[SysMemLength];
      m_debuggable_sys = new TVCDebuggableMemory(m_mem_sys, TVCMemoryType.System, 0xc000);

			// init Extension ROM
      m_mem_ext = new byte[ExtMemLength];
			m_debuggable_ext = new TVCDebuggableMemory(m_mem_ext, TVCMemoryType.SystemExtension, 0xe000);

      // init slot memory
      m_slot_memory = new IDebuggableMemory[TVComputerConstants.ExpansionCardCount];
			for (int i = 0; i < m_slot_memory.Length; i++)
			{
				m_slot_memory[i] = null;
			}
			m_slot_selected_memory = null;

			// Init slot memory
			m_mem_iomem = new byte[TVComputerConstants.ExpansionCardCount][];
			for (int i = 0; i < m_mem_iomem.Length; i++)
				m_mem_iomem[i] = null;

			// video memory
			m_mem_video = new byte[VideoPageCount][];
			for (int i = 0; i < VideoPageCount; i++)
				m_mem_video[i] = new byte[VideoPageLength];

			VisibleVideoMem = m_mem_video[0];
			m_page1_video_mem = m_mem_video[0];
			m_page2_video_mem = m_mem_video[0];

			// register port handlers
			m_tvc.Ports.AddPortWriter(0x02, PortWrite02H);
			m_tvc.Ports.AddPortWriter(0x03, PortWrite03H);
			m_tvc.Ports.AddPortWriter(0x0F, PortWrite0FH);

			m_tvc.Ports.AddPortReset(0x02, PortReset02H);
			m_tvc.Ports.AddPortReset(0x03, PortReset03H);
			m_tvc.Ports.AddPortReset(0x0F, PortReset0FH);

			// U2-U3 memory handler
			SetU2UserMemoryHandler(null, null);
			SetU3UserMemoryHandler(null, null);

			// loads ROM content
			LoadROM();
		}

		private void PortReset02H()
		{
			PortWrite02H(0x02, 0);
		}

		private void PortReset03H()
		{
			PortWrite03H(0x03, 0);
		}

		private void PortReset0FH()
		{
			PortWrite0FH(0x0F, 0);
		}

		/// <summary>
		/// Updates settings
		/// </summary>
		/// <param name="in_restart_tvc">True - if TVC needs to be restarted</param>
		public void SettingsChanged(ref bool in_restart_tvc)
		{
			// load configuration
			TVCConfigurationSettings new_settings = SettingsFile.Default.GetSettings<TVCConfigurationSettings>();

			if (m_settings.HardwareVersion != new_settings.HardwareVersion)
				in_restart_tvc = true;

			// update configuration
			m_settings = new_settings;

			SetU2UserMemoryHandler(null, null);
			SetU3UserMemoryHandler(null, null);

			if (LoadROM())
				in_restart_tvc = true;
		}

		/// <summary>
		/// Loads ROM content
		/// </summary>
		/// <returns>Returns true if new content detected</returns>
		private bool LoadROM()
		{
			bool memory_changed = false;

			// load ROM content
			switch (m_settings.ROMVersion)
			{
				// custom version
				case 0:
					break;

				// BASIC 1.2
				case 1:
					ROMFile.LoadMemoryFromResource("YATE.Resources.rom_1_2.bin", m_mem_sys, ref memory_changed);
					ROMFile.LoadMemoryFromResource("YATE.Resources.ext_1_2.bin", m_mem_ext, ref memory_changed);
					break;

				// BASIC 1.2 (RU)
				case 2:
					ROMFile.LoadMemoryFromResource("YATE.Resources.rom_1_2_ru.bin", m_mem_sys, ref memory_changed);
					ROMFile.LoadMemoryFromResource("YATE.Resources.ext_1_2_ru.bin", m_mem_ext, ref memory_changed);
					break;

				// BASIC 2.1
				case 3:
					ROMFile.LoadMemoryFromResource("YATE.Resources.rom_2_1.bin", m_mem_sys, ref memory_changed);
					ROMFile.LoadMemoryFromResource("YATE.Resources.ext_2_1.bin", m_mem_ext, ref memory_changed);
					break;

				// BASIC 2.2
				case 4:
					ROMFile.LoadMemoryFromResource("YATE.Resources.rom_2_2.bin", m_mem_sys, ref memory_changed);
					ROMFile.LoadMemoryFromResource("YATE.Resources.ext_2_2.bin", m_mem_ext, ref memory_changed);
					break;
			}

			return memory_changed;
		}

		/// <summary>
		/// Clears memory content
		/// </summary>
		public void ClearMemory()
		{
			// clear main memory
			for (int i = 0; i < m_mem_user.Length; i++)
				m_mem_user[i] = 0;

			// clear video memory
			for (int j = 0; j < VideoPageCount; j++)
			{
				for (int i = 0; i < VideoPageLength; i++)
					m_mem_video[j][i] = 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte Read(ushort in_address)
		{
			byte page_index = (byte)(in_address >> 14);
			ushort page_addres = (ushort)(in_address & 0x3fff);

			return m_page_reader[page_index](page_addres);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte Read(ushort in_address, bool in_m1_state)
		{
			byte page_index = (byte)(in_address >> 14);
			ushort page_addres = (ushort)(in_address & 0x3fff);

			return m_page_reader[page_index](page_addres);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		public TVCMemoryType GetMemoryTypeAtAddress(ushort in_address)
		{
			int index = in_address >> 14;

			return m_memory_types[index];
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
			DebugUserMemoryWriteByte(in_address + 1, HighByte(in_data));
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
		// |    00 CART    | 0 VID |    00 SYS     | 0 U1  |   -   |   -   |
		// |    01 SYS     | 1 U2  |    01 CART    | 1 VID*|       |       |
		// |    10 U3      |       |    10 U0      |       |       |       |
		// |    11 EXT     |       |    11 U3**    |       |       |       |
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		// * 64k+ only
		// ** 32k (with memory expansion) and 64k+ only
		public void PortWrite02H(ushort in_address, byte in_data)
		{
			Port02H = in_data;

			// Page0
			switch (GetPage0Mapping())
			{
				case 0:
					m_page_reader[0] = MemSysRead;
					m_page_writer[0] = MemNoWrite;
					m_memory_types[0] = TVCMemoryType.System;
					break;

				case 1:
					m_page_reader[0] = MemCartRead;
					m_page_writer[0] = MemCartWrite;
					m_memory_types[0] = TVCMemoryType.Cart;
					break;

				case 2:
					m_page_reader[0] = MemU0Read;
					m_page_writer[0] = MemU0Write;
					m_memory_types[0] = TVCMemoryType.RAM;
					break;

				case 3:
					switch (m_settings.HardwareVersion)
					{
						case SetupTVCConfigurationDataProvider.TVCHardwareVersion32k:
							m_page_reader[0] = m_u3_reader;
							m_page_writer[0] = m_u3_writer;
							m_memory_types[0] = TVCMemoryType.RAM;
							break;

						case SetupTVCConfigurationDataProvider.TVCHardwareVersion64kPaging:
						case SetupTVCConfigurationDataProvider.TVCHardwareVersion64kplus:
							m_page_reader[0] = MemU3Read;
							m_page_writer[0] = MemU3Write;
							m_memory_types[0] = TVCMemoryType.RAM;
							break;

						default:
							m_page_reader[0] = MemU0Read;
							m_page_writer[0] = MemU0Write;
							m_memory_types[0] = TVCMemoryType.RAM;
							break;
					}
					break;
			}

			// Page1
			switch (GetPage1Mapping())
			{
				case 0:
					m_page_reader[1] = MemU1Read;
					m_page_writer[1] = MemU1Write;
					m_memory_types[1] = TVCMemoryType.RAM;
					break;

				case 1:
					if (m_settings.HardwareVersion == SetupTVCConfigurationDataProvider.TVCHardwareVersion64kplus) // Only 64k+
					{
						m_page_reader[1] = MemVideo1Read;
						m_page_writer[1] = MemVideo1Write;
						m_memory_types[1] = TVCMemoryType.Video;
					}
					else
					{
						m_page_reader[1] = MemU1Read;
						m_page_writer[1] = MemU1Write;
						m_memory_types[1] = TVCMemoryType.RAM;
					}
					break;
			}

			// Page2
			switch (GetPage2Mapping())
			{
				case 0:
					m_page_reader[2] = MemVideo2Read;
					m_page_writer[2] = MemVideo2Write;
					m_memory_types[2] = TVCMemoryType.Video;
					break;

				case 1:
					m_page_reader[2] = m_u2_reader;
					m_page_writer[2] = m_u2_writer;
					m_memory_types[2] = TVCMemoryType.RAM;
					break;
			}

			// Page 3
			switch (GetPage3Mapping())
			{
				case 0:
					m_page_reader[3] = MemCartRead;
					m_page_writer[3] = MemCartWrite;
					m_memory_types[3] = TVCMemoryType.Cart;
					break;

				case 1:
					m_page_reader[3] = MemSysRead;
					m_page_writer[3] = MemNoWrite;
					m_memory_types[3] = TVCMemoryType.System;
					break;

				case 2:
					m_page_reader[3] = m_u3_reader;
					m_page_writer[3] = m_u3_writer;
					m_memory_types[3] = TVCMemoryType.RAM;
					break;

				case 3:
					m_page_reader[3] = MemExtRead;
					m_page_writer[3] = MemExtWrite;
					switch (m_ext_mem_select)
					{
						case 0:
							m_memory_types[3] = TVCMemoryType.Slot0;
							break;

						case 1:
							m_memory_types[3] = TVCMemoryType.Slot1;
							break;

						case 2:
							m_memory_types[3] = TVCMemoryType.Slot2;
							break;

						case 3:
							m_memory_types[3] = TVCMemoryType.Slot3;
							break;
					}
					break;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetPage0Mapping()
		{
			return (Port02H >> 3) & 0x03;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetPage1Mapping()
		{
			return (Port02H >> 2) & 0x01;
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

			switch (m_ext_mem_select)
			{
				case 0:
					m_slot_selected_memory_type = TVCMemoryType.Slot0;
					m_slot_selected_memory = m_slot_memory[0];
					break;

				case 1:
          m_slot_selected_memory_type = TVCMemoryType.Slot1;
          m_slot_selected_memory = m_slot_memory[1];
          break;

				case 2:
          m_slot_selected_memory_type = TVCMemoryType.Slot2;
          m_slot_selected_memory = m_slot_memory[2];
          break;

				case 3:
          m_slot_selected_memory_type = TVCMemoryType.Slot3;
          m_slot_selected_memory = m_slot_memory[3];
          break;
			}
		}

		// PORT 0FH
		// ========
		//
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		// |              V I D E O M E M  S E L E C T                     |
		// +---7---+---6---+---5---+---4---+---3---+---2---+---1---+---0---+
		// |   -   |   -   |      CRT      |     Page 2    |     Page 1    |
		// |       |       |  00: VID0     |  00: VID0     |  00: VID0     |
		// |       |       |  01: VID1     |  01: VID1     |  01: VID1     |
		// |       |       |  10: VID2     |  10: VID2     |  10: VID2     |
		// |       |       |  11: VID3     |  11: VID3     |  11: VID3     |
		// +-------+-------+-------+-------+-------+-------+-------+-------+
		public void PortWrite0FH(ushort in_address, byte in_data)
		{
			m_port0f = in_data;

			if (m_settings.HardwareVersion == 3) // Only 64k+
			{
				VisibleVideoMem = m_mem_video[(in_data >> 4) & 0x03];
				m_page1_video_mem = m_mem_video[(in_data) & 0x03];
				m_page2_video_mem = m_mem_video[(in_data >> 2) & 0x03];
			}
			else
			{
				VisibleVideoMem = m_mem_video[0];
				m_page1_video_mem = m_mem_video[0];
				m_page2_video_mem = m_mem_video[0];
			}
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte MemU0Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page0StartAddress];
		}

		/// <summary>
		/// Page 0 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MemU0Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page0StartAddress] = in_data;
		}

		/// <summary>
		/// Page 1 RAM read
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte MemU1Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page1StartAddress];
		}

		/// <summary>
		/// Page 1 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MemU1Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page1StartAddress] = in_data;
		}

		/// <summary>
		/// Page 2 RAM read
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte MemU2Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page2StartAddress];
		}

		/// <summary>
		/// Pagfe 2 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MemU2Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page2StartAddress] = in_data;
		}

		/// <summary>
		/// Page 2 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte MemU3Read(ushort in_page_address)
		{
			return m_mem_user[in_page_address + Page3StartAddress];
		}

		/// <summary>
		/// Page 3 RAM write
		/// </summary>
		/// <param name="in_page_address"></param>
		/// <param name="in_data"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MemU3Write(ushort in_page_address, byte in_data)
		{
			m_mem_user[in_page_address + Page3StartAddress] = in_data;
		}

		/// <summary>
		/// Video memory page 1 read
		/// </summary>
		/// <param name="in_address"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte MemVideo1Read(ushort in_address)
		{
			VideoMemAccessCount++;
			return m_page1_video_mem[in_address];
		}

		/// <summary>
		/// Video memory page 1 write
		/// </summary>
		/// <param name="in_address"></param>
		/// <param name="in_data"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MemVideo1Write(ushort in_address, byte in_data)
		{
			VideoMemAccessCount++;
			m_page1_video_mem[in_address] = in_data;
		}

		/// <summary>
		/// Video memory page 2 read
		/// </summary>
		/// <param name="in_address"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte MemVideo2Read(ushort in_address)
		{
			VideoMemAccessCount++;
			return m_page2_video_mem[in_address];
		}

		/// <summary>
		/// Video memory page 2 write
		/// </summary>
		/// <param name="in_address"></param>
		/// <param name="in_data"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MemVideo2Write(ushort in_address, byte in_data)
		{
			VideoMemAccessCount++;
			m_page2_video_mem[in_address] = in_data;
		}

		private byte MemExtRead(ushort in_page_address)
		{
			if ((in_page_address & 0x2000) == 0)
			{
				// read IO mem
				if (m_tvc.Cards[m_ext_mem_select] != null)
					return m_tvc.Cards[m_ext_mem_select].MemoryRead(in_page_address);

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
					m_tvc.Cards[m_ext_mem_select].MemoryWrite(in_page_address, in_data);
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
		private static readonly string[] m_page0_mapping_names_no_u3_paging = { "SYS", "CART", "U0", "U0" };
		private static readonly string[] m_page1_mapping_names = { "U1", "VID" };
		private static readonly string[] m_page2_mapping_names = { "VID", "U2" };
		private static readonly string[] m_page3_mapping_names = { "CART", "SYS", "U3", "EXT" };

		/// <summary>
		/// Gets displayable memory names
		/// </summary>
		/// <param name="in_page_index">Page index [0..3]</param>
		/// <returns>Currently selected page name</returns>
		public string GetPageMappingNameString(int in_page_index)
		{
			switch (in_page_index)
			{
				case 0:
					if (m_settings.HardwareVersion == SetupTVCConfigurationDataProvider.TVCHardwareVersion64k)
						return m_page0_mapping_names_no_u3_paging[GetPage0Mapping()];
					else
						return m_page0_mapping_names[GetPage0Mapping()];

				case 1:
          if (m_settings.HardwareVersion == SetupTVCConfigurationDataProvider.TVCHardwareVersion64kplus)
            return m_page1_mapping_names[GetPage1Mapping()];
					else
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

		public void SetU2UserMemoryHandler(TVCMemoryReadDelegate in_reader, TVCMemoryWriteDelegate in_writer)
		{
			if (in_reader == null)
			{
				if (m_settings.HardwareVersion == SetupTVCConfigurationDataProvider.TVCHardwareVersion32k)
				{
					m_u2_reader = MemNoRead;
					m_u2_writer = MemNoWrite;
				}
				else
				{
					m_u2_reader = MemU2Read;
					m_u2_writer = MemU2Write;
				}
			}
			else
			{
				m_u2_reader = in_reader;
				m_u2_writer = in_writer;
			}
		}

		public void SetU3UserMemoryHandler(TVCMemoryReadDelegate in_reader, TVCMemoryWriteDelegate in_writer)
		{
			if (in_reader == null || in_writer == null)
			{
				if (m_settings.HardwareVersion == SetupTVCConfigurationDataProvider.TVCHardwareVersion32k)
				{
					m_u3_reader = MemNoRead;
					m_u3_writer = MemNoWrite;
				}
				else
				{
					m_u3_reader = MemU3Read;
					m_u3_writer = MemU3Write;
				}
			}
			else
			{
				m_u3_reader = in_reader;
				m_u3_writer = in_writer;
			}
		}
	}
}
