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
// Multi Channel Audio Manager Class
///////////////////////////////////////////////////////////////////////////////
using System.Threading;
using System.Windows;
using YATE.Drivers;
using YATE.Forms;
using YATE.Settings;
using YATECommon;
using YATECommon.Settings;

namespace YATE.Managers
{
  public class AudioManager : IAudioManager
  {
    #region ·  Constants ·
    public const int StereoChannelCount = 2; // Number of channels for stereo (2)
    public const int AudioChannelCount = 8;  // Number of handled audio channels
    public const int DefaultSampleRate = 44100;  // Desired sample rate
    public const int AudioLag = 200; // Audio lag in ms
    public const int AudioBufferLength = 20; // Length of the audio buffer in ms
    public const int AudioBufferSampleLength =  DefaultSampleRate * AudioBufferLength / 1000; // Length of the audio buffers in samples
    public const int AudioBufferCount = AudioLag / AudioBufferLength; // Number of used audio buffers
    #endregion

    #region · Types ·

    /// <summary>
    /// Audio Channel Information and State Class
    /// </summary>
    class ChannelInfo
    {
      public ulong TickPosition;
      public int SamplePosition;

      public AudioChannelRenderDelegate RenderingMethod;

      public ChannelInfo()
      {
        RenderingMethod = null;
        TickPosition = 0;
        SamplePosition = 0;
      }
    }
    #endregion

    #region · Data members · 
    private WaveOut m_wave_out;
    private ChannelInfo[] m_channels;
    private SetupAudioSettings m_settings;
    private ExecutionManager m_execution_manager;

    private int m_active_channels;

    private int[] m_rendering_buffer;
    private object m_audio_render_lock = new object();
    #endregion

    #region · Properties ·

    /// <summary>
    /// Current sample rate of the audio subsystem
    /// </summary>
    public uint SampleRate
    {
      get { return DefaultSampleRate; }
    }
    #endregion

    #region · Constructor ·

    /// <summary>
    /// Default constructor
    /// </summary>
    public AudioManager(ExecutionManager in_execution_manager)
    {
      m_active_channels = 0;

      m_rendering_buffer = new int[AudioBufferSampleLength * StereoChannelCount]; // stereo buffer
      for (int i = 0; i < m_rendering_buffer.Length; i++)
      {
        m_rendering_buffer[i] = 0;
      }

      m_channels = new ChannelInfo[AudioChannelCount];
      m_execution_manager = in_execution_manager;

      for (int i = 0; i < m_channels.Length; i++)
      {
        m_channels[i] = new ChannelInfo();
      }
    }
    #endregion

    #region · Public members ·

    /// <summary>
    /// Starts audio service
    /// </summary>
    public void Start()
    {
      int device_id;

      m_settings = SettingsFile.Default.GetSettings<SetupAudioSettings>();

      device_id = m_settings.AudioOutDevice;
      if (device_id == 0)
      {
        device_id = WaveNative.WAVE_MAPPER;
      }
      else
      {
        device_id = device_id - 1;
      }

      m_wave_out = new WaveOut();
      m_wave_out.OnSampleRequest = FinishAllChannels;

      m_wave_out.ChannelCount = StereoChannelCount;
      m_wave_out.SampleRate = (int)SampleRate;
      m_wave_out.BufferCount = AudioBufferCount;
      m_wave_out.BufferLength = AudioBufferSampleLength;

      m_wave_out.Open(device_id);

      m_wave_out.Start();
    }

    /// <summary>
    /// Stops audio service
    /// </summary>
    public void Stop()
    {
      m_wave_out.Stop();
    }

    /// <summary>
    /// Opens audio channel
    /// </summary>
    /// <param name="in_audio_rendering_method">Audio rendering method for the channel</param>
    /// <returns>Opened channel index</returns>
    public int OpenChannel(AudioChannelRenderDelegate in_audio_rendering_method)
    {
      int channel_index = -1;

      // find an unused channel
      for (int i = 0; i < m_channels.Length; i++)
      {
        if (m_channels[i].RenderingMethod == null)
        {
          channel_index = i;
          m_channels[i].RenderingMethod = in_audio_rendering_method;
          m_channels[i].SamplePosition = 0;
          m_channels[i].TickPosition = m_execution_manager.TVC.GetCPUTicks();
          m_active_channels++;
          break;
        }
      }

      return channel_index;
    }

    /// <summary>
    /// Closes audio channel
    /// </summary>
    /// <param name="in_channel_index">Audio channel index</param>
    public void CloseChannel(int in_channel_index)
    {
      if (in_channel_index >= 0 && in_channel_index < AudioChannelCount && m_channels[in_channel_index].RenderingMethod != null)
      {
        m_active_channels--;
        m_channels[in_channel_index].RenderingMethod = null;
      }
    }

    /// <summary>
    /// Advances channel to the given CPU tick
    /// </summary>
    /// <param name="in_channel_index">Audio channel index to advance</param>
    /// <param name="in_target_tick">CPU tick to reach</param>
    public void AdvanceChannel(int in_channel_index, ulong in_target_tick)
    {
      ulong cpu_clock = (ulong)m_execution_manager.TVC.CPUClock;

      lock (m_audio_render_lock)
      {
        ChannelInfo channel_info = m_channels[in_channel_index];

        ulong tick_count = in_target_tick - channel_info.TickPosition;

        int sample_count = (int)(tick_count * SampleRate / cpu_clock);

        // maximize sample count to buffer length
        if (channel_info.SamplePosition + sample_count > AudioBufferSampleLength)
        {
          sample_count = AudioBufferSampleLength - channel_info.SamplePosition;
        }

        if (sample_count > 0)
        {
          int end_sample_position = channel_info.SamplePosition + sample_count;

          channel_info?.RenderingMethod(m_rendering_buffer, channel_info.SamplePosition, end_sample_position);

          channel_info.SamplePosition = end_sample_position;
          channel_info.TickPosition += (ulong)sample_count * cpu_clock / SampleRate;
        }
      }
    }


    /// <summary>
    /// Advances all channels to the given CPU tick
    /// </summary>
    /// <param name="in_target_tick"></param>
    public void AdvanceAllChannels(ulong in_target_tick)
    {
      for (int i = 0; i < AudioChannelCount; i++)
      {
        if (m_channels[i].RenderingMethod != null)
          AdvanceChannel(i, in_target_tick);
      }
    }

    /// <summary>
    /// Finished rendering of all channels and copies rendered audio stream into the audio buffer for playback. Then resets the rendering buffer.
    /// </summary>
    /// <param name="inout_audio_buffer"></param>
    public void FinishAllChannels(short[] inout_audio_buffer)
    {
      ulong cpu_clock = (ulong)m_execution_manager.TVC.CPUClock;
      ulong cpu_tick = m_execution_manager.TVC.GetCPUTicks();

      // render channels if required
      lock (m_audio_render_lock)
      {
        for (int i = 0; i < AudioChannelCount; i++)
        {
          if (m_channels[i].RenderingMethod != null)
          {
            ChannelInfo channel_info = m_channels[i];

            if (channel_info.SamplePosition < AudioBufferSampleLength)
            {
              channel_info?.RenderingMethod(m_rendering_buffer, channel_info.SamplePosition, AudioBufferSampleLength);
              //ulong l = (ulong)(AudioBufferSampleLength - channel_info.SamplePosition) * (ulong)cpu_clock / SampleRate;
              //ulong d = channel_info.TickPosition + l;
              channel_info.TickPosition = cpu_tick;
            }

            channel_info.SamplePosition = 0; // reset sample position
          }
        }

        // copy rendering buffer to the audio buffer
        if (m_active_channels == 0)
        {
          for (int i = 0; i < AudioBufferSampleLength * StereoChannelCount; i++)
          {
            inout_audio_buffer[i] = 0;
            m_rendering_buffer[i] = 0;
          }
        }
        else
        {
          for (int i = 0; i < AudioBufferSampleLength * StereoChannelCount; i++)
          {
            inout_audio_buffer[i] = (short)(m_rendering_buffer[i] / m_active_channels);
            m_rendering_buffer[i] = 0;
          }
        }
      }

      m_execution_manager.SetSoundEvent();
    }

    public void UpdateSettings(bool in_restart_tvc)
    {
      
    }

    #endregion
  }
}

