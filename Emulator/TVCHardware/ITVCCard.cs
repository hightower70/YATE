namespace TVCHardware
{
	public interface ITVCCard
	{
		void Reset();

		// memory read/write
		byte CardMemoryRead(ushort in_address);
		void CardMemoryWrite(ushort in_address, byte in_byte);

		// port read/write
		void CardPortRead(ushort in_address, ref byte inout_data);
		void CardPortWrite(ushort in_address, byte in_byte);

		byte GetCardID();
	}
}
