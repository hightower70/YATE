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
using System.Windows;
using YATE.Drivers;
using YATE.Forms;
using YATE.Settings;
using YATECommon.Settings;

namespace YATE.Controls
{
  public class AudioControl
  {
    #region ·  Constants ·
    public const int ChannelCount = 8;
    public const int SampleRate = 44100;
    public const int AudioBufferLength = 2 * 44100 / 10;
    public const int AudioBufferCount = 2;
    #endregion

    #region · Types ·

    public delegate void ChannelRenderDelegate(int[] inout_buffer, int in_start_sample_index, int in_end_sample_index);


    class ChannelInfo
    {
      public ulong Position;
    }
    #endregion

    #region · Data members · 
    private MainWindow m_main_window;
    private WaveOut m_wave_out;
    private SetupAudioSettings m_settings;

    private int[] m_audio_buffer;
    #endregion

    #region · Constructor ·
    public AudioControl(Window in_parent_window)
    {
      m_main_window = (MainWindow)in_parent_window;

    }
    #endregion

    public void Start()
    {
      int device_id;

      m_settings = SettingsFile.Default.GetSettings<SetupAudioSettings>();

      device_id = m_settings.AudioOutDevice;
      if(device_id == 0)
      {
        device_id = WaveNative.WAVE_MAPPER;
      }
      else
      {
        device_id = device_id - 1;
      }

      m_wave_out = new WaveOut();

      m_wave_out.Open(device_id, SampleRate, AudioBufferCount, AudioBufferLength);

      m_wave_out.Start();
    }

    public void Stop()
    {
      m_wave_out.Stop();
    }


    public int OpenChannel()
    {
      return 0;
    }

    public void CloseChannel()
    {

    }

    public void AdvanceChannel(ulong in_target_tick)
    {

    }
  }
}

