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
// Windows WAV file reader/writer class
///////////////////////////////////////////////////////////////////////////////
using System.IO;
using System.Runtime.InteropServices;
using YATECommon.Helpers;

namespace YATECommon.Files
{
  class WavFile
  {
    #region · Constants ·
    const int WAVE_BLOCK_LENGTH = 65536;
    const int RIFF_HEADER_CHUNK_ID = 0x46464952; /* 'RIFF' */
    const int RIFF_HEADER_FORMAT_ID = 0x45564157; /* 'WAVE' */
    const int CHUNK_ID_FORMAT = 0x20746d66; /* 'fmt ' */
    const int CHUNK_ID_DATA = 0x61746164; /* 'data' */
    #endregion

    #region · Types ·
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private class RIFFHeaderType
    {
      public uint ChunkID;   // 'RIFF'
      public uint ChunkSize;
      public uint Format;    // 'WAVE'
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private class ChunkHeaderType
    {
      public uint ChunkID;
      public uint ChunkSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private class FormatChunkType
    {
      public ushort AudioFormat; // PCM = 1
      public ushort NumChannels;
      public uint SampleRate;
      public uint ByteRate;
      public ushort BlockAlign;
      public ushort BitsPerSample;
    }

    #endregion

    #region · Data members ·

    private FileStream m_output_wav_stream;
    private BinaryWriter m_output_wav_writer;
    private FormatChunkType m_output_wav_file_format_chunk;
    private uint m_output_wav_file_sample_count;
    private byte m_output_wav_file_sample_buffer;
    private byte m_output_wav_file_sample_bit_pos = 0;

    #endregion

    #region · Wave file write functions ·

    ///////////////////////////////////////////////////////////////////////////////
    // Creates wave file
    public void OpenOutput(string in_file_name, uint in_sample_rate, byte in_num_channels, byte in_bits_per_sample)
    {
      // create file
      m_output_wav_file_sample_bit_pos = 0;
      m_output_wav_file_sample_buffer = 0;
      m_output_wav_file_sample_count = 0;

      m_output_wav_stream = new FileStream(in_file_name, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
      m_output_wav_writer = new BinaryWriter(m_output_wav_stream);
 
      // write RIFF header
      WriteRIFFHeader();

      // write format chunk header
      ChunkHeaderType chunk_header = new ChunkHeaderType();

      chunk_header.ChunkID = CHUNK_ID_FORMAT;
      chunk_header.ChunkSize = (uint)Marshal.SizeOf(m_output_wav_file_format_chunk);

      m_output_wav_writer.Write(chunk_header);

      // write format chunk
      m_output_wav_file_format_chunk = new FormatChunkType();

      m_output_wav_file_format_chunk.AudioFormat = 1;
      m_output_wav_file_format_chunk.SampleRate = in_sample_rate;
      m_output_wav_file_format_chunk.NumChannels = in_num_channels;
      m_output_wav_file_format_chunk.BitsPerSample = in_bits_per_sample;
      m_output_wav_file_format_chunk.BlockAlign = 1;
      m_output_wav_file_format_chunk.ByteRate = m_output_wav_file_format_chunk.SampleRate * m_output_wav_file_format_chunk.NumChannels * m_output_wav_file_format_chunk.BitsPerSample / 8;

      m_output_wav_writer.Write(m_output_wav_file_format_chunk);

      // write chunk header
      chunk_header.ChunkID = CHUNK_ID_DATA;
      chunk_header.ChunkSize = 0;

      m_output_wav_writer.Write(chunk_header);
    }

    /////////////////////////////////////////////////////////////////////////////////////////
    // Write sample to the output wave file
    public void WriteSample(ushort in_sample)
    {
      switch (m_output_wav_file_format_chunk.BitsPerSample)
      {
        case 1:
          m_output_wav_file_sample_buffer <<= 1;
          if (in_sample > 128)
            m_output_wav_file_sample_buffer |= 1;

          m_output_wav_file_sample_bit_pos++;
          if (m_output_wav_file_sample_bit_pos > 7)
          {
            m_output_wav_writer.Write(m_output_wav_file_sample_buffer);
            m_output_wav_file_sample_bit_pos = 0;
            m_output_wav_file_sample_buffer = 0;
          }

          break;

        case 8:
          m_output_wav_writer.Write((byte)in_sample);
          break;

        case 16:
          m_output_wav_writer.Write(in_sample);
          break;
      }

      m_output_wav_file_sample_count++;
    }

    ///////////////////////////////////////////////////////////////////////////////
    // Closes wave output file
    public void CloseOutput()
    {
      // save remaining bits in one bits per sample mode
      if (m_output_wav_file_format_chunk.BitsPerSample == 1 && m_output_wav_file_sample_bit_pos > 0)
      {
        m_output_wav_file_sample_buffer <<= (8 - m_output_wav_file_sample_bit_pos);
        m_output_wav_writer.Write(m_output_wav_file_sample_buffer);
      }

      // update riff header
      m_output_wav_writer.Seek(0, SeekOrigin.Begin);

      WriteRIFFHeader();

      // update data header
      int pos = Marshal.SizeOf(typeof(RIFFHeaderType)) + Marshal.SizeOf(typeof(ChunkHeaderType)) + Marshal.SizeOf(typeof(FormatChunkType));
      m_output_wav_writer.Seek(pos, SeekOrigin.Begin);

      ChunkHeaderType chunk_header = new ChunkHeaderType();

      chunk_header.ChunkID = CHUNK_ID_DATA;
      chunk_header.ChunkSize = m_output_wav_file_sample_count * m_output_wav_file_format_chunk.NumChannels * m_output_wav_file_format_chunk.BitsPerSample / 8;

      m_output_wav_writer.Write(chunk_header);

      m_output_wav_writer.Close();
      m_output_wav_stream.Close();
      m_output_wav_file_format_chunk = null;
    }

    ///////////////////////////////////////////////////////////////////////////////
    // Writes (or updates) riff file header
    private void WriteRIFFHeader()
    {
      RIFFHeaderType riff_header = new RIFFHeaderType();

      //                 RIFF FORMAT		 fmt chunk header          format chunk content			 data chunk header				 data chunk content
      long chunk_size = Marshal.SizeOf(typeof(uint)) + Marshal.SizeOf(typeof(ChunkHeaderType)) + Marshal.SizeOf(typeof(FormatChunkType)) + Marshal.SizeOf(typeof(ChunkHeaderType)) + m_output_wav_file_sample_count;

      // write header
      riff_header.ChunkID = RIFF_HEADER_CHUNK_ID;
      riff_header.Format = RIFF_HEADER_FORMAT_ID;
      riff_header.ChunkSize = (uint)chunk_size;

      m_output_wav_writer.Write(riff_header);
    }

    #endregion

  }
}
