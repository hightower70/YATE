using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
