namespace YATE.Emulator.Z80CPU
{
	public interface IZ80Port
	{
		void SetCPU(Z80 in_cpu);

		byte Read(ushort addr);
		void Write(ushort addr, byte value);
	}
}
