namespace YATECommon
{
	public interface ITVComputer
	{
    // Timing functions
    int CPUClock { get; }
		ulong GetCPUTicks();
		ulong MicrosecToCPUTicks(int in_us);
		ulong GetTicksSince(ulong in_start_ticks);

		// cartridge handling functions
		void InsertCartridge(ITVCCartridge in_cartridge);
		void RemoveCartridge();

    // card handling function
    void InsertCard(int in_slot_index, ITVCCard in_card);
    void RemoveCard(int in_slot_index);
  }
}
