namespace Z80CPU
{
	/// <summary>
	/// Interface for memory access of the Z80 CPU
	/// </summary>
	public interface IZ80Memory
	{
		void SetCPU(Z80 in_cpu);

		byte Read(ushort in_address, bool in_m1_state = false);
		void Write(ushort in_address, byte in_value);
	}
}
