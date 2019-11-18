namespace TVCEmuCommon
{
	public interface ITVComputer
	{
		// Timing functions
		ulong GetCPUTicks();
		ulong MicrosecToCPUTicks(int in_us);
		ulong GetTicksSince(ulong in_start_ticks);

	}
}
