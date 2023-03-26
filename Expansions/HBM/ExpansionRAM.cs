using YATECommon;

namespace HBM
{
  internal class ExpansionRAM : IDebuggableMemory
  {
    public const int PageSize = 16 * 1024;
    public const int RAMSize = 32 * 1024;

    private IDebuggableMemory m_original_memory;
       
    private byte[] m_memory_content;

    public ExpansionRAM()
    {
      m_memory_content = new byte[RAMSize];
    }

    public void RegisterDebugMemory(ITVComputer in_computer)
    {
      // get original memory manager
      m_original_memory = TVCManagers.Default.DebugManager.GetDebuggableMemory(TVCMemoryType.RAM);

      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(this);

      in_computer.Memory.SetU2UserMemoryHandler(U2MemoryRead, U2MemoryWrite);
      in_computer.Memory.SetU3UserMemoryHandler(U3MemoryRead, U3MemoryWrite);
    }

    public void UnregisterDebugMemory(ITVComputer in_computer)
    {
      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(m_original_memory);

      in_computer.Memory.SetU2UserMemoryHandler(null, null);
      in_computer.Memory.SetU3UserMemoryHandler(null, null);
    }
    public byte U2MemoryRead(ushort in_address)
    {
      return m_memory_content[in_address];
    }

    public void U2MemoryWrite(ushort in_address, byte in_data)
    {
      m_memory_content[in_address] = in_data;
    }

    public byte U3MemoryRead(ushort in_address)
    {
      return m_memory_content[in_address + PageSize];
    }

    public void U3MemoryWrite(ushort in_address, byte in_data)
    {
      m_memory_content[in_address + PageSize] = in_data;
    }

    public TVCMemoryType MemoryType { get => TVCMemoryType.RAM; }

    public int AddressOffset { get { return 0; } }

    public int MemorySize { get { return 64*1024; } }

    public int PageCount { get { return 1; } }

    public int PageIndex { get { return 0; } }

    public string[] PageNames
    {
      get { return null; }
    }

    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      if (in_address >= 0x8000)
        return m_memory_content[in_address - 0x8000];
      else
        return m_original_memory.DebugReadMemory(in_page_index, in_address);
    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      if (in_address >= 0x8000)
        m_memory_content[in_address - 0x8000] = in_data;
      else
        m_original_memory.DebugWriteMemory(in_page_index, in_address, in_data);
    }
  }
}
