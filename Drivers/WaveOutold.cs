﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using YATE.Drivers;

namespace YATE.Driversold
{
  internal class WaveOutHelper
  {
    public static void Try(WaveNative.MMRESULT err)
    {
      if (err != WaveNative.MMRESULT.MMSYSERR_NOERROR)
        throw new Exception(err.ToString());
    }
  }

  public delegate void BufferFillEventHandler(IntPtr data, int size);

  internal class WaveOutBuffer : IDisposable
  {
    public WaveOutBuffer NextBuffer;

    private AutoResetEvent m_PlayEvent = new AutoResetEvent(false);
    private IntPtr m_WaveOut;

    private WaveNative.WaveHdr m_Header;
    private byte[] m_HeaderData;
    private GCHandle m_HeaderHandle;
    private GCHandle m_HeaderDataHandle;

    private bool m_Playing;

    internal static void WaveOutProc(IntPtr hdrvr, WaveNative.WaveOutMsg uMsg, int dwUser, ref WaveNative.WaveHdr wavhdr, int dwParam2)
    {
      if (uMsg == WaveNative.WaveOutMsg.MM_WOM_DONE)
      {
        try
        {
          GCHandle h = (GCHandle)wavhdr.dwUser;
          WaveOutBuffer buf = (WaveOutBuffer)h.Target;
          buf.OnCompleted();
        }
        catch
        {
        }
      }
    }

    public WaveOutBuffer(IntPtr waveOutHandle, int size)
    {
      m_WaveOut = waveOutHandle;

      m_HeaderHandle = GCHandle.Alloc(m_Header, GCHandleType.Pinned);
      m_Header.dwUser = (IntPtr)GCHandle.Alloc(this);
      m_HeaderData = new byte[size];
      m_HeaderDataHandle = GCHandle.Alloc(m_HeaderData, GCHandleType.Pinned);
      m_Header.lpData = m_HeaderDataHandle.AddrOfPinnedObject();
      m_Header.dwBufferLength = size;
      //WaveOutHelper.Try(WaveNative.waveOutPrepareHeader(m_WaveOut, ref m_Header, Marshal.SizeOf(m_Header)));
    }

    ~WaveOutBuffer()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (m_Header.lpData != IntPtr.Zero)
      {
        WaveNative.waveOutUnprepareHeader(m_WaveOut, ref m_Header, Marshal.SizeOf(m_Header));
        m_HeaderHandle.Free();
        m_Header.lpData = IntPtr.Zero;
      }
      m_PlayEvent.Close();
      if (m_HeaderDataHandle.IsAllocated)
        m_HeaderDataHandle.Free();
      GC.SuppressFinalize(this);
    }

    public int Size
    {
      get { return m_Header.dwBufferLength; }
    }

    public IntPtr Data
    {
      get { return m_Header.lpData; }
    }

    public bool Play()
    {
      lock (this)
      {
        m_PlayEvent.Reset();
        m_Playing = WaveNative.waveOutWrite(m_WaveOut, ref m_Header, Marshal.SizeOf(m_Header)) == WaveNative.MMRESULT.MMSYSERR_NOERROR;
        return m_Playing;
      }
    }
    public void WaitFor()
    {
      if (m_Playing)
      {
        m_Playing = m_PlayEvent.WaitOne();
      }
      else
      {
        Thread.Sleep(0);
      }
    }
    public void OnCompleted()
    {
      m_PlayEvent.Set();
      m_Playing = false;
    }
  }

  public class WaveOutold : IDisposable
  {
    private IntPtr m_WaveOut;
    private WaveOutBuffer m_Buffers; // linked list
    private WaveOutBuffer m_CurrentBuffer;
    private Thread m_Thread;
    private BufferFillEventHandler m_FillProc;
    private bool m_Finished;
    private byte m_zero;

    private WaveNative.WaveOutDelegate m_BufferProc = new WaveNative.WaveOutDelegate(WaveOutBuffer.WaveOutProc);

    public static int DeviceCount
    {
      get { return WaveNative.waveOutGetNumDevs(); }
    }

    public WaveOutold(int device, WaveFormat format, int bufferSize, int bufferCount, BufferFillEventHandler fillProc)
    {
      m_zero = format.wBitsPerSample == 8 ? (byte)128 : (byte)0;
      m_FillProc = fillProc;
      //WaveOutHelper.Try(WaveNative.waveOutOpen(out m_WaveOut, device, format, m_BufferProc, 0, WaveNative.CALLBACK_FUNCTION));
      AllocateBuffers(bufferSize, bufferCount);
      m_Thread = new Thread(new ThreadStart(ThreadProc));
      m_Thread.Start();
    }

    ~WaveOutold()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (m_Thread != null)
        try
        {
          m_Finished = true;
          if (m_WaveOut != IntPtr.Zero)
            WaveNative.waveOutReset(m_WaveOut);
          m_Thread.Join();
          m_FillProc = null;
          FreeBuffers();
          if (m_WaveOut != IntPtr.Zero)
            WaveNative.waveOutClose(m_WaveOut);
        }
        finally
        {
          m_Thread = null;
          m_WaveOut = IntPtr.Zero;
        }
      GC.SuppressFinalize(this);
    }

    private void ThreadProc()
    {
      while (!m_Finished)
      {
        Advance();
        if (m_FillProc != null && !m_Finished)
          m_FillProc(m_CurrentBuffer.Data, m_CurrentBuffer.Size);
        else
        {
          // zero out buffer
          byte v = m_zero;
          byte[] b = new byte[m_CurrentBuffer.Size];
          for (int i = 0; i < b.Length; i++)
            b[i] = v;
          Marshal.Copy(b, 0, m_CurrentBuffer.Data, b.Length);

        }
        m_CurrentBuffer.Play();
      }
      WaitForAllBuffers();
    }
    private void AllocateBuffers(int bufferSize, int bufferCount)
    {
      FreeBuffers();
      if (bufferCount > 0)
      {
        m_Buffers = new WaveOutBuffer(m_WaveOut, bufferSize);
        WaveOutBuffer Prev = m_Buffers;
        try
        {
          for (int i = 1; i < bufferCount; i++)
          {
            WaveOutBuffer Buf = new WaveOutBuffer(m_WaveOut, bufferSize);
            Prev.NextBuffer = Buf;
            Prev = Buf;
          }
        }
        finally
        {
          Prev.NextBuffer = m_Buffers;
        }
      }
    }

    private void FreeBuffers()
    {
      m_CurrentBuffer = null;
      if (m_Buffers != null)
      {
        WaveOutBuffer First = m_Buffers;
        m_Buffers = null;

        WaveOutBuffer Current = First;
        do
        {
          WaveOutBuffer Next = Current.NextBuffer;
          Current.Dispose();
          Current = Next;
        } while (Current != First);
      }
    }

    private void Advance()
    {
      m_CurrentBuffer = m_CurrentBuffer == null ? m_Buffers : m_CurrentBuffer.NextBuffer;
      m_CurrentBuffer.WaitFor();
    }

    private void WaitForAllBuffers()
    {
      WaveOutBuffer Buf = m_Buffers;
      while (Buf.NextBuffer != m_Buffers)
      {
        Buf.WaitFor();
        Buf = Buf.NextBuffer;
      }
    }

    public class WaveOutDeviceInfo
    {
      public string DisplayName { get; set; }
      public Guid Guid { get; set; }
    }

                                 
    private static bool EnumCallback(IntPtr lpGuid, IntPtr lpcstrDescription, IntPtr lpcstrModule, IntPtr lpContext)
    {
      // get directsound device info
      var device = new WaveNative.DirectSoundDeviceInfo();
      if (lpGuid == IntPtr.Zero)
      {
        device.Guid = Guid.Empty;
      }
      else
      {
        byte[] guidBytes = new byte[16];
        Marshal.Copy(lpGuid, guidBytes, 0, 16);
        device.Guid = new Guid(guidBytes);
      }
      device.Description = Marshal.PtrToStringAnsi(lpcstrDescription);
      if (lpcstrModule != null)
      {
        device.ModuleName = Marshal.PtrToStringAnsi(lpcstrModule);
      }

      // replace truncated devices names with full name
      WaveOutDeviceInfo[] devices = ((GCHandle)lpContext).Target as WaveOutDeviceInfo[];

      for (int i=0;i<devices.Length;i++)
      {
        if (devices[i].DisplayName != device.Description && device.Description.StartsWith(devices[i].DisplayName))
        {
          devices[i].DisplayName = device.Description;
          devices[i].Guid = device.Guid;
          break;
        }
      }

      return true;
    }

                                   
    public static WaveOutDeviceInfo[] GetWaveOutDevices()
    {
      // enumerate WaveOut devices
      int devices = WaveNative.waveOutGetNumDevs();
      WaveOutDeviceInfo[] result = new WaveOutDeviceInfo[devices];
      WaveNative.WAVEOUTCAPS caps = new WaveNative.WAVEOUTCAPS();

      for (int i = 0; i < devices; i++)
      {
        WaveNative.waveOutGetDevCaps((IntPtr)i, ref caps, (uint)Marshal.SizeOf(caps));
        result[i] = new WaveOutDeviceInfo();
        result[i].DisplayName = caps.szPname;
      }

      // expand names
      GCHandle result_handle = GCHandle.Alloc(result);
      WaveNative.DirectSoundEnumerate(new WaveNative.DSEnumCallback(EnumCallback), (IntPtr)result_handle);
      result_handle.Free();

      return result;
    }
  }
}
