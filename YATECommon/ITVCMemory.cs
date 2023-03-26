using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YATECommon
{
  public delegate byte TVCMemoryReadDelegate(ushort in_address);
  public delegate void TVCMemoryWriteDelegate(ushort in_address, byte in_data);

  public interface ITVCMemory
  {
    byte[] VisibleVideoMem { get; }


    void SetU2UserMemoryHandler(TVCMemoryReadDelegate in_reader, TVCMemoryWriteDelegate in_writer);
    void SetU3UserMemoryHandler(TVCMemoryReadDelegate in_reader, TVCMemoryWriteDelegate in_writer);
  }
}
