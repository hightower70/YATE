using YATECommon;

namespace YATE.Emulator.TVCHardware
{
  internal class TVCDebuggableMemory : IDebuggableMemory
  {
    private byte[] m_memory_content;
    private TVCMemoryType m_memory_type;
    private ushort m_memory_offset;

    public TVCDebuggableMemory(byte[] in_memory_content, TVCMemoryType in_type, ushort in_offset)
    {
      m_memory_content = in_memory_content;
      m_memory_type = in_type;
      m_memory_offset = in_offset;
      TVCManagers.Default.DebugManager.RegisterDebuggableMemory(this);
    }

    public TVCMemoryType MemoryType { get { return m_memory_type; } }

    public int AddressOffset { get { return m_memory_offset; } }

    public int MemorySize { get { return m_memory_content.Length; } }

    public int PageCount { get { return 1; } }

    public int PageIndex { get { return 0; } }

    public string[] PageNames
    {
      get { return null; }
    }

    public byte DebugReadMemory(int in_page_index, int in_address)
    {
      return m_memory_content[in_address];
    }

    public void DebugWriteMemory(int in_page_index, int in_address, byte in_data)
    {
      m_memory_content[in_address] = in_data;
    }
  }
}
