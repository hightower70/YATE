using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomControls.HexEdit
{
  public interface IHexEditorDataProvider
  {
    long Length { get; }

    long Position { get; set; }

    byte ReadByte();

    //sbyte ReadSByte();

    //Int16 ReadInt16();

    //Int32 ReadInt32();

    //Int64 ReadInt64();

    //UInt16 ReadUInt16();

    //UInt32 ReadUInt32();
    //UInt64 ReadUInt64();
  }
}
