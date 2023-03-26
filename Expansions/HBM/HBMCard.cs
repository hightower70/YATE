using HBM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YATECommon;

namespace MemoryExpansion32k
{
  internal class HBMCard
  {
    private ExpansionRAM m_ram;

    public void Install(ITVComputer in_computer)
    {
      m_ram = new ExpansionRAM();
      m_ram.RegisterDebugMemory(in_computer);
    }

    public void Remove(ITVComputer in_computer)
    {
      m_ram.UnregisterDebugMemory(in_computer);
    }
  }
}
