using Z80CPU;

namespace TVCHardware
{
	public class TVComputer
	{
		public int ExpansionCardCount = 4;
		public int ExpansionCardPortRange = 16;

		public Z80 CPU { get; private set; }
		public TVCPorts Ports { get; private set; }
		public TVCMemory Memory { get; private set; }
		public TVCVideo Video { get; private set; }
		public TVCKeyboard Keyboard {get;private set;}
		public TVCInterrupt Interrupt { get; private set; }
		public ITVCCard[] Cards { get; private set; }


		public void Initialize()
		{
			Ports = new TVCPorts();
			Memory = new TVCMemory(this);
			Memory.LoadSystemMemory(@"..\..\roms\rom.bin");
			Memory.LoadExtMemory(@"..\..\roms\ext.bin");
			Memory.LoadCartMemory(@"d:\Stuff\tvc\szanko.rom");
			//Memory.LoadCartMemory(@"c:\Temp\tvc\mralex.rom");
			//Memory.LoadCartMemory(@"c:\Temp\tvc\invaders.rom");
			//Memory.LoadCartMemory(@"c:\Temp\tvc\vili.rom");

			Video = new TVCVideo(this);
			Keyboard = new TVCKeyboard(this);
			Interrupt = new TVCInterrupt(this);

			CPU = new Z80(Memory, Ports, null, true);

			Cards = new ITVCCard[ExpansionCardCount];

			InsertCard(0, new HBFCard());

			Ports.AddPortReader(0x5a, PortRead5AH);

			Reset();
		}

		public void Reset()
		{
			CPU.Reset();
			Video.Reset();
			Ports.Reset();
		}

		public void InsertCard(int in_slot_index, ITVCCard in_card)
		{
			// remove card (if installed)
			if (Cards[in_slot_index] != null)
				RemoveCard(in_slot_index);

			// store card
			Cards[in_slot_index] = in_card;

			// set io callbacks
			ushort port_address = GetCardIOAddress(in_slot_index);

			for (int port_count = 0; port_count < ExpansionCardPortRange; port_count++)
			{
				Ports.AddPortReader(port_address, in_card.CardPortRead);
				Ports.AddPortWriter(port_address, in_card.CardPortWrite);

				port_address++;
			}
		}

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

		private ushort GetCardIOAddress(int in_slot_index)
		{
			return (ushort)((in_slot_index + 1) * 0x10);
		}

		private void PortRead5AH(ushort in_address, ref byte inout_data)
		{
			byte data = 0;

			for (int i = 0; i < ExpansionCardCount; i++)
			{
				if (Cards[i] != null)
				{
					data |= (byte)(Cards[i].GetCardID() << (i * 2));
				}	
				else
				{
					data |= (byte)(0x03 << (i * 2));
				}
			}

			inout_data = data;
		}
	}
}
