using TVCEmu.Models.TVCHardware;
using TVCEmuCommon;
using Z80CPU;

namespace TVCHardware
{
	public class TVComputer : ITVComputer
	{
		public const int ExpansionCardCount = 4;
		public const int ExpansionCardPortRange = 16;

		public int CPUClock
    {
      get
      {
        return 3125000;
      }
    }

		public Z80 CPU { get; private set; }
		public TVCPorts Ports { get; private set; }
		public TVCMemory Memory { get; private set; }
		public TVCVideo Video { get; private set; }
		public TVCKeyboard Keyboard { get; private set; }
		public TVCInterrupt Interrupt { get; private set; }
		public ITVCCard[] Cards { get; private set; }
		public ITVCCartridge Cartridge { get; private set; }


		public void Initialize()
		{
			Ports = new TVCPorts();
			Memory = new TVCMemory(this);
			Memory.LoadSystemMemory(@"..\..\roms\rom.bin");
			Memory.LoadExtMemory(@"..\..\roms\ext.bin");
			//Memory.LoadCartMemory(@"d:\Stuff\tvc\szanko.rom");
			//Memory.LoadCartMemory(@"c:\Temp\tvc\mralex.rom");
			//Memory.LoadCartMemory(@"c:\Temp\tvc\invaders.rom");
			//Memory.LoadCartMemory(@"c:\Temp\tvc\vili.rom");
			//Memory.LoadCartMemory(@"c:\Users\laszlo.arvai\Downloads\cfcart128.crt");

			Video = new TVCVideo(this);
			Keyboard = new TVCKeyboard(this);
			Interrupt = new TVCInterrupt(this);

			CPU = new Z80(Memory, Ports, null, true);

			Cards = new ITVCCard[ExpansionCardCount];

			//InsertCard(0, new HBF.HBFCard());

			Ports.AddPortReader(0x5a, PortRead5AH);

			// cartridge init
			Cartridge = new TVCCartridge();
			Cartridge.Initialize(this);
			//Cartridge = new TVCMultiCart();
			//Cartridge.Initialize(this);

			Reset();
		}

		/// <summary>
		/// Resets computer
		/// </summary>
		public void Reset()
		{
			CPU.Reset();
			Video.Reset();
			Ports.Reset();

			// reset cards
			for (int i = 0; i < ExpansionCardCount; i++)
			{
				if (Cards[i] != null)
					Cards[i].CardReset();
			}

      // reset cartridge
      Cartridge?.Reset();
		}

		/// <summary>
		/// Resets computer (Cold reset)
		/// </summary>
		public void ColdReset()
		{
			// clear memory
			Memory.ClearMemory();

			Reset();
		}

		#region · Expansion Card handling ·

		/// <summary>
		/// Inserts expansion cart to the one of the TV Computer expansion slot
		/// </summary>
		/// <param name="in_slot_index">Index of the exansion slot</param>
		/// <param name="in_card">Expansion card to object to insert</param>
		public void InsertCard(int in_slot_index, ITVCCard in_card)
		{
			// remove card (if installed)
			if (Cards[in_slot_index] != null)
				RemoveCard(in_slot_index);

			// store card
			Cards[in_slot_index] = in_card;

			// set parent
			Cards[in_slot_index].Initialize(this);

			// set io callbacks
			ushort port_address = GetCardIOAddress(in_slot_index);

			// subscribe to port read-write event 
			for (int port_count = 0; port_count < ExpansionCardPortRange; port_count++)
			{
				Ports.AddPortReader(port_address, in_card.CardPortRead);
				Ports.AddPortWriter(port_address, in_card.CardPortWrite);

				port_address++;
			}
		}

		/// <summary>
		/// Removes expansion card from the TV Computer expansion slot
		/// </summary>
		/// <param name="in_slot_index">Expansion slot index for removing card</param>
		public void RemoveCard(int in_slot_index)
		{
			if (Cards[in_slot_index] != null)
			{
				// remove io callbacks
				ushort port_address = GetCardIOAddress(in_slot_index);

				for (int port_count = 0; port_count < ExpansionCardPortRange; port_count++)
				{
					Ports.RemovePortReader(port_address, Cards[in_slot_index].CardPortRead);
					Ports.RemovePortWriter(port_address, Cards[in_slot_index].CardPortWrite);

					port_address++;
				}
			}

			// remove card
			Cards[in_slot_index] = null;
		}

		/// <summary>
		/// Calsulatess expansion card start port address from the card index
		/// </summary>
		/// <param name="in_slot_index">Card index</param>
		/// <returns>First I/O port address of the given card</returns>
		private ushort GetCardIOAddress(int in_slot_index)
		{
			return (ushort)((in_slot_index + 1) * 0x10);
		}

		/// <summary>
		/// Reds expansion card ID register
		/// </summary>
		/// <param name="in_address">not used</param>
		/// <param name="inout_data">Data to read from the card</param>
		private void PortRead5AH(ushort in_address, ref byte inout_data)
		{
			byte data = 0;

			for (int i = 0; i < ExpansionCardCount; i++)
			{
				if (Cards[i] != null)
				{
					data |= (byte)(Cards[i].CardGetID() << (i * 2));
				}
				else
				{
					data |= (byte)(0x03 << (i * 2));
				}
			}

			inout_data = data;
		}

		#endregion																								 

		public void PeriodicCallback()
		{
			for (int i = 0; i < ExpansionCardCount; i++)
			{
				if (Cards[i] != null)
					Cards[i].CardPeriodicCallback(CPU.TotalTState);
			}
		}

		/// <summary>
		/// Converts milliseconds to CPU clock cycle count
		/// </summary>
		/// <param name="in_ms">Millisec to convert to CPU ticks</param>
		/// <returns>CPU clock cycle count</returns>
		public ulong MillisecToCPUTicks(int in_ms)
		{
			return (ulong)in_ms * (ulong)CPUClock / 1000ul;
		}

		/// <summary>
		/// Converts microseconds to CPU clock count
		/// </summary>
		/// <param name="in_us"></param>
		/// <returns></returns>
		public ulong MicrosecToCPUTicks(int in_us)
		{
			return (ulong)in_us * (ulong)CPUClock / 1000000ul;
		}

		/// <summary>
		/// Converts CPU tick into millisec including fractional millisec
		/// </summary>
		/// <param name="in_cpu_tick">CPU ticks</param>
		/// <returns>Time in millisec</returns>
		public double CPUTickToMillisec(long in_cpu_tick)
		{
			return (double)in_cpu_tick / CPUClock * 1000.0;
		}

		public ulong GetCPUTicks()
		{
			return CPU.TotalTState;
		}

		public ulong GetTicksSince(ulong in_start_ticks)
		{
			return CPU.TotalTState - in_start_ticks;
		}

		#region · Cartridge handling routines ·

		/// <summary>
		/// Inserts cartridge
		/// </summary>
		/// <param name="in_cartridge"></param>
		public void InsertCartridge(ITVCCartridge in_cartridge)
		{
		}

		/// <summary>
		/// Removes cartridge
		/// </summary>
		public void RemoveCartridge()
		{
		}

		#endregion
	}
}
