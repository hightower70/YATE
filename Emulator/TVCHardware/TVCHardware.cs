using Z80CPU;

namespace TVCHardware
{
	public class TVComputer
	{
		public Z80 CPU { get; private set; }
		public TVCPorts Ports { get; private set; }
		public TVCMemory Memory { get; private set; }
		public TVCVideo Video { get; private set; }
		public TVCKeyboard Keyboard {get;private set;}
		public TVCInterrupt Interrupt { get; private set; }


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

			Video = new TVCVideo(this);
			Keyboard = new TVCKeyboard(this);
			Interrupt = new TVCInterrupt(this);

			CPU = new Z80(Memory, Ports, null, true);

			Reset();
		}

		public void Reset()
		{
			CPU.Reset();
			Memory.Reset();
			Video.Reset();
		}
	}
}
