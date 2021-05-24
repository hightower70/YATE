///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2020 Laszlo Arvai. All rights reserved.
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
// Audio System Manager Interface
///////////////////////////////////////////////////////////////////////////////

namespace YATECommon
{
  public delegate void AudioChannelRenderDelegate(int[] inout_buffer, int in_start_sample_index, int in_end_sample_index);

  public interface IAudioManager
  {
    int OpenChannel(AudioChannelRenderDelegate in_audio_rendering_method);
    void CloseChannel(int in_channel_index);

    void AdvanceChannel(int in_channel_index, ulong in_target_tick);

    void Start();
    void Stop();
  }
}
