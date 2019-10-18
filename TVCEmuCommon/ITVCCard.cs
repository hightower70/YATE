namespace TVCEmuCommon
{
	public interface ITVCCard
	{
		void CardReset();

		// memory read/write
		byte CardMemoryRead(ushort in_address);
		void CardMemoryWrite(ushort in_address, byte in_byte);

		// port read/write
		void CardPortRead(ushort in_address, ref byte inout_data);
		void CardPortWrite(ushort in_address, byte in_byte);

		byte CardGetID();

		void CardPeriodicCallback(ulong in_cpu_tick);

		void Initialize(ITVComputer in_parent);
	}
}
