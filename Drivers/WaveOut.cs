///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2021 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// Wave out device handler class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace YATE.Drivers
{
  /// <summary>
  /// Waveout device handler class
  /// </summary>
  public class WaveOut : IDisposable
  {
    #region · Types ·

    /// <summary>
    /// Wave out buffer class
    /// </summary>
    class WaveOutBuffer : IDisposable
    {
      public bool Free;
      public short[] Data;
      public WaveNative.WaveHdr Header;

      private GCHandle m_header_handle;
      private GCHandle m_data_handle;
      private GCHandle m_this;
      internal int BufferIndex { get; }
      internal WaveOut Parent { get; private set; }

      /// <summary>
      /// Waveout buffer default constructor
      /// </summary>
      /// <param name="in_parent">Parent Waveout class</param>
      /// <param name="in_buffer_index">NUmber of the wave buffer</param>
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

      /// <summary>
      /// Clears data buffer
      /// </summary>
      public void ClearBuffer()
      {
        for (int i = 0; i < Data.Length; i++)
        {
          Data[i] = 0;
        }
      }

      /// <summary>
      /// Enqueues buffer for playback
      /// </summary>
      public void Enqueue()
      {
        Free = false;

        // write header
        WaveNative.Try(WaveNative.waveOutWrite(Parent.Handle, m_header_handle.AddrOfPinnedObject(), Marshal.SizeOf(Header)));
      }

      /// <summary>
      /// Dequeues buffer after playing it
      /// </summary>
      public void Dequeue()
      {
        Free = true;
        Header.dwFlags &= ~WaveNative.WaveHdrFlags.WHDR_DONE;
      }

      /// <summary>
      /// Destructor
      /// </summary>
      ~WaveOutBuffer()
      {
        Dispose();
      }

      /// <summary>
      /// Dispose of non managed resources
      /// </summary>
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

    #endregion

    #region · Data members ·

    // buffers
    private WaveOutBuffer[] m_wave_out_buffers;

    // Class configuration
    private int m_sample_rate;
    private int m_device_id;

    // Wave settings
    private WaveNative.WaveFormat m_wave_out_format;
    private IntPtr m_wave_out_device_handle;

    // thread variables
    private Thread m_thread;
    private AutoResetEvent m_thread_event;
    private bool m_thread_running;
    private WaveOutBuffer m_finished_buffer;

    private readonly WaveNative.WaveOutDelegate m_buffer_proc = new WaveNative.WaveOutDelegate(WaveOutProc);

    #endregion

    #region · Properties ·

    /// <summary>
    /// Number of buffers
    /// </summary>
    public int BufferCount { get; private set; }

    /// <summary>
    /// Number of samples in one buffer
    /// </summary>
    public int BufferSampleCount { get; private set; }

    /// <summary>
    /// WaveOut device handle
    /// </summary>
    public IntPtr Handle
    {
      get { return m_wave_out_device_handle; }
    }

    #endregion

    #region · Public members ·

    /// <summary>
    /// Open wave out device for playback
    /// </summary>
    /// <param name="in_device_id">Device ID to use for playback</param>
    /// <param name="in_sample_rate">Sample rate</param>
    /// <param name="in_buffer_count">Number of buffers to use</param>
    /// <param name="in_buffer_sample_count">Length of one buffer in samples</param>
    public void Open(int in_device_id, int in_sample_rate, int in_buffer_count, int in_buffer_sample_count)
    {
      // init class
      m_wave_out_device_handle = IntPtr.Zero;

      // store class settings
      m_device_id = in_device_id;
      m_sample_rate = in_sample_rate;
      BufferCount = in_buffer_count;
      BufferSampleCount = in_buffer_sample_count;
    }

    /// <summary>
    /// Starts wave out device
    /// </summary>
    /// <returns></returns>
    public bool Start()
    {
      WaveNative.MMRESULT error;

      m_finished_buffer = null;

      // create thread event
      m_thread_event = new AutoResetEvent(false);

      m_wave_out_format = new WaveNative.WaveFormat(m_sample_rate, 16, 2);

      // open wave device
      error = WaveNative.waveOutOpen(out m_wave_out_device_handle, m_device_id, m_wave_out_format, m_buffer_proc, 0, (uint)WaveNative.WaveInOutOpenFlags.CALLBACK_FUNCTION);
      if (error == WaveNative.MMRESULT.MMSYSERR_NOERROR)
      {
        // start thread
        m_thread_running = true;
        m_thread = new Thread(new ThreadStart(ThreadProc));
        m_thread.Priority = ThreadPriority.AboveNormal;
        m_thread.Start();
      }
      else
      {
        m_thread_event.Dispose();
        m_thread_event = null;
        m_wave_out_format = null;
        m_thread = null;
        m_thread_running = false;
      }

      return error == WaveNative.MMRESULT.MMSYSERR_NOERROR;
    }

    /// <summary>
    /// Stops waveout playback
    /// </summary>
    public void Stop()
    {
      if (m_wave_out_device_handle != IntPtr.Zero)
      {
        m_thread_running = false;
        m_thread_event.Set();

        m_thread.Join();
      }
    }

    #endregion

    #region · Private members ·

    /// <summary>
    /// Waveout thread procedure
    /// </summary>
    private void ThreadProc()
    {
      WaveOutBuffer buffer;

      // create buffers
      m_wave_out_buffers = new WaveOutBuffer[BufferCount];
      for (int i = 0; i < BufferCount; i++)
      {
        m_wave_out_buffers[i] = new WaveOutBuffer(this, i);
        m_wave_out_buffers[i].ClearBuffer();
      }

      // queue buffers
      for (int i = 0; i < BufferCount; i++)
      {
        m_wave_out_buffers[i].Enqueue();
      }

      // thread loop
      while (m_thread_running)
      {
        m_thread_event.WaitOne();
        buffer = m_finished_buffer;
        m_finished_buffer = null;

        if (buffer != null)
        {
          buffer.Dequeue();
          buffer.Enqueue();
        }

        // find finished buffer and dequeue
        for (int i = 0; i < BufferCount; i++)
        {
          if (m_wave_out_buffers[i].Free)
          {
            if (m_thread_running)
            {
              //m_wave_out_buffers[i].Enqueue();
            }
            break;
          }
        }
      }

      WaveNative.waveOutReset(m_wave_out_device_handle);

      for (int i = 0; i < BufferCount; i++)
        m_wave_out_buffers[i].Dispose();

      WaveNative.waveOutClose(m_wave_out_device_handle);

      m_wave_out_buffers = null;

      m_wave_out_device_handle = IntPtr.Zero;
      m_thread = null;
      m_thread_event.Dispose();
      m_thread_event = null;
    }

    /// <summary>
    /// Buffer finished callback. Called by static callback.
    /// </summary>
    /// <param name="in_buffer"></param>
    private void BufferFinished(WaveOutBuffer in_buffer)
    {
      Interlocked.CompareExchange(ref m_finished_buffer, in_buffer, null);
      
      m_thread_event.Set();
    }

    /// <summary>
    /// Static callback function. Called by windows.
    /// </summary>
    /// <param name="hdrvr"></param>
    /// <param name="uMsg"></param>
    /// <param name="dwUser"></param>
    /// <param name="wavhdr"></param>
    /// <param name="dwParam2"></param>
    internal static void WaveOutProc(IntPtr hdrvr, WaveNative.WaveMessage uMsg, int dwUser, WaveNative.WaveHdr wavhdr, int dwParam2)
    {
      try
      {
        if (uMsg == WaveNative.WaveMessage.WOM_DONE)
        {
          //Debug.WriteLine(uMsg.ToString() + " " + wavhdr.dwUser.ToString());

          GCHandle hBuffer = (GCHandle)wavhdr.dwUser;
          WaveOutBuffer buffer = (WaveOutBuffer)hBuffer.Target;

          buffer.Free = true;
          buffer.Parent.BufferFinished(buffer);
        }
      }
      catch
      {

      }
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~WaveOut()
    {
      Dispose();
    }

    /// <summary>
    /// Dispose. Cleans up all non managed resources.
    /// </summary>
    public void Dispose()
    {
      Stop();

      if (m_wave_out_buffers != null)
      {
        for (int i = 0; i < m_wave_out_buffers.Length; i++)
        {
          m_wave_out_buffers[i].Dispose();
          m_wave_out_buffers[i] = null;
        }

        m_wave_out_buffers = null;
      }

      GC.SuppressFinalize(this);
    }
    #endregion

    #region · Wave out device enumeration ·

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

      if (string.IsNullOrEmpty(device.ModuleName))
      {
        devices[0].DisplayName = device.Description;
        devices[0].Guid = device.Guid;
      }
      else
      {
        for (int i = 1; i < devices.Length; i++)
        {
          if (devices[i].DisplayName != device.Description && device.Description.StartsWith(devices[i].DisplayName))
          {
            devices[i].DisplayName = device.Description;
            devices[i].Guid = device.Guid;
            break;
          }
        }
      }
      return true;
    }

    public static WaveOutDeviceInfo[] GetWaveOutDevices()
    {
      // enumerate WaveOut devices
      int device_count = WaveNative.waveOutGetNumDevs();
      WaveOutDeviceInfo[] result = new WaveOutDeviceInfo[device_count + 1];
      WaveNative.WAVEOUTCAPS caps = new WaveNative.WAVEOUTCAPS();

      result[0] = new WaveOutDeviceInfo();

      for (int i = 0; i < device_count; i++)
      {
        WaveNative.waveOutGetDevCaps((IntPtr)i, ref caps, (uint)Marshal.SizeOf(caps));
        result[i + 1] = new WaveOutDeviceInfo();
        result[i + 1].DisplayName = caps.szPname;
      }

      // expand names
      GCHandle result_handle = GCHandle.Alloc(result);
      WaveNative.DirectSoundEnumerate(new WaveNative.DSEnumCallback(EnumCallback), (IntPtr)result_handle);
      result_handle.Free();

      if (string.IsNullOrEmpty(result[0].DisplayName))
        result[0].DisplayName = "(default)";

      return result;
    }

    #endregion
  }
}
