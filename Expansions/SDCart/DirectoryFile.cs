using System;
using System.IO;
using YATECommon;
using YATECommon.Files;
using YATECommon.Helpers;

namespace SDCart
{
  class DirectoryFile
  {
    public const int DirectoryBufferSize = 512;
    private const int DirectoryFileNameLength = 21;
    private const int DirectoryMaxFileNameLength = 240;
    private const int DirectoryExensionLength = 3;

    // directory members
    private FileInfo[] m_directory_info;
    private byte[] m_directory_buffer;
    private int m_directory_pos;
    private int m_directory_index;
    private int m_line_number;

    private byte[] m_path_header1 = new byte[] { 0x00, 0x01, 0x00, 0xD6, 0x20, 0xC4, 0x20, 0x34, 0xA0, 0xBC, 0x20, 0x31, 0xFD, 0xDD, 0x20, 0x22, 0x20, 0x6B, 0x42, 0x20, 0x22, 0xA0, 0x43, 0x48, 0x52, 0x24, 0x96, 0x33, 0x34, 0x95, 0xA0, 0x22 };
    private byte[] m_path_header2 = new byte[] { 0x22, 0xA0, 0x43, 0x48, 0x52, 0x24, 0x96, 0x33, 0x34, 0x95, 0xA0, 0x22 };
    private byte[] m_path_footer = new byte[] { 0x22, 0xA0, 0xFD, 0xD6, 0x20, 0xC4, 0x20, 0x31, 0xA0, 0xBC, 0x20, 0x34, 0xFD, 0xDD, 0xFF };
    private byte[] m_filename_header1 = new byte[] { 0x00, 0x01, 0x00, 0xDD, 0x20, 0x22, 0x20, 0x20, 0x20, 0x20, 0x22, 0xA0, 0x43, 0x48, 0x52, 0x24, 0x96, 0x33, 0x34, 0x95, 0xA0, 0x22 };
    private byte[] m_filename_header2 = new byte[] { 0x22, 0xA0, 0x43, 0x48, 0x52, 0x24, 0x96, 0x33, 0x34, 0x95, 0xA0, 0x22 };
    private byte[] m_filename_footer = new byte[] { 0x22, 0xFF };

    public Cassette Cas { get; private set; }

    public DirectoryFile(Cassette in_cassette)
    {
      Cas = in_cassette;
      m_directory_buffer = new byte[DirectoryBufferSize];
    }

    public void Open()
    {
      int buffer_pos = 0;
      int dir_length = 0;

      // path display length
      StoreRootPathInfo(ref buffer_pos);
      dir_length += buffer_pos;
      buffer_pos = 0;

      // files display length
      DirectoryInfo directory_info = new DirectoryInfo(Cas.CoProc.Cart.Settings.FilesystemFolder);
      m_directory_info = directory_info.GetFiles();
      foreach (FileInfo file_info in m_directory_info)
      {
        StoreFileInfo(ref buffer_pos, file_info);
        dir_length += buffer_pos;
        buffer_pos = 0;
      }

      dir_length += 1; //  Terminator Basic program zero

      // Create CAS header
      TVCFileTypes.CASProgramFileHeaderType cas_header = new TVCFileTypes.CASProgramFileHeaderType();
      cas_header.FileType = 1;
      cas_header.FileLength = (ushort)dir_length;   // Length of the file
      cas_header.Autorun = 0xff;                    // Autostart: 0xff, no autostart: 0x00

      using (MemoryStream stream = new MemoryStream())
      {
        stream.Write(cas_header);
        stream.ToArray().CopyTo(m_directory_buffer, 0);
      }

      m_directory_pos = 0;
      m_directory_index = 0;
    }

    public void Close()
    {
      m_directory_info = null;
    }

    public void ReadByte()
    {
      if (m_directory_pos < m_directory_buffer.Length)
      {
        // directory mode
        Cas.ReadByteBuffer = m_directory_buffer[m_directory_pos++];
      }
      else
      {
        Cas.EndFunction(CoProcessorConstants.ERR_END_OF_FILE);
      }
    }

    internal void ReadBlock()
    {
      if (m_directory_index == 0)
      {
        m_directory_pos = 0;
        StoreRootPathInfo(ref m_directory_pos);
      }

      if (m_directory_index < m_directory_info.Length)
      {
        while (m_directory_pos < Cassette.InternalBufferSize && m_directory_index < m_directory_info.Length)
        {
          StoreFileInfo(ref m_directory_pos, m_directory_info[m_directory_index]);
          m_directory_index++;
        }

        if (m_directory_index == m_directory_info.Length)
        {
          m_directory_buffer[m_directory_pos++] = 0x00; // terminator 
        }
      }

      if (m_directory_index < m_directory_info.Length || m_directory_pos != 0)
      {
        Buffer.BlockCopy(m_directory_buffer, 0, Cas.ReadBuffer, 0, Cassette.InternalBufferSize);
        Buffer.BlockCopy(m_directory_buffer, Cassette.InternalBufferSize, m_directory_buffer, 0, Cassette.InternalBufferSize);

        if (m_directory_pos > Cassette.InternalBufferSize)
          m_directory_pos -= Cassette.InternalBufferSize;
        else
          m_directory_pos = 0;
      }
      else
      {
        Cas.EndFunction(CoProcessorConstants.ERR_END_OF_FILE);
      }
    }
    private const int MaxPathLineLength = 24;

    private void StoreRootPathInfo(ref int in_buffer_pos)
    {
      string root_folder = "\\Defaultpath";

      int length = 0;
      m_line_number = 1;

      // header 1
      Array.Copy(m_path_header1, 0, m_directory_buffer, in_buffer_pos, m_path_header1.Length);
      length += m_path_header1.Length;

      // path
      int pos = 0;
      while (pos < root_folder.Length && pos < MaxPathLineLength)
      {
        m_directory_buffer[in_buffer_pos + length] = (byte)TVCCharacterCodePage.UNICODECharToTVCChar(root_folder[pos]);
        pos++;
        length++;
      }

      // header 2
      Array.Copy(m_path_header2, 0, m_directory_buffer, in_buffer_pos + length, m_path_header2.Length);
      length += m_path_header2.Length;

      while (pos < MaxPathLineLength + 1)
      {
        m_directory_buffer[in_buffer_pos + length] = (byte)' ';
        pos++;
        length++;
      }

      // footer
      Array.Copy(m_path_footer, 0, m_directory_buffer, in_buffer_pos + length, m_path_footer.Length);
      length += m_path_footer.Length;

      // update basic line length
      m_directory_buffer[in_buffer_pos] = (byte)length;
      in_buffer_pos += length;
    }

    private void StoreFileInfo(ref int in_buffer_pos, FileInfo in_file_info)
    {
      string name = Path.GetFileNameWithoutExtension(in_file_info.Name);
      int length = 0;

      // header 1
      Array.Copy(m_filename_header1, 0, m_directory_buffer, in_buffer_pos, m_filename_header1.Length);
      length += m_filename_header1.Length;

      // update basic line number
      m_directory_buffer[in_buffer_pos + 1] = (byte)(m_line_number % 256);
      m_directory_buffer[in_buffer_pos + 2] = (byte)(m_line_number / 256);

      // store file length in kb
      int file_length = (int)((in_file_info.Length + 1023) / 1024);

      if (file_length > 999)
      {
        m_directory_buffer[in_buffer_pos + 6] = (byte)'x';
        m_directory_buffer[in_buffer_pos + 7] = (byte)'x';
        m_directory_buffer[in_buffer_pos + 8] = (byte)'x';
      }
      else
      {
        if (file_length >= 100)
          m_directory_buffer[in_buffer_pos + 6] = (byte)(file_length / 100 + '0');
        if (file_length >= 10)
          m_directory_buffer[in_buffer_pos + 7] = (byte)((file_length % 100) / 10 + '0');
        m_directory_buffer[in_buffer_pos + 8] = (byte)(file_length % 10 + '0');
      }

      // store filename
      int pos = 0;
      while (pos < name.Length && pos < DirectoryFileNameLength)
      {
        m_directory_buffer[in_buffer_pos + length] = (byte)TVCCharacterCodePage.UNICODECharToTVCChar(name[pos]);
        pos++;
        length++;
      }

      // header 2
      Array.Copy(m_filename_header2, 0, m_directory_buffer, in_buffer_pos + length, m_filename_header2.Length);
      length += m_filename_header2.Length;

      // padding filename with spaces
      while (pos < DirectoryFileNameLength + 1)
      {
        m_directory_buffer[in_buffer_pos + length] = (byte)' ';
        pos++;
        length++;
      }

      // extension
      string extension = in_file_info.Extension.Substring(1);
      for (int i = 0; i < DirectoryExensionLength; i++)
      {
        if (i < extension.Length)
          m_directory_buffer[in_buffer_pos + length] = (byte)TVCCharacterCodePage.UNICODECharToTVCChar(extension[i]);
        else
          m_directory_buffer[in_buffer_pos + length] = (byte)' ';
        length++;
      }

      // header 2
      Array.Copy(m_filename_footer, 0, m_directory_buffer, in_buffer_pos + length, m_filename_footer.Length);
      length += m_filename_footer.Length;

      // update basic line length
      m_directory_buffer[in_buffer_pos] = (byte)length;
      in_buffer_pos += length;
      m_line_number++;
    }


#if false
        private void StoreFileInfo(ref int in_buffer_pos, FileInfo in_file_info)
    {
      string name = Path.GetFileNameWithoutExtension(in_file_info.Name);

      // line length
      m_directory_buffer[in_buffer_pos++] = 2 + DirectoryFileNameLength + DirectoryExensionLength + 2 + 1; // line number+file name+extension+apostrophes+0xff

      // store file length in kb
      int file_length = (int)((in_file_info.Length + 1023) / 1024);
      m_directory_buffer[in_buffer_pos++] = (byte)(file_length % 256);
      m_directory_buffer[in_buffer_pos++] = (byte)(file_length / 256);

      m_directory_buffer[in_buffer_pos++] = (byte)'\"';

      // filename
      if (name.Length > DirectoryFileNameLength)
      {
        // handle long filename
        for (int i = 0; i < name.Length && i < DirectoryFileNameLength; i++)
        {
          m_directory_buffer[in_buffer_pos++] = (byte)TVCCharacterCodePage.UNICODECharToTVCChar(name[i]);
        }
        m_directory_buffer[in_buffer_pos++] = (byte)'\"';
      }
      else
      {
        // copy short filename
        for (int i = 0; i < DirectoryFileNameLength; i++)
        {
          if (i < name.Length)
          {
            m_directory_buffer[in_buffer_pos++] = (byte)TVCCharacterCodePage.UNICODECharToTVCChar(name[i]);
          }
          else
          {
            if (i == name.Length)
              m_directory_buffer[in_buffer_pos++] = (byte)'\"';

            m_directory_buffer[in_buffer_pos++] = (byte)' ';
          }
        }
      }

      // extension
      string extension = in_file_info.Extension.Substring(1);
      for (int i = 0; i < DirectoryExensionLength; i++)
      {
        if (i < extension.Length)
          m_directory_buffer[in_buffer_pos++] = (byte)TVCCharacterCodePage.UNICODECharToTVCChar(extension[i]);
        else
          m_directory_buffer[in_buffer_pos++] = (byte)' ';
      }

      // line terminator
      m_directory_buffer[in_buffer_pos++] = 0xff;
    }
#endif
  }
}
