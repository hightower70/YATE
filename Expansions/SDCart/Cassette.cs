using System.IO;
using System.Runtime.InteropServices;
using YATECommon;
using YATECommon.Files;
using YATECommon.Helpers;

namespace SDCart
{
  /// <summary>
  /// Cassette function emulator
  /// </summary>
  public class Cassette : IFunctionGroup
  {
    internal const int InternalBufferSize = 256;

    private enum FileReadMode { File, Directory }
    private enum FileWriteMode { CASFile, File }

    public CoProcessor CoProc { get; private set; }

    private byte m_status_code;
    private CoProcessor.CoProcessorReadHandler m_read_function_to_set;

    private string m_filename_param;
    private int m_filename_length_param;

    private int m_read_data_index;
    private int m_write_data_index;

    // file reader members
    private FileStream m_read_stream;
    private BinaryReader m_read_file;
    public byte ReadByteBuffer { get; set; }
    public byte[] ReadBuffer { get; private set; }
    public int ReadLength { get; set; }
    public int ReadPos { get; set; }
    private FileReadMode m_read_mode;


    // File write members
    private FileStream m_write_stream;
    private BinaryWriter m_write_file;
    private byte[] m_write_buffer;
    private int m_write_length;
    private int m_write_pos;
    private FileWriteMode m_write_mode;


    private DirectoryFile m_directory_file;

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="in_parent"></param>
    public Cassette(CoProcessor in_parent)
    {
      CoProc = in_parent;

      m_directory_file = new DirectoryFile(this);

      ReadBuffer = new byte[InternalBufferSize];
      m_write_buffer = new byte[InternalBufferSize];
    }

    /// <summary>
    /// Reads status code
    /// </summary>
    /// <returns></returns>
    public byte CoProcessorRead()
    {
      return m_status_code;
    }

    /// <summary>
    /// Reads
    /// </summary>
    /// <returns></returns>
    private byte CoProcessorReadResult()
    {
      CoProc.CoProcessorRead = m_read_function_to_set;
      m_read_function_to_set = null;
      m_read_data_index = 0;
      return CoProcessorConstants.ERR_RESULT;
    }

    /// <summary>
    /// Returns filename to TVC
    /// </summary>
    /// <returns></returns>
    private byte CoProcessorReadFilename()
    {
      byte retval;

      if (m_read_data_index == 0)
      {
        // filename length
        retval = (byte)m_filename_length_param;
      }
      else
      {
        // filename characters
        retval = (byte)m_filename_param[m_read_data_index - 1];
      }

      m_read_data_index++;

      if (m_read_data_index - 1 == m_filename_length_param)
      {
        // last character
        EndFunction(CoProcessorConstants.ERR_OK);
      }

      return retval;
    }

    /// <summary>
    /// Reads byte from coprocessor
    /// </summary>
    /// <returns></returns>
    private byte CoProcessorReadByte()
    {
      EndFunction(CoProcessorConstants.ERR_OK);

      return ReadByteBuffer;
    }

    /// <summary>
    /// Ends function with OK status
    /// </summary>
    /// <returns></returns>
    private byte CoProcessorReadStatusOK()
    {
      EndFunction(CoProcessorConstants.ERR_OK);

      return CoProcessorConstants.ERR_OK;
    }

    /// <summary>
    /// Reads block from coprocessor
    /// </summary>
    /// <returns></returns>
    private byte CoProcessorReadBlock()
    {
      byte retval = 0xff;

      retval = ReadBuffer[ReadPos];

      ReadPos++;

      if (ReadPos == ReadLength)
      {
        // last byte
        EndFunction(CoProcessorConstants.ERR_OK);
      }

      return retval;
    }

    /// <summary>
    /// First byte (function code) write
    /// </summary>
    /// <param name="in_data"></param>
    public void CoProcessorWrite(byte in_data)
    {
      switch (in_data)
      {
        case CoProcessorConstants.CPFN_OPEN:
          StartFunctionWithParameters(CasOpen);
          break;

        case CoProcessorConstants.CPFN_RDCH:
          StartFunctionWithoutParameters();
          CasReadByte();
          break;

        case CoProcessorConstants.CPFN_RDBLK:
          StartFunctionWithParameters(CasReadBlock);
          break;

        case CoProcessorConstants.CPFN_RDCLOSE:
          StartFunctionWithoutParameters();
          CasCloseRead();
          break;

        case CoProcessorConstants.CPFN_VYBLK:
          StartFunctionWithParameters(CasVerifyBlock);
          break;

        case CoProcessorConstants.CPFN_CREATE:
          StartFunctionWithParameters(CasCreate);
          break;

        case CoProcessorConstants.CPFN_WRCH:
          StartFunctionWithParameters(CasWriteByte);
          break;

        case CoProcessorConstants.CPFN_WRBLK:
          StartFunctionWithParameters(CasWriteBlock);
          break;

        case CoProcessorConstants.CPFN_WRCLOSE:
          StartFunctionWithoutParameters();
          CasCloseWrite();
          break;
      }
    }

    /// <summary>
    /// Opens file for reading
    /// </summary>
    /// <param name="in_data">Function parameter bytes</param>
    private void CasOpen(byte in_data)
    {
      if (m_write_data_index == 0)
      {
        m_filename_length_param = in_data;
        m_filename_param = "";
      }

      // store filename character
      int filename_index = m_write_data_index - 1;
      if (filename_index >= 0 && filename_index < m_filename_length_param)
      {
        m_filename_param += (char)in_data;
      }

      // if last character is received
      if (filename_index + 1 == m_filename_length_param)
      {
        if (m_filename_param == "+")
        {
          m_read_mode = FileReadMode.Directory;

          m_directory_file.Open();

          m_filename_param = "+";
          SetReadResult(CoProcessorReadFilename);
        }
        else
        {
          if (m_read_stream == null)
          {
            // Last filename character received
            m_read_mode = FileReadMode.File;

            string filename;
            if (m_filename_length_param == 0)
            {
              filename = GetFilePath("start");
            }
            else
            {
              filename = GetFilePath(m_filename_param);
            }

            if (!File.Exists(filename))
            {
              EndFunction(CoProcessorConstants.ERR_FILE_NOT_FOUND);
            }
            else
            {
              if (!Directory.Exists(Path.GetDirectoryName(filename)))
              {
                EndFunction(CoProcessorConstants.ERR_NO_PATH);
              }
              else
              {
                // try to open the file
                try
                {
                  m_read_stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                  m_read_file = new BinaryReader(m_read_stream);

                  // In case of CAS file skip UPM header
                  if (string.Compare(Path.GetExtension(filename), ".CAS", true) == 0)
                  {
                    m_read_stream.Seek(Marshal.SizeOf(typeof(TVCFileTypes.CASUPMHeaderType)), SeekOrigin.Begin);
                  }

                  // generate result file name
                  filename = Path.GetFileNameWithoutExtension(filename);
                  if (filename.Length < m_filename_length_param)
                    m_filename_length_param = filename.Length;
                  m_filename_param = TVCCharacterCodePage.UNICODEStringToTVCString(filename);
                  SetReadResult(CoProcessorReadFilename);
                }
                catch
                {
                  EndFunction(CoProcessorConstants.ERR_FILE_NOT_FOUND);
                }
              }
            }
          }
          else
          {
            // if file is already created
            EndFunction(CoProcessorConstants.ERR_ALREADY_OPENED);
          }
        }
      }

      m_write_data_index++;
    }

    /// <summary>
    /// Reads byte from the input file
    /// </summary>
    private void CasReadByte()
    {
      if (m_read_mode == FileReadMode.File)
      {
        // file read
        if (m_read_file == null)
        {
          EndFunction(CoProcessorConstants.ERR_END_OF_FILE);
        }
        else
        {
          try
          {
            ReadByteBuffer = m_read_file.ReadByte();
            SetReadResult(CoProcessorReadByte);
          }
          catch
          {
            EndFunction(CoProcessorConstants.ERR_END_OF_FILE);
          }
        }
      }
      else
      {
        m_directory_file.ReadByte();
        SetReadResult(CoProcessorReadByte);
      }
    }

    /// <summary>
    /// Reads block from the input file
    /// </summary>
    /// <param name="in_data"></param>
    private void CasReadBlock(byte in_data)
    {
      if (m_read_data_index == 0)
      {
        // set block length
        if (in_data == 0)
        {
          ReadLength = 256;
        }
        else
        {
          ReadLength = in_data;
        }

        if (m_read_mode == FileReadMode.File)
        {
          // read block
          try
          {
            m_read_file.Read(ReadBuffer, 0, ReadLength);
            ReadPos = 0; // reset read index
            SetReadResult(CoProcessorReadBlock);
          }
          catch
          {
            EndFunction(CoProcessorConstants.ERR_NO_MORE_DATA);
          }
        }
        else
        {
          m_directory_file.ReadBlock();
          ReadPos = 0; // reset read index
          SetReadResult(CoProcessorReadBlock);
        }
      }

      m_read_data_index++;
    }

    /// <summary>
    /// Verifies block of the opened file
    /// </summary>
    /// <param name="in_data"></param>
    private void CasVerifyBlock(byte in_data)
    {
      if (m_write_data_index == 0)
      {
        if (in_data == 0)
        {
          m_write_length = 256;
        }
        else
        {
          m_write_length = in_data;
        }

        m_write_pos = 0;
      }
      else
      {
        m_write_buffer[m_write_pos++] = in_data;

        if (m_write_pos == m_write_length)
        {
          if (m_read_file != null && m_read_mode == FileReadMode.File)
          {
            // read block
            try
            {
              m_read_file.Read(ReadBuffer, 0, m_write_length);

              // compare buffer content
              int i;
              for (i = 0; i < m_write_length; i++)
              {
                if (ReadBuffer[i] != m_write_buffer[i])
                {
                  EndFunction(CoProcessorConstants.ERR_VERIFY);
                  break;
                }
              }

              if (i >= m_write_length)
                EndFunction(CoProcessorConstants.ERR_OK);
            }
            catch
            {
              EndFunction(CoProcessorConstants.ERR_DISK_FULL);
            }
          }
        }
      }

      m_write_data_index++;
    }

    /// <summary>
    /// Closes opened file after read
    /// </summary>
    private void CasCloseRead()
    {
      if (m_read_mode == FileReadMode.File)
      {
        if (m_read_file != null)
        {
          try
          {
            m_read_file.Close();
            m_read_stream.Close();

            EndFunction(CoProcessorConstants.ERR_OK);
          }
          catch
          {
            EndFunction(CoProcessorConstants.ERR_DISK_FULL);
          }
          finally
          {
            m_read_file = null;
            m_read_stream = null;
          }
        }
        else
        {
          EndFunction(CoProcessorConstants.ERR_NO_OPENED_FILE);
        }
      }
      else
      {
        m_read_mode = FileReadMode.File;
        m_directory_file.Close();

        EndFunction(CoProcessorConstants.ERR_OK);
      }
    }

    /// <summary>
    /// Creates file for write
    /// </summary>
    /// <param name="in_data">Function parameter bytes</param>
    private void CasCreate(byte in_data)
    {
      if (m_write_data_index == 0)
      {
        m_filename_length_param = in_data;
        m_filename_param = "";
      }

      // store filename character
      int filename_index = m_write_data_index - 1;
      if (filename_index >= 0 && filename_index < m_filename_length_param)
      {
        m_filename_param += (char)in_data;
      }

      // if last character is received
      if (filename_index + 1 == m_filename_length_param)
      {
        // create file
        if (m_write_file == null)
        {
          string filename = GetFilePath(m_filename_param);
          if (File.Exists(filename))
          {
            EndFunction(CoProcessorConstants.ERR_FILE_EXISTS);
          }
          else
          {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
              EndFunction( CoProcessorConstants.ERR_NO_PATH);
            }
            else
            {
              // try to create file
              try
              {
                m_write_stream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite);
                m_write_file = new BinaryWriter(m_write_stream);

                // In case of CAS file write UPM header
                if (string.Compare(Path.GetExtension(filename), ".CAS", true) == 0)
                {
                  m_write_mode = FileWriteMode.CASFile;

                  // create and write empty upm header (close will update it)
                  TVCFileTypes.CASUPMHeaderType upm_header = new TVCFileTypes.CASUPMHeaderType();

                  upm_header.FileType = TVCFileTypes.CASBlockHeaderFileUnbuffered;
                  upm_header.CopyProtect = 0;
                  upm_header.BlockNumber = 0;
                  upm_header.LastBlockBytes = 0;

                  m_write_file.Write(upm_header);
                }
                else
                {
                  m_write_mode = FileWriteMode.File;
                }

                // gererate result file name
                filename = Path.GetFileNameWithoutExtension(filename);
                m_filename_param = TVCCharacterCodePage.UNICODEStringToTVCString(filename);
                SetReadResult(CoProcessorReadFilename);
              }
              catch
              {
                EndFunction(CoProcessorConstants.ERR_FILE_EXISTS);
              }
            }
          }
        }
        else
        {
          // if file is already created
          EndFunction(CoProcessorConstants.ERR_ALREADY_OPENED);
        }
      }

      m_write_data_index++;
    }

    /// <summary>
    /// Writes byte to the opened file
    /// </summary>
    /// <param name="in_data">Bytes to write</param>
    private void CasWriteByte(byte in_data)
    {
      if (m_write_file == null)
      {
        EndFunction(CoProcessorConstants.ERR_NO_OPENED_FILE);
      }
      else
      {
        try
        {
          // write byte
          m_write_file.Write(in_data);
          EndFunction(CoProcessorConstants.ERR_OK);
        }
        catch
        {
          EndFunction(CoProcessorConstants.ERR_DISK_FULL);
        }
      }
    }

    /// <summary>
    /// Writes block of byte to the opened file
    /// </summary>
    /// <param name="in_data"></param>
    private void CasWriteBlock(byte in_data)
    {
      if (m_write_data_index == 0)
      {
        if (in_data == 0)
        {
          m_write_length = 256;
        }
        else
        {
          m_write_length = in_data;
        }

        m_write_pos = 0;
      }
      else
      {
        m_write_buffer[m_write_pos++] = in_data;

        if (m_write_pos == m_write_length)
        {
          try
          {
            m_write_file.Write(m_write_buffer, 0, m_write_length);
            EndFunction(CoProcessorConstants.ERR_OK);
          }
          catch
          {
            EndFunction(CoProcessorConstants.ERR_DISK_FULL);
          }
        }
      }

      m_write_data_index++;
    }

    /// <summary>
    /// Closes opened file after write
    /// </summary>
    private void CasCloseWrite()
    {
      if (m_write_file != null)
      {
        try
        {
          if (m_write_mode == FileWriteMode.CASFile)
          {
            // read back cas header
            BinaryReader reader = new BinaryReader(m_write_stream);
            reader.BaseStream.Seek(Marshal.SizeOf(typeof(TVCFileTypes.CASUPMHeaderType)), SeekOrigin.Begin);
            TVCFileTypes.CASProgramFileHeaderType program_header = new TVCFileTypes.CASProgramFileHeaderType();
            reader.Read(program_header);

            // in case of cas file update header
            TVCFileTypes.CASUPMHeaderType upm_header = new TVCFileTypes.CASUPMHeaderType();

            ushort cas_length = (ushort)(program_header.FileLength + Marshal.SizeOf(typeof(TVCFileTypes.CASUPMHeaderType)) + Marshal.SizeOf(typeof(TVCFileTypes.CASProgramFileHeaderType)));

            upm_header.FileType = TVCFileTypes.CASBlockHeaderFileUnbuffered;
            upm_header.CopyProtect = 0;
            upm_header.BlockNumber = (byte)(cas_length / 128);
            upm_header.LastBlockBytes = (byte)(cas_length % 128);

            m_write_file.Seek(0, SeekOrigin.Begin);
            m_write_file.Write(upm_header);
          }

          m_write_file.Close();
          m_write_stream.Close();

          EndFunction(CoProcessorConstants.ERR_OK);
        }
        catch
        {
          EndFunction(CoProcessorConstants.ERR_DISK_FULL);
        }
        finally
        {
          m_write_file = null;
          m_write_stream = null;
        }
      }
      else
      {
        EndFunction(CoProcessorConstants.ERR_NO_OPENED_FILE);
      }
    }

    /// <summary>
    /// Starts function without parameters
    /// </summary>
    private void StartFunctionWithoutParameters()
    {
      m_status_code = CoProcessorConstants.ERR_BUSY;
    }

    /// <summary>
    /// Starts function with parameters. Initializes parameter receiving
    /// </summary>
    /// <param name="in_write_handler"></param>
    private void StartFunctionWithParameters(CoProcessor.CoProcessorWriteHandler in_write_handler)
    {
      m_status_code = CoProcessorConstants.ERR_BUSY;
      m_write_data_index = 0;
      CoProc.CoProcessorWrite = in_write_handler;
    }


    private void SetReadResult(CoProcessor.CoProcessorReadHandler in_read_handler)
    {
      m_read_function_to_set = in_read_handler;
      CoProc.CoProcessorRead = CoProcessorReadResult;
    }


    /// <summary>
    /// Ends current function
    /// </summary>
    /// <param name="in_result_code"></param>
    public void EndFunction(byte in_result_code)
    {
      CoProc.Status = in_result_code;
      CoProc.CoProcessorRead = null;
      CoProc.CoProcessorWrite = null;
    }

    /// <summary>
    /// Expends filename with full path and extension
    /// </summary>
    /// <param name="in_filename">TVC filename</param>
    /// <returns>Full path unicode filename</returns>
    private string GetFilePath(string in_filename)
    {
      string filename = TVCCharacterCodePage.TVCStringToUNICODEString(in_filename);

      if (string.IsNullOrEmpty(Path.GetExtension(filename)))
      {
        filename = filename + ".CAS";
      }

      string path = Path.Combine(CoProc.Cart.Settings.FilesystemFolder, filename);

      return path;
    }
  }
}
