using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoSD
{
  class ArduinoCPU
  {
    private int BufferLength = 128;
    private int GETDATA_SIZE = 64;

    private readonly string FirmwareVersionString = "NanoSD Arduino fw v0.31";

    private enum COMMANDS : byte
    {
      OPEN = 0, // Open a file. Parameter is a null terminated string. Returns 0: ok
      CLOSE,   // Closes the lastopen file. Returns OK
      CHDIR,   // Change dir. Parameter is a null terminated string. Returns 0:ok, 1:FILE_NOT_FOUND
      LIST,    // Gets the list of files in the current directory (fName, type, size)
      ACK_LIST, //
      GETDATA,   // Returns OK, then [num of bytes to return (GETDATA_SIZE)]. After num of bytes read TVC must send ACK_GETDATA;
      FINFO,   // Returns the file info. Param: null terminated file name.
      GETCDIR,  // Returns the current path: length of string + string
      BANKTO0,  // NOTIMPLEMENTED YET!
      BANKTO1,
      CREATE, // 0x0a gets a fileName (length + chars), fileType (0x01 or 0x11), return CREATE_OK of CREATE_FAILED
      PUTDATA,
      CLOSEWRITE,
      GETPARAMETER, // 0x0D gets a parameter from Nano
      SETPARAMETER, // 0x0E sets a parameter to Nano
      MKDIR,
      RMDIR,  // 0x10 - removes an empty directory from SDs
      DELETE,
      CDPINCHANGE_DETECTED,
      NONE = 255
    };

    private enum STATUS : byte
    {
      OK = 0,
      OPEN_OK,
      DATA_NOT_READY_YET = 0x80,  // not sent from outputQueue
      WAITING_FOR_INPUT,
      FILE_NOT_FOUND,
      END_OF_FILE,
      END_OF_LIST,
      FILE_NOT_OPEN,              // 0x85
      CARD_NOT_READY,
      READ_OUT_OF_SYNC,
      BUFFER_OVERRUN,
      BUFFER_UNDERRUN,
      CLOSE_OK,                     // 0x8a
      CLOSE_FAILED,
      CHDIR_OK,
      CHDIR_FAILED,                 // 0x8d
      BANK_SELECT_DONE,
      CREATE_OK,
      CREATE_FAILED,                // 0x90
      DATA_RECEIVED,
      DATA_FAILED_TO_RECEIVE,
      MKDIR_OK,                     // 0x93
      MKDIR_FAILED,
      RMDIR_OK,                     // 0x95
      RMDIR_FAILED,
      DELETE_OK,
      DELETE_FAILED,                // 0x98
      INVALID_PARAMETER_ID,
      PARAMETER_SET,                 // 0x9
      STATUS_NO_SD = 0xFF
    };

    private enum PARAMETERS : byte
    {
      PARAM_MENU_STATUS = 0,
      PARAM_DEFAULT_SORT,
      PARAM_SD_STATE,
      PARAM_NANO_VERSION
    };

    private enum FILETYPE : byte
    {
      DIRECTORY = 0,
      PLAINFILE
    };

    private delegate void ReadBlockFinishedCallback();

    private NanoSDCard m_parent;

    private STATUS m_status;

    private byte[] m_output_buffer;
    private int m_output_pos;
    private int m_output_length;

    private byte m_pending_command;
    private byte[] m_input_buffer;
    private int m_input_pos;

    private string m_current_dir;
    private FileInfo[] m_directory;
    private int m_directory_index;

    private ReadBlockFinishedCallback m_read_block_finished;

    private bool m_file_system_changed;

    private BinaryReader m_file_read;

    public ArduinoCPU(NanoSDCard in_parent)
    {
      m_parent = in_parent;

      m_output_buffer = new byte[BufferLength];
      m_output_pos = 0;
      m_output_length = 0;

      m_input_buffer = new byte[BufferLength];
      m_input_pos = -1;

      m_current_dir = "/";

      m_read_block_finished = null;

      m_file_system_changed = false;
    }

    public byte ReadByte()
    {
      if ((((byte)m_status) & 0x80) != 0)
      {
        return (byte)m_status;
      }
      else
      {
        if (m_output_pos == -1)
        {
          m_output_pos = 0;
          return (byte)m_status;
        }

        if (m_output_pos < m_output_length)
        {
          return m_output_buffer[m_output_pos++];
        }
        else
        {
          if (m_read_block_finished != null)
          {
            m_read_block_finished();
            return (byte)STATUS.DATA_NOT_READY_YET;
          }
          else
          {
            return (byte)STATUS.BUFFER_UNDERRUN;
          }
        }
      }
    }

    public void WriteByte(byte in_byte)
    {
      // store input data
      if (m_input_pos == -1)
      {
        m_pending_command = in_byte;
        m_input_pos = 0;
        m_status = STATUS.DATA_NOT_READY_YET;
        ResetOutputBuffer();
      }
      else
      {
        m_input_buffer[m_input_pos] = in_byte;
        if (m_input_pos < BufferLength - 1)
        {
          m_input_pos++;
        }
      }

      if (FilesystemIsReady())
      {
        // process command bytes
        switch ((COMMANDS)m_pending_command)
        {
          case COMMANDS.OPEN:
            CmdOpen();
            break;

          case COMMANDS.CLOSE:
            CmdClose();
            break;

          case COMMANDS.GETDATA:
            CmdGetData();
            break;

          case COMMANDS.LIST:
            CmdListFirst();
            break;

          case COMMANDS.ACK_LIST:
            CmdListNext();
            break;

          case COMMANDS.CHDIR:
            //handleChDir();
            break;

          case COMMANDS.FINFO:
            //handleFileInfo();
            break;

          case COMMANDS.GETCDIR:
            CmdGetCDir();
            break;

          case COMMANDS.BANKTO0:
            CmdBankSelect(0);
            break;
          case COMMANDS.BANKTO1:
            CmdBankSelect(1);
            break;

          case COMMANDS.CREATE:
            //handleCreate();
            break;

          case COMMANDS.PUTDATA:
            //handlePutData();
            break;

          case COMMANDS.CLOSEWRITE:
            //handleCloseForWrite();
            break;

          case COMMANDS.GETPARAMETER:
            CmdGetParameter();
            break;

          case COMMANDS.SETPARAMETER:
            CmdSetParameter();
            break;

          case COMMANDS.MKDIR:
            //handleMkDir();
            break;

          case COMMANDS.RMDIR:
            //handleRmDir();
            break;

          case COMMANDS.DELETE:
            //handleDelete();
            break;

          case COMMANDS.CDPINCHANGE_DETECTED:
            //handleCDPinChangeDetected();
            break;
        }
      }
      else
      {
        switch ((COMMANDS)m_pending_command)
        {
          case COMMANDS.GETPARAMETER:
            CmdGetParameter();
            break;

          case COMMANDS.CDPINCHANGE_DETECTED:
            //handleCDPinChangeDetected();
            break;

          default:
            SetStatus(STATUS.STATUS_NO_SD);
            ResetInputBuffer();
            break;
        }
      }
    }

    public void FileSystemChanged()
    {
      m_file_system_changed = true;
    }


    private void ResetInputBuffer()
    {
      m_input_pos = -1;
    }

    private void ResetOutputBuffer()
    {
      m_output_length = 0;
      m_output_pos = 0;
    }

    private void SendOuputBuffer()
    {
      m_output_length = m_output_pos;
      m_output_pos = 0;
    }

    private void SendOuputBuffer(STATUS in_status)
    {
      m_status = in_status;
      m_output_length = m_output_pos;
      m_output_pos = -1;
    }

    private void SetStatus(STATUS in_status)
    {
      m_status = in_status;
    }

    private void StoreByte(byte in_byte)
    {
      if (m_output_pos < BufferLength)
      {
        m_output_buffer[m_output_pos++] = in_byte;
      }
    }

    private bool FilesystemIsReady()
    {
      bool file_system_is_ready =  Directory.Exists(m_parent.Settings.FilesystemFolder);

      if (m_file_system_changed && m_directory == null && m_file_read == null)
      {
        file_system_is_ready = false;
      }

      return file_system_is_ready;
    }

    private string GetFilePath()
    {
      string file_path = m_current_dir;

      if (file_path.StartsWith("/"))
        file_path = file_path.Remove(0, 1);

      file_path = Path.Combine(m_parent.Settings.FilesystemFolder, file_path);

      return file_path;
    }

    private void CmdOpen()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
        return;

      // check end of filename
      if (m_input_buffer[m_input_pos - 1] == 0)
      {
        SetStatus(STATUS.DATA_NOT_READY_YET);

        string full_file_name;

        // if no file name specified
        if (m_input_buffer[0] == 0)
        {
          // get the first CAS file
          string[] files = Directory.GetFiles(GetFilePath(), "*.cas");

          if(files.Length>0)
          {
            full_file_name = Path.Combine(GetFilePath(), files[0]);
          }
          else
          {
            full_file_name = "";
          }
        }
        else
        {
          full_file_name = Path.Combine(GetFilePath(), Encoding.ASCII.GetString(m_input_buffer, 0, m_input_pos - 1));
        }

        // Add cas if there is no extension
        if(!File.Exists(full_file_name))
        {
          if(Path.GetExtension(full_file_name).Length== 0)
          {
            full_file_name += ".CAS";
          }
        }

        if (File.Exists(full_file_name))
        {
          m_file_read = new BinaryReader(File.Open(full_file_name, FileMode.Open, FileAccess.Read, FileShare.Read));

          if (string.Compare(Path.GetExtension(full_file_name), ".CAS", true) == 0)
          {
            m_file_read.BaseStream.Seek(128, SeekOrigin.Begin);
          }

          // return file name
          string file_name = Path.GetFileName(full_file_name);

          StoreByte((byte)file_name.Length);
          for (int i = 0; i < file_name.Length; i++)
          {
            StoreByte((byte)file_name[i]);
          }

          
          // get length
          long length = m_file_read.BaseStream.Length - m_file_read.BaseStream.Position;

          StoreByte((byte)(length & 0xff));
          StoreByte((byte)((length >> 8) & 0xff));
          StoreByte((byte)((length >> 16) & 0xff));
          StoreByte((byte)((length >> 24) & 0xff));

          SendOuputBuffer(STATUS.OPEN_OK);
        }
        else
        {
          SetStatus(STATUS.FILE_NOT_FOUND);
        }

        ResetInputBuffer();
      }
    }

    private void CmdGetData()
    {
      if(m_file_read == null)
      {
        SetStatus(STATUS.FILE_NOT_OPEN);
        ResetInputBuffer();
        return;
      }

      byte[] buffer = m_file_read.ReadBytes(GETDATA_SIZE);

      StoreByte((byte)buffer.Length);

      for (int i = 0; i < buffer.Length; i++)
      {
        StoreByte(buffer[i]);
      }

      if (buffer.Length == 0)
        SendOuputBuffer(STATUS.END_OF_FILE);
      else
        SendOuputBuffer(STATUS.OK);

      ResetInputBuffer();
    }

    private void CmdClose()
    {
      if(m_file_read != null)
      {
        m_file_read.Close();
        m_file_read = null;
      }

      ResetInputBuffer();

      SetStatus(STATUS.CLOSE_OK);
    }

    private void CmdGetParameter()
    {
      if (m_input_pos < 1)
        return;

      byte param = m_input_buffer[0];
      switch ((PARAMETERS)param)
      {
        case PARAMETERS.PARAM_MENU_STATUS:
          StoreByte(1);
          StoreByte((byte)((m_parent.Settings.MenuStatus) ? 1 : 0));
          SendOuputBuffer(STATUS.OK);
          break;

        case PARAMETERS.PARAM_DEFAULT_SORT:
          StoreByte(1); // byte
          StoreByte((byte)m_parent.Settings.SortMode);
          SendOuputBuffer(STATUS.OK);
          break;

        case PARAMETERS.PARAM_SD_STATE:
          {
            StoreByte(1); // byte
            StoreByte((byte)((FilesystemIsReady()) ? 1 : 0));
            SendOuputBuffer(STATUS.OK);
            m_file_system_changed = false;
          }
          break;

        case PARAMETERS.PARAM_NANO_VERSION:
          StoreByte(3); // string
          for (int i = 0; i < FirmwareVersionString.Length; i++)
          {
            StoreByte((byte)FirmwareVersionString[i]);
          }
          StoreByte(0);
          SendOuputBuffer(STATUS.OK);
          break;

        default:
          SetStatus(STATUS.INVALID_PARAMETER_ID);
          break;
      }

      ResetInputBuffer();
    }

    private void CmdSetParameter()
    {
      if (m_input_pos < 2)
        return;

      byte param = m_input_buffer[0];
      switch ((PARAMETERS)param)
      {
        case PARAMETERS.PARAM_MENU_STATUS:
          m_parent.Settings.MenuStatus = (m_input_buffer[1] == 0);
          SetStatus(STATUS.PARAMETER_SET);
          m_parent.StoreSettings();
          break;

        case PARAMETERS.PARAM_DEFAULT_SORT:
          m_parent.Settings.SortMode = m_input_buffer[1];
          SetStatus(STATUS.PARAMETER_SET);
          m_parent.StoreSettings();
          break;

        default:
          SetStatus(STATUS.INVALID_PARAMETER_ID);
          break;
      }

      ResetInputBuffer();
    }

    private void CmdGetCDir()
    {
      StoreByte((byte)m_current_dir.Length);
      for (int i = 0; i < m_current_dir.Length; i++)
      {
        StoreByte((byte)m_current_dir[i]);
      }

      SendOuputBuffer(STATUS.OK);

      ResetInputBuffer();
    }

    private void CmdBankSelect(int in_bank)
    {
      if (in_bank == 0)
        m_parent.ROMHighAddress = 0xc000;
      else
        m_parent.ROMHighAddress = 0xc200;

      SetStatus(STATUS.BANK_SELECT_DONE);

      ResetInputBuffer();
    }

    private void CmdListFirst()
    {
      string path = GetFilePath();

      DirectoryInfo di = new DirectoryInfo(path);
      m_directory = di.GetFiles();

      m_directory_index = 0;

      StoreDirectoryBlock();

      ResetInputBuffer();
    }

    private void CmdListNext()
    {
      m_directory_index++;

      StoreDirectoryBlock();

      ResetInputBuffer();
    }

    private void StoreDirectoryBlock()
    {
      if(m_directory_index < m_directory.Length)
      {
        ResetOutputBuffer();

        StoreByte((byte)STATUS.OK);

        FileInfo file_info = m_directory[m_directory_index];

        string name = Path.GetFileNameWithoutExtension(file_info.Name);
        string ext = Path.GetExtension(file_info.Name).Remove(0, 1);
        bool is_directory = (file_info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        int length = (UInt16)file_info.Length;

        if (name.Length > 8)
          name = name.Remove(8);

        if (ext.Length > 3)
          ext = ext.Remove(3);

        string filename = name + "." + ext;

        // store name
        for (int i = 0; i < 12; i++)
        {
          if (i < filename.Length)
            StoreByte((byte)filename[i]);
          else
            StoreByte((byte)' ');
        }

        // store type
        if (is_directory)
          StoreByte((byte)FILETYPE.DIRECTORY);
        else
          StoreByte((byte)FILETYPE.PLAINFILE);

        // store length
        if(is_directory)
        {
          StoreByte(0);
          StoreByte(0);
          StoreByte(0);
          StoreByte(0);
        }
        else
        {
          if (string.Compare(ext, "CAS", true) == 0)
            length -= 128;

          StoreByte((byte)(length & 0xff));
          StoreByte((byte)((length>>8) & 0xff));
          StoreByte((byte)((length>>16) & 0xff));
          StoreByte((byte)((length>>24) & 0xff));
        }

        SendOuputBuffer(STATUS.OK);
      }
      else
      {
        m_directory = null;
        m_read_block_finished = null;
        ResetOutputBuffer();
        ResetInputBuffer();
        SetStatus(STATUS.END_OF_LIST);
      }
    }
  }
}
