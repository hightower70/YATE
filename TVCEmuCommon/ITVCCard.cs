namespace TVCEmuCommon
{
	public interface ITVCCard
	{
		// memory read/write
		byte MemoryRead(ushort in_address);
		void MemoryWrite(ushort in_address, byte in_byte);

		// port read/write
		void PortRead(ushort in_address, ref byte inout_data);
		void PortWrite(ushort in_address, byte in_byte);

    // emulation functions
		void PeriodicCallback(ulong in_cpu_tick);
    void Reset();
    byte GetID();

    // card management
    //int SlotIndex { get; }
    void Install(ITVComputer in_parent);
    void Remove();
  }
}
