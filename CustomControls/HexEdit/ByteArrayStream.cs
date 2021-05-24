using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace CustomControls.HexEdit
{
  public class ByteArrayStream : Stream
  {
    private byte[] m_byte_array;
    private int m_current_pos;

    public ByteArrayStream(byte[] in_byte_array)
    {
      m_byte_array = in_byte_array;
      m_current_pos = 0;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length
    {
      get
      {
        return m_byte_array.Length;
      }
    }

    public override long Position
    {
      get
      {
        return m_current_pos;
      }
      set
      {
        m_current_pos = (int)(value);
      }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");
      if (count < 0)
        throw new ArgumentOutOfRangeException("count");
      if (buffer.Length < count)
        throw new ArgumentException("count");

      int bytes_to_copy = count;
      if (m_current_pos + bytes_to_copy >= m_byte_array.Length)
        bytes_to_copy = (int)(m_byte_array.Length - m_current_pos);

      Buffer.BlockCopy(m_byte_array, m_current_pos, buffer, offset, bytes_to_copy);

      m_current_pos += bytes_to_copy;

      return bytes_to_copy;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      switch(origin)
      {
        case SeekOrigin.Begin:
          m_current_pos = (int)offset;
          break;

        case SeekOrigin.Current:
          m_current_pos += (int)offset;
          break;

        case SeekOrigin.End:
          m_current_pos = (int)(m_byte_array.Length + offset);
          break;
      }

      return m_current_pos;
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotImplementedException();
    }
  }
}
