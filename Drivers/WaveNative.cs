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
// Wave native functions and type declarations
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Runtime.InteropServices;

namespace YATE.Drivers
{
  internal class WaveNative
  {
    #region · Constants ·
    public const int WAVE_MAPPER = -1; // Wave mapper device id
    private const short WAVE_FORMAT_PCM = 1; // PCM wave format type
    #endregion

    #region · Types ·

    /// <summary>Wave function result codes</summary>
    public enum MMRESULT : uint
    {
      MMSYSERR_NOERROR = 0,
      MMSYSERR_ERROR = 1,
      MMSYSERR_BADDEVICEID = 2,
      MMSYSERR_NOTENABLED = 3,
      MMSYSERR_ALLOCATED = 4,
      MMSYSERR_INVALHANDLE = 5,
      MMSYSERR_NODRIVER = 6,
      MMSYSERR_NOMEM = 7,
      MMSYSERR_NOTSUPPORTED = 8,
      MMSYSERR_BADERRNUM = 9,
      MMSYSERR_INVALFLAG = 10,
      MMSYSERR_INVALPARAM = 11,
      MMSYSERR_HANDLEBUSY = 12,
      MMSYSERR_INVALIDALIAS = 13,
      MMSYSERR_BADDB = 14,
      MMSYSERR_KEYNOTFOUND = 15,
      MMSYSERR_READERROR = 16,
      MMSYSERR_WRITEERROR = 17,
      MMSYSERR_DELETEERROR = 18,
      MMSYSERR_VALNOTFOUND = 19,
      MMSYSERR_NODRIVERCB = 20,
      WAVERR_BADFORMAT = 32,
      WAVERR_STILLPLAYING = 33,
      WAVERR_UNPREPARED = 34
    }

    [StructLayout(LayoutKind.Sequential)]
    public class WaveFormat
    {
      /// <summary>format type</summary>
      public short wFormatTag;
      /// <summary>number of channels</summary>
      public short nChannels;
      /// <summary>sample rate</summary>
      public int nSamplesPerSec;
      /// <summary>for buffer estimation</summary>
      public int nAvgBytesPerSec;
      /// <summary>block size of data</summary>
      public short nBlockAlign;
      /// <summary>number of bits per sample of mono data</summary>
      public short wBitsPerSample;
      /// <summary>number of following bytes</summary>
      public short cbSize;

      public WaveFormat(int rate, int bits, int channels)
      {
        wFormatTag = WAVE_FORMAT_PCM;
        nChannels = (short)channels;
        nSamplesPerSec = rate;
        wBitsPerSample = (short)bits;
        cbSize = 0;

        nBlockAlign = (short)(channels * (bits / 8));
        nAvgBytesPerSec = nSamplesPerSec * nBlockAlign;
      }
    }

    [Flags]
    public enum WaveInOutOpenFlags : uint
    {
      /// <summary>No callback</summary>
      CALLBACK_NULL = 0,
      /// <summary>dwCallback is a FARPROC</summary>
      CALLBACK_FUNCTION = 0x30000,
      /// <summary>dwCallback is an EVENT handle</summary>
      CALLBACK_EVENT = 0x50000,
      /// <summary>dwCallback is a HWND</summary>
      CALLBACK_WINDOW = 0x10000,
      /// <summary>dwCallback is a thread ID</summary>
      CALLBACK_THREAD = 0x20000
    }

    [Flags]
    public enum WaveHdrFlags : uint
    {
      WHDR_DONE = 1,
      WHDR_PREPARED = 2,
      WHDR_BEGINLOOP = 4,
      WHDR_ENDLOOP = 8,
      WHDR_INQUEUE = 16
    }

    public enum WaveMessage
    {
      WIM_OPEN = 0x3BE,
      WIM_CLOSE = 0x3BF,
      WIM_DATA = 0x3C0,

      WOM_CLOSE = 0x3BC,
      WOM_DONE = 0x3BD,
      WOM_OPEN = 0x3BB
    }

    // structs 

    [StructLayout(LayoutKind.Sequential)]
    public class WaveHdr
    {
      /// <summary>pointer to locked data buffer (lpData)</summary>
      public IntPtr lpData;
      /// <summary>length of data buffer (dwBufferLength)</summary>
      public int dwBufferLength;
      /// <summary>used for input only (dwBytesRecorded)</summary>
      public int dwBytesRecorded;
      /// <summary>for client's use (dwUser)</summary>
      public IntPtr dwUser; // for client's use
      /// <summary>assorted flags</summary>
      public WaveHdrFlags dwFlags;
      /// <summary>loop control counter (dwLoops)</summary>
      public int dwLoops;
      /// <summary>PWaveHdr, reserved for driver (lpNext)</summary>
      public IntPtr lpNext;
      /// <summary>reserved for driver</summary>
      public int reserved; 
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WAVEOUTCAPS
    {
      public ushort wMid;
      public ushort wPid;
      public uint vDriverVersion;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 164)]
      public string szPname;
      public uint dwFormats;
      public ushort wChannels;
      public ushort wReserved1;
      public uint dwSupport;

      private Guid manufacturerGuid;
      private Guid productGuid;
      private Guid nameGuid;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WAVEINCAPS
    {
      public short ManufacturerId, ProductId;
      public uint DriverVersion;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
      public string Name;
      public uint Formats;
      public short Channels;
      ushort Reserved;
      public Guid ManufacturerGuid, ProductGuid, NameGuid;
    }

    /// <summary>
    /// Class for enumerating DirectSound devices
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class DirectSoundDeviceInfo
    {
      /// <summary>
      /// The device identifier
      /// </summary>
      public Guid Guid { get; set; }
      /// <summary>
      /// Device description
      /// </summary>
      public string Description { get; set; }
      /// <summary>
      /// Device module name
      /// </summary>
      public string ModuleName { get; set; }
    }

    // callbacks
    public delegate void WaveOutDelegate(IntPtr hdrvr, WaveMessage uMsg, int dwUser, WaveHdr wavhdr, int dwParam2);

    #endregion

    #region · Error handler helper · 

    /// <summary>
    /// Throws an exception when return code is not MMSYSERR_NOERROR
    /// </summary>
    /// <param name="err"></param>
    public static void Try(MMRESULT err)
    {
      if (err != MMRESULT.MMSYSERR_NOERROR)
        throw new Exception(err.ToString());
    }

    #endregion

    #region · DllImport ·

    // DLL names
    private const string mmdll = "winmm.dll";
    private const string dsdll = "dsound.dll";

    // Directsound calls
    [DllImport(dsdll, EntryPoint = "DirectSoundEnumerateA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern void DirectSoundEnumerate(DSEnumCallback lpDSEnumCallback, IntPtr lpContext);

    [DllImport(dsdll, EntryPoint = "DirectSoundCaptureEnumerateA", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern void DirectSoundCaptureEnumerate(DSEnumCallback lpDSEnumCallback, IntPtr lpContext);

    public delegate bool DSEnumCallback(IntPtr lpGuid, IntPtr lpcstrDescription, IntPtr lpcstrModule, IntPtr lpContext);

    // WaveOut calls
    [DllImport(mmdll)]
    public static extern int waveOutGetNumDevs();
    [DllImport(mmdll, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern MMRESULT waveOutGetDevCaps(IntPtr hwo, ref WAVEOUTCAPS pwoc, uint cbwoc);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);
    [DllImport(mmdll, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern MMRESULT waveOutOpen(out IntPtr hWaveOut, int uDeviceID, WaveFormat lpFormat, WaveOutDelegate dwCallback, uint dwInstance, uint dwFlags);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutReset(IntPtr hWaveOut);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutClose(IntPtr hWaveOut);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutPause(IntPtr hWaveOut);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutRestart(IntPtr hWaveOut);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutGetPosition(IntPtr hWaveOut, out int lpInfo, int uSize);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutSetVolume(IntPtr hWaveOut, int dwVolume);
    [DllImport(mmdll)]
    public static extern MMRESULT waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);

    // WaveIn calls
    [DllImport(mmdll)]
    public static extern int waveInGetNumDevs();
    [DllImport(mmdll)]
    public static extern uint waveInGetDevCaps(IntPtr deviceId, out WAVEINCAPS caps, int capsSize);
    [DllImport(mmdll)]
    public static extern int waveInAddBuffer(IntPtr hwi, ref WaveHdr pwh, int cbwh);
    [DllImport(mmdll)]
    public static extern int waveInClose(IntPtr hwi);
    [DllImport(mmdll)]
    public static extern int waveInOpen(out IntPtr phwi, int uDeviceID, WaveFormat lpFormat, WaveOutDelegate dwCallback, int dwInstance, int dwFlags);
    [DllImport(mmdll)]
    public static extern int waveInPrepareHeader(IntPtr hWaveIn, ref WaveHdr lpWaveInHdr, int uSize);
    [DllImport(mmdll)]
    public static extern int waveInUnprepareHeader(IntPtr hWaveIn, ref WaveHdr lpWaveInHdr, int uSize);
    [DllImport(mmdll)]
    public static extern int waveInReset(IntPtr hwi);
    [DllImport(mmdll)]
    public static extern int waveInStart(IntPtr hwi);
    [DllImport(mmdll)]
    public static extern int waveInStop(IntPtr hwi);

    #endregion
  }
}
