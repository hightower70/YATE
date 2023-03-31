///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2019-2021 Laszlo Arvai. All rights reserved.
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
// NanoSD Card onboard CPU emulator class
///////////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using System.Text;

namespace NanoSD
{
  class ArduinoCPU
  {
    #region · Constants ·

    private int BufferLength = 128;
    private int GETDATA_SIZE = 64;

    private readonly string FirmwareVersionString = "NanoSD Arduino fw v0.31";

    #endregion

    #region · Types · 

    /// <summary>
    /// Arduino CPU commands used by TVC software
    /// </summary>
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

    /// <summary>
    /// Status byte values, returned from Arduino to TVC
    /// </summary>
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

    /// <summary>
    /// Get/Set parameter, parameter types
    /// </summary>
    private enum PARAMETERS : byte
    {
      PARAM_MENU_STATUS = 0,
      PARAM_DEFAULT_SORT,
      PARAM_SD_STATE,
      PARAM_NANO_VERSION
    };

    /// <summary>
    /// Handled file types
    /// </summary>
    private enum FILETYPE : byte
    {
      DIRECTORY = 0,
      PLAINFILE
    };

    #endregion

    #region · Data members ·

    private NanoSDCard m_parent;

    private STATUS m_status;

    private byte[] m_output_buffer;
    private int m_output_pos;
    private int m_output_length;

    private byte m_pending_command;
    private byte[] m_input_buffer;
    private int m_input_pos;

    private string m_current_dir;
    private FileSystemInfo[] m_directory;
    private int m_directory_index;

    private bool m_file_system_changed;
    private string m_current_changed_file;

    private BinaryReader m_file_read;
    private BinaryWriter m_file_write;

    private int m_bytes_written;
    private byte m_write_file_type;

    #endregion

    #region · Constructor ·

    /// <summary>
    /// Default constructor. Creates NanoSD Arduino CPU function emulation class.
    /// </summary>
    /// <param name="in_parent"></param>
    public ArduinoCPU(NanoSDCard in_parent)
    {
      m_parent = in_parent;

      m_output_buffer = new byte[BufferLength];
      m_output_pos = 0;
      m_output_length = 0;

      m_input_buffer = new byte[BufferLength];
      m_input_pos = -1;

      m_current_dir = "/";

      m_file_system_changed = false;
    }

    #endregion

    #region · Host communication functions ·

    /// <summary>
    /// Reads byte from the Arduino CPU
    /// </summary>
    /// <returns></returns>
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
          return (byte)STATUS.BUFFER_UNDERRUN;
        }
      }
    }

    /// <summary>
    /// Write byte to the Arduino CPU
    /// </summary>
    /// <param name="in_byte"></param>
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
            CmdChDir();
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
            CmdCreate();
            break;

          case COMMANDS.PUTDATA:
            CmdPutData();
            break;

          case COMMANDS.CLOSEWRITE:
            CmdCloseForWrite();
            break;

          case COMMANDS.GETPARAMETER:
            CmdGetParameter();
            break;

          case COMMANDS.SETPARAMETER:
            CmdSetParameter();
            break;

          case COMMANDS.MKDIR:
            CmdMkDir();
            break;

          case COMMANDS.RMDIR:
            CmdRmDir();
            break;

          case COMMANDS.DELETE:
            CmdDelete();
            break;

          case COMMANDS.CDPINCHANGE_DETECTED:
            //handleCDPinChangeDetected();
            break;

          default:
            ResetInputBuffer();
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

    #endregion

    #region · Arduino CPU Commands ·

    /// <summary>
    /// Command: opens file for read
    /// </summary>
    private void CmdOpen()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
      {
        // close file if already opened
        if (m_file_read != null)
        {
          m_file_read.Close();
          m_file_read = null;
        }
        return;
      }

      // check end of filename
      if (m_input_buffer[m_input_pos - 1] == 0)
      {
        SetStatus(STATUS.DATA_NOT_READY_YET);

        string full_file_name;

        // if no file name specified
        if (m_input_buffer[0] == 0)
        {
          // get the first CAS file
          string[] files = Directory.GetFiles(GetAbsoluteFolderPath(), "*.cas");

          if (files.Length > 0)
          {
            full_file_name = Path.Combine(GetAbsoluteFolderPath(), files[0]);
          }
          else
          {
            full_file_name = "";
          }
        }
        else
        {
          full_file_name = Path.Combine(GetAbsoluteFolderPath(), Encoding.ASCII.GetString(m_input_buffer, 0, m_input_pos - 1));
        }

        // Add cas if there is no extension
        if (!File.Exists(full_file_name))
        {
          if (Path.GetExtension(full_file_name).Length == 0)
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

    /// <summary>
    /// Command: read data from the opened read file
    /// </summary>
    private void CmdGetData()
    {
      if (m_file_read == null)
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

    /// <summary>
    /// Command: close read file
    /// </summary>
    private void CmdClose()
    {
      if (m_file_read != null)
      {
        m_file_read.Close();
        m_file_read = null;
      }

      ResetInputBuffer();

      SetStatus(STATUS.CLOSE_OK);
    }

    /// <summary>
    /// Command: creates file for write
    /// </summary>
    private void CmdCreate()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
      {
        if (m_file_write != null)
        {
          m_file_write.Close();
          m_file_write = null;

          return;
        }
      }

      int expected_length = 1 + m_input_buffer[0] + 1; // file name length + file name + file type
      if (m_input_pos < expected_length)
        return;

      m_write_file_type = m_input_buffer[m_input_buffer[0] + 1];

      string file_name = Encoding.ASCII.GetString(m_input_buffer, 1, m_input_buffer[0]);

      // default name if no file name specified
      if (file_name.Length == 0)
        file_name = "NONAME.CAS";

      // add extension if not specified
      string ext = Path.GetExtension(file_name);
      if (string.IsNullOrEmpty(ext))
        file_name += ".CAS";
      else
      {
        if (string.Compare(ext, ".CAS", true) != 0)
        {
          m_write_file_type = 0;
        }
      }

      string absolute_file_name = GetAbsouteFilePath(file_name);

      // create file
      m_current_changed_file = absolute_file_name;
      m_file_write = new BinaryWriter(File.Open(absolute_file_name, FileMode.Create, FileAccess.ReadWrite, FileShare.Read));
      m_current_changed_file = null;
      if (m_file_write != null)
      {
        m_bytes_written = 0;

        // Write CAS header
        if ((m_write_file_type == 0x01) || (m_write_file_type == 0x11))
        {
          m_file_write.Write(m_write_file_type);

          for (int i = 1; i < 128; i++)
            m_file_write.Write((byte)0x00);

          m_bytes_written += 128;
        }

        SetStatus(STATUS.CREATE_OK);
      }
      else
      {
        SetStatus(STATUS.CREATE_FAILED);
      }

      ResetInputBuffer();
      ResetOutputBuffer();
    }

    /// <summary>
    /// Command: write data to the write opened file
    /// </summary>
    private void CmdPutData()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
        return;

      int expected_length = m_input_buffer[0] + 1; // data packet length + data packet
      if (m_input_pos < expected_length)
        return;

      int bytes_to_write = m_input_buffer[0];

      if (m_file_write == null)
      {
        SetStatus(STATUS.DATA_FAILED_TO_RECEIVE);
      }
      else
      {
        try
        {
          m_file_write.Write(m_input_buffer, 1, bytes_to_write);

          m_bytes_written += bytes_to_write;

          SetStatus(STATUS.DATA_RECEIVED);
        }
        catch
        {
          SetStatus(STATUS.DATA_FAILED_TO_RECEIVE);
        }
      }

      ResetInputBuffer();
      ResetOutputBuffer();
    }

    /// <summary>
    /// Command: Close write opened file
    /// </summary>
    private void CmdCloseForWrite()
    {
      if (m_file_write == null)
      {
        SetStatus(STATUS.CLOSE_FAILED);
        return;
      }

      // Update CAS header
      if ((m_write_file_type == 0x01) || (m_write_file_type == 0x11))
      {
        int lastBlock = m_bytes_written % 128;

        m_file_write.Seek(2, SeekOrigin.Begin);
        m_file_write.Write((byte)((m_bytes_written / 128) % 256));
        m_file_write.Write((byte)((m_bytes_written / 128) / 256));
        m_file_write.Write((byte)(lastBlock));
      }

      m_file_write.Close();
      m_file_write = null;

      SetStatus(STATUS.CLOSE_OK);

      ResetInputBuffer();
      ResetOutputBuffer();
    }

    /// <summary>
    /// Command: get parameter
    /// </summary>
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

    /// <summary>
    /// Command: set parameter
    /// </summary>
    private void CmdSetParameter()
    {
      if (m_input_pos < 2)
        return;

      byte param = m_input_buffer[0];
      switch ((PARAMETERS)param)
      {
        case PARAMETERS.PARAM_MENU_STATUS:
          m_parent.Settings.MenuStatus = (m_input_buffer[1] != 0);
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

    /// <summary>
    /// Command: get current directory
    /// </summary>
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

    /// <summary>
    /// Command: bank select
    /// </summary>
    /// <param name="in_bank"></param>
    private void CmdBankSelect(int in_bank)
    {
      if (in_bank == 0)
        m_parent.SetMemoryPage(0);
      else
        m_parent.SetMemoryPage(1);

      SetStatus(STATUS.BANK_SELECT_DONE);

      ResetInputBuffer();
    }

    /// <summary>
    /// Command: list first directory entry
    /// </summary>
    private void CmdListFirst()
    {
      string path = GetAbsoluteFolderPath();

      DirectoryInfo di = new DirectoryInfo(path);
      m_directory = di.GetFileSystemInfos();

      m_directory_index = 0;

      StoreDirectoryBlock();

      ResetInputBuffer();
    }

    /// <summary>
    /// Command: list next directory entry
    /// </summary>
    private void CmdListNext()
    {
      m_directory_index++;

      StoreDirectoryBlock();

      ResetInputBuffer();
    }

    /// <summary>
    /// Command: change directory
    /// </summary>
    private void CmdChDir()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
        return;

      // check end of directory name
      if (m_input_buffer[m_input_pos - 1] == 0)
      {
        SetStatus(STATUS.DATA_NOT_READY_YET); // no use in this case but looks fancy

        string path = GetParameterAbsoluteFilePath();

        if (path.StartsWith(m_parent.Settings.FilesystemFolder) && Directory.Exists(path))
        {
          string current_dir = path.Remove(0, m_parent.Settings.FilesystemFolder.Length).Replace("\\", "/");

          if (!current_dir.StartsWith("/"))
            current_dir = "/" + current_dir;

          m_current_dir = current_dir;

          SetStatus(STATUS.CHDIR_OK);
        }
        else
        {
          SetStatus(STATUS.CHDIR_FAILED);
        }

        ResetInputBuffer();
      }
    }

    /// <summary>
    /// Command: create directory
    /// </summary>
    private void CmdMkDir()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
        return;

      // check end of directory name
      if (m_input_buffer[m_input_pos - 1] == 0)
      {
        SetStatus(STATUS.DATA_NOT_READY_YET); // no use inthis case but looks fancy

        string path = GetParameterAbsoluteFilePath();

        if (Directory.Exists(path))
        {
          SetStatus(STATUS.MKDIR_FAILED);
        }
        else
        {
          try
          {
            m_current_changed_file = path;
            Directory.CreateDirectory(path);
            SetStatus(STATUS.MKDIR_OK);
            m_current_changed_file = null;
          }
          catch
          {
            SetStatus(STATUS.MKDIR_FAILED);
          }
        }

        ResetInputBuffer();
      }
    }

    /// <summary>
    /// Command: remove directory
    /// </summary>
    private void CmdRmDir()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
        return;

      // check end of directory name
      if (m_input_buffer[m_input_pos - 1] == 0)
      {
        SetStatus(STATUS.DATA_NOT_READY_YET);

        string path = GetParameterAbsoluteFilePath();

        if (!Directory.Exists(path))
        {
          SetStatus(STATUS.RMDIR_FAILED);
        }
        else
        {
          try
          {
            m_current_changed_file = path;
            Directory.Delete(path);
            SetStatus(STATUS.RMDIR_OK);
            m_current_changed_file = null;
          }
          catch
          {
            SetStatus(STATUS.RMDIR_FAILED);
          }
        }

        ResetInputBuffer();
      }
    }

    /// <summary>
    /// Command: delete file
    /// </summary>
    private void CmdDelete()
    {
      SetStatus(STATUS.WAITING_FOR_INPUT);

      if (m_input_pos < 1)
        return;

      // check end of directory name
      if (m_input_buffer[m_input_pos - 1] == 0)
      {
        SetStatus(STATUS.DATA_NOT_READY_YET);

        string path = GetParameterAbsoluteFilePath();

        if (!File.Exists(path))
        {
          SetStatus(STATUS.DELETE_FAILED);
        }
        else
        {
          try
          {
            m_current_changed_file = path;
            File.Delete(path);
            SetStatus(STATUS.DELETE_OK);
            m_current_changed_file = null;
          }
          catch
          {
            SetStatus(STATUS.DELETE_FAILED);
          }
        }

        ResetInputBuffer();
      }
    }


    #endregion

    #region · Helper functions ·

    public void FileSystemChanged(string in_changed_file)
    {
      // skip currently handled file
      if (!string.IsNullOrEmpty(m_current_changed_file) && string.Compare(m_current_changed_file, in_changed_file) == 0)
        return;

      m_file_system_changed = true;
    }

    /// <summary>
    /// Resets command buffer
    /// </summary>
    private void ResetInputBuffer()
    {
      m_input_pos = -1;
    }

    /// <summary>
    /// Resets result buffer
    /// </summary>
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

    /// <summary>
    /// Sets status code
    /// </summary>
    /// <param name="in_status"></param>
    private void SetStatus(STATUS in_status)
    {
      m_status = in_status;
    }

    /// <summary>
    /// Stores one byte in the result buffer
    /// </summary>
    /// <param name="in_byte"></param>
    private void StoreByte(byte in_byte)
    {
      if (m_output_pos < BufferLength)
      {
        m_output_buffer[m_output_pos++] = in_byte;
      }
    }

    private bool FilesystemIsReady()
    {
      bool file_system_is_ready = Directory.Exists(m_parent.Settings.FilesystemFolder);

      if (m_file_system_changed && m_directory == null && m_file_read == null)
      {
        file_system_is_ready = false;
      }

      return file_system_is_ready;
    }

    private string GetParameterAbsoluteFilePath()
    {
      string parameter_path = Encoding.ASCII.GetString(m_input_buffer, 0, m_input_pos - 1);
      return GetAbsouteFilePath(parameter_path);
    }

    /// <summary>
    /// Gets absolute path of a file
    /// </summary>
    /// <param name="in_path"></param>
    /// <returns></returns>
    private string GetAbsouteFilePath(string in_path)
    {
      string current_path;

      // start path from root
      if (in_path.StartsWith("/"))
      {
        current_path = "";
        in_path.Remove(0, 1);
      }
      else
      {
        current_path = m_current_dir;
        if (current_path.StartsWith("/"))
          current_path = current_path.Remove(0, 1);

        if (current_path.Length != 0 && !current_path.EndsWith("/"))
          current_path += '/';
      }

      string[] path_elements = in_path.Split('/');
      for (int i = 0; i < path_elements.Length; i++)
      {
        if (path_elements[i] == ".")
          continue;

        if (path_elements[i] == "..")
        {
          int index = -1;

          if (current_path.Length > 1)
            index = current_path.LastIndexOf('/', current_path.Length - 2);

          if (index == -1)
            current_path = "";
          else
          {
            current_path = current_path.Remove(index + 1, current_path.Length - (index + 1));
          }

          continue;
        }

        if (!string.IsNullOrEmpty(current_path))
          current_path += '/';

        current_path += path_elements[i];
      }

      current_path = current_path.Replace('/', '\\');

      current_path = Path.Combine(m_parent.Settings.FilesystemFolder, current_path);

      return current_path;
    }

    private string GetAbsoluteFolderPath()
    {
      string file_path = m_current_dir;

      if (file_path.StartsWith("/"))
        file_path = file_path.Remove(0, 1);

      file_path = Path.Combine(m_parent.Settings.FilesystemFolder, file_path);

      return file_path;
    }

    private void StoreDirectoryBlock()
    {
      if (m_directory_index < m_directory.Length)
      {
        ResetOutputBuffer();

        StoreByte((byte)STATUS.OK);

        FileSystemInfo file_info = m_directory[m_directory_index];

        string name = Path.GetFileNameWithoutExtension(file_info.Name);
        string ext = file_info.Extension;
        if (ext.StartsWith("."))
          ext = ext.Remove(0, 1);

        bool is_directory = (file_info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

        int length = 0;
        if (file_info is FileInfo)
          length = (UInt16)((file_info as FileInfo).Length);

        if (name.Length > 8)
          name = name.Remove(8);

        if (ext.Length > 3)
          ext = ext.Remove(3);

        string filename = name;
        if (!string.IsNullOrEmpty(ext))
          filename += "." + ext;

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
        if (is_directory)
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
          StoreByte((byte)((length >> 8) & 0xff));
          StoreByte((byte)((length >> 16) & 0xff));
          StoreByte((byte)((length >> 24) & 0xff));
        }

        SendOuputBuffer(STATUS.OK);
      }
      else
      {
        m_directory = null;
        ResetOutputBuffer();
        ResetInputBuffer();
        SetStatus(STATUS.END_OF_LIST);
      }
    }

    #endregion
  }
}
