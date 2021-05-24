
using System;                             
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YATE.Drivers
{
  internal class WaveOutBuffer  : IDisposable
  {
    public bool Free;
    public short[] Data;
    public WaveNative.WaveHdr Header;

    private GCHandle m_header_handle;
    private GCHandle m_data_handle;
    private GCHandle m_this;
    internal int BufferIndex { get; }
    internal WaveOut Parent { get; private set; }

    public WaveOutBuffer(WaveOut in_parent, int in_buffer_index)
    {
      Parent = in_parent;

      BufferIndex = in_buffer_index;
      Free = true;
      Header = new WaveNative.WaveHdr();
      Data = new short[in_parent.BufferSampleCount];

      m_this = GCHandle.Alloc(this);
      m_header_handle = GCHandle.Alloc(Header, GCHandleType.Pinned);
      m_data_handle = GCHandle.Alloc(Data, GCHandleType.Pinned);

      Header.lpData = m_data_handle.AddrOfPinnedObject();
      Header.dwBufferLength = sizeof(short) * Parent.BufferSampleCount;
      Header.dwUser = (IntPtr)m_this;

      // prepare header
      WaveNative.Try(WaveNative.waveOutPrepareHeader(Parent.Handle, m_header_handle.AddrOfPinnedObject(), Marshal.SizeOf(Header)));
    }

    public void ClearBuffer()
    {
      for (int i = 0; i < Data.Length / 2; i++)
      {
        Data[i * 2] = Data[i * 2 + 1] = (short)(Math.Sin(Math.PI * 2 * 200 * i / (Data.Length / 2)) * 2048);
      }
    }

    public void Enqueue()
    {
      Free = false;

      // write header
      //Debug.WriteLine("E: " + BufferIndex.ToString());

      WaveNative.Try(WaveNative.waveOutWrite(Parent.Handle, m_header_handle.AddrOfPinnedObject(), Marshal.SizeOf(Header)));
    }

    public void Dequeue()
    {
      Free = true;
      Header.dwFlags &= ~WaveNative.WaveHdrFlags.WHDR_DONE;

      //Debug.WriteLine("D: " + BufferIndex.ToString());
    }

    ~WaveOutBuffer()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (Parent != null)
      {
        // release header
        WaveNative.Try(WaveNative.waveOutUnprepareHeader(Parent.Handle, m_header_handle.AddrOfPinnedObject(), Marshal.SizeOf(Header)));

        Parent = null;
      } 

      if (m_header_handle.IsAllocated)
        m_header_handle.Free();

      if (m_data_handle.IsAllocated)
        m_data_handle.Free();

      if (m_this.IsAllocated)
        m_this.Free();

      Data = null;
    }
  }
}
