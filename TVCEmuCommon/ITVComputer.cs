namespace TVCEmuCommon
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
	}
}
