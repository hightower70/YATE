using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using YATECommon;
using YATECommon.Helpers;

namespace HBF
{
  public class HBFCard : ITVCCard
  {
    private struct TrackReadElements
    {
      public byte Count;
      public byte Data;

      public TrackReadElements(byte in_count, byte in_data)
      {
        Count = in_count;
        Data = in_data;
      }
    }

    private const int CardROMSize = 16384;
    private const int CardRAMSize = 4096;
    private const int CardROMPageSize = 4096;

    private const int NumberOfDrives = 4;
    private const int InvalidDriveIndex = -1;

    private readonly int IndexPulsePeriod = 200000; // Index pulse period in microsec
    private readonly int IndexPulseWidth = 4000; // Index pulse width in microsec

    private readonly ulong DataTransferSpeed = 250000; // data transfer speed in byte/sec

    private const int AddressLength = 6;

    private const UInt32 TrackIDADDMark = 0xf5f5f5fe;   // Address mark for track write command
    private const UInt32 TrackDATAMark = 0xf5f5f5fb;    // Data mark for track write command

    // Register addresses
    private const int PORT_COMMAND = 0;
    private const int PORT_STATUS = 0;
    private const int PORT_TRACK = 1;
    private const int PORT_SECTOR = 2;
    private const int PORT_DATA = 3;
    private const int PORT_HWSTATUS = 4;
    private const int PORT_PARAM = 4;
    private const int PORT_PAGE = 8;

    private readonly int[] SteppingDelays = { 6, 12, 20, 30 };

    private readonly TrackReadElements[] m_track_read_elements =
    {
      new TrackReadElements(80, 0x4e), // GAP
      new TrackReadElements(12, 0x00), // SYNC
      new TrackReadElements( 3, 0xc2), // IAM
      new TrackReadElements( 1, 0xfc), // IAM
      new TrackReadElements(50, 0x4e), // GAP1

      new TrackReadElements(12, 0x00), // SYNC
      new TrackReadElements( 3, 0xa1), // IDAM
      new TrackReadElements( 1, 0xfe), // IDAM
      new TrackReadElements( 0, 0x01), // CYL
      new TrackReadElements( 0, 0x02), // HD
      new TrackReadElements( 0, 0x03), // SEC
      new TrackReadElements( 0, 0x04), // LEN
      new TrackReadElements( 0, 0x10), // CRC1
      new TrackReadElements( 0, 0x11), // CRC2
      new TrackReadElements(22, 0x4e), // GAP2
      new TrackReadElements(12, 0x00), // SYNC
      new TrackReadElements( 3, 0xa1), // DAM
      new TrackReadElements( 1, 0xFB), // DAM
      new TrackReadElements( 0, 0x20), // DATA
      new TrackReadElements( 0, 0x10), // CRC1
      new TrackReadElements( 0, 0x11), // CRC2
      new TrackReadElements(80, 0x4e), // GAP

      new TrackReadElements(0,0)
    };

    private readonly byte[] m_address_buffer = new byte[AddressLength];

    /// <summary>
    /// Internal operation status
    /// </summary>
    private enum OperationState
    {
      None,

      Seek,

      // ID field read
      IDReadWaitForIndex,
      IDRead,

      // Sector read
      SectorRead,

      // Track write
      TrackWriteWaitForIndex,
      TrackWriteGap,
      TrackWriteIDADDR,
      TrackWriteData,

      // Sector write
      WriteSector

    }

    [Flags]
    enum CommandFlags : byte
    {
      DELMARK = 0x01,
      SIDECOMP = 0x02,
      STEPRATE = 0x03,
      VERIFY = 0x04,
      WAIT15MS = 0x04,
      LOADHEAD = 0x08,
      SIDE = 0x08,
      IRQ = 0x08,
      SETTRACK = 0x10,
      MULTIREC = 0x10
    }

    /// <summary>
    /// Hardware status flag (external flag pins register)
    /// </summary>
    [Flags]
    enum HardwareFlags : byte
    {
      DRQ = 0x80,          // Data request flag
      INT = 0x01           // Interrupt flag
    }

    /// <summary>
    /// Force interrupt command flags
    /// </summary>
    [Flags]
    enum ForceInterruptFlags: byte
    {
      NOT_READY_TO_READY = 0x01,
      READY_TO_NOT_READY = 0x02,
      INDEX_PULSE = 0x04,
      IMMEDIATE_INTERRUPT = 0x08
    }


    /// <summary>
    /// Parameter register bits
    /// </summary>
    [Flags]
    enum ParametersFlags : byte
    {
      DriveSelect0 = 0x01,
      DriveSelect1 = 0x02,
      DriveSelect2 = 0x04,
      DriveSelect3 = 0x08,

      DriveSelectMask = DriveSelect0 | DriveSelect1 | DriveSelect2 | DriveSelect3,

      HeadLoad = 0x10,
      DoubleDensityEnabled = 0x20,
      MotorOn = 0x40,
      SideSelect = 0x80
    }

    /// <summary>
    /// Internal status register bits
    /// </summary>
    [Flags]
    enum StatusFlags : byte
    {
      // Common status bits:
      BUSY = 0x01,          // Controller is executing a command
      WRITEPROTECT = 0x40,  // The disk is write-protected
      NOTREADY = 0x80,      // The drive is not ready

      // Type-1 command status:
      INDEX = 0x02,         // Index mark detected
      TRACK0 = 0x04,        // Head positioned at track #0
      CRCERR = 0x08,        // CRC error in ID field
      SEEKERR = 0x10,       // Seek error, track not verified
      HEADLOAD = 0x20,      // Head loaded

      // Type-2 and Type-3 command status:
      DRQ = 0x02,           // Data request pending
      LOSTDATA = 0x04,      // Data has been lost (missed DRQ)
      ERRCODE = 0x18,       // Error code bits:
      BADDATA = 0x08,       // 1 = bad data CRC
      NOTFOUND = 0x10,      // 2 = sector not found
      BADID = 0x18,         // 3 = bad ID field CRC
      DELETED = 0x20,       // Deleted data mark (when reading)
      WRFAULT = 0x20        // Write fault (when writing)
    }

    private ITVComputer m_tvcomputer;

    // CRC calculator
    private CRC16 m_crc_generator;

    // Timing values
    private ulong m_index_pulse_period;
    private ulong m_index_pulse_width;

    /// <summary>FD1793 registers</summary>
    private StatusFlags m_fdc_status;
    private byte m_fdc_status_mode = 0;
    private byte m_fdc_command;
    private byte m_fdc_track;
    private byte m_fdc_sector;
    private byte m_fdc_data;
    private bool m_fdc_reset_state;
    private byte m_fdc_last_step_direction;
    private bool m_prev_index_pulse_state;

    /// <summary>HBU card registers</summary>
    private HardwareFlags m_reg_hw_status;
    private ParametersFlags m_reg_param;
    private byte m_reg_page;

    /// <summary>Virtual disk images</summary>
    private DiskDrive[] m_disk_drives;
    private int m_current_drive_index;

    /// <summary>Pending status data</summary>
    private ulong m_pending_delay = 0;
    private ulong m_operation_start_tick = 0;
    private StatusFlags m_pending_status = 0;
    private HardwareFlags m_pending_hw_status = 0;
    private byte m_pending_track = 0;

    private OperationState m_operation_state;

    private int m_data_count = 0;
    private int m_data_length = 0;

    /// <summary>Track write wariables</summary>
    private UInt32 m_track_write_id_buffer;

    /// <summary>Force interrupt mode</summary>
    private ForceInterruptFlags m_force_interrupt;

    /// <summary>Pending status data</summary>
    private bool m_fast_operation = false;

    // Card memory
    private byte[] m_card_rom;
    private byte[] m_card_ram;

    private bool m_head_loaded;

    private HBFCardSettings m_settings;

    #region · Properties ·
    #endregion


    public HBFCard()
    {
      Reset();

      // CRC calculator init
      m_crc_generator = new CRC16(0x1021, 0xffff);

      // reserve memory
      m_card_ram = new byte[CardRAMSize];
      m_card_rom = new byte[CardROMSize];

      // create disk drives
      m_disk_drives = new DiskDrive[NumberOfDrives];

      for (int i = 0; i < NumberOfDrives; i++)
      {
        m_disk_drives[i] = new DiskDrive();
      }

      //m_disk_drives[0] = new DiskDrive();
      m_disk_drives[0].Geometry.NumberOfTracks = 80;
      m_disk_drives[0].Geometry.NumberOfSides = 2;
      m_disk_drives[0].Geometry.SectorPerTrack = 9;
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\mralex.dsk");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\IKPLUS_TVC.dsk");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\test.dsk");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\UPM_TEST.dsk");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\UPM_test1.xdi");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\nautilus.dsk");
      m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\upmlemez.img");

      //m_disk_drives[1] = new DiskDrive();
      m_disk_drives[1].Geometry.NumberOfTracks = 40;
      m_disk_drives[1].Geometry.NumberOfSides = 2;
      m_disk_drives[1].Geometry.SectorPerTrack = 9;
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\mralex.dsk");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\IKPLUS_TVC.dsk");
      m_disk_drives[1].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\test.dsk");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\UPM_TEST.dsk");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\UPM_test1.xdi");
      //m_disk_drives[0].OpenDiskImageFile(@"d:\Projects\Retro\YATE\disk\nautilus.dsk");
    }



    public void SetSettings(HBFCardSettings in_settings)
    {
      m_settings = in_settings;

      // load ROM content
      switch (m_settings.ROMType)
      {
        // customn version
        case 0:
          break;

        // UPM
        case 1:
          LoadCardRomFromResource("HBF.Resources.UPM-DISK.ROM");
          break;

        // VT-DOS 1.1
        case 2:
          LoadCardRomFromResource("HBF.Resources.VT-DOS11-DISK.ROM");
          break;

        // VT-DOS 1.2
        case 3:
          LoadCardRomFromResource("HBF.Resources.VT-DOS12-DISK.ROM");
          break;
      }
    }

    /// <summary>
    /// Installs HBF card to the system
    /// </summary>
    /// <param name="in_tvcomputer"></param>
    public void Install(ITVComputer in_tvcomputer)
    {
      m_tvcomputer = in_tvcomputer;

      // set timing
      m_index_pulse_period = in_tvcomputer.MicrosecToCPUTicks(IndexPulsePeriod);
      m_index_pulse_width = in_tvcomputer.MicrosecToCPUTicks(IndexPulseWidth);
    }

    /// <summary>
    /// Removes card from the system
    /// </summary>
    public void Remove(ITVComputer in_parent)
    {
      // no action needed
    }

    /// <summary>
    /// Loads ROM content of the card from the given file
    /// </summary>
    /// <param name="in_file_name">File name containing the binary rom content</param>
		public void LoadCardRom(string in_file_name)
    {
      byte[] data = File.ReadAllBytes(in_file_name);

      Array.Copy(data, 0, m_card_rom, 0, data.Length);
    }

    /// <summary>
    /// Loads card ROM content from the given resource file
    /// </summary>
    /// <param name="in_resource_name"></param>
    public void LoadCardRomFromResource(string in_resource_name)
    {
      // load default key mapping
      Assembly assembly = Assembly.GetExecutingAssembly();

      using (Stream stream = assembly.GetManifestResourceStream(in_resource_name))
      {
        using (BinaryReader binary_reader = new BinaryReader(stream))
        {
          byte[] data = binary_reader.ReadBytes((int)stream.Length);

          int byte_to_copy = data.Length;

          if (byte_to_copy > m_card_rom.Length)
            byte_to_copy = m_card_rom.Length;

          Array.Copy(data, 0, m_card_rom, 0, byte_to_copy);
        }
      }
    }

    /// <summary>
    /// Memory read function for card mamory
    /// </summary>
    /// <param name="in_address">Address of the memory</param>
    /// <returns>Byte from the card memory</returns>
		public byte MemoryRead(ushort in_address)
    {
      if (in_address < CardROMPageSize)
        return m_card_rom[in_address + ((m_reg_page >> 4) & 0x03) * CardROMPageSize];
      else
        return m_card_ram[in_address - CardROMPageSize];
    }

    /// <summary>
    /// Writes card memory
    /// </summary>
    /// <param name="in_address">Addres of the memory</param>
    /// <param name="in_byte">Data to write</param>
		public void MemoryWrite(ushort in_address, byte in_byte)
    {
      if (in_address < CardROMPageSize)
        return; // no ROM write
      else
        m_card_ram[in_address - CardROMPageSize] = in_byte;
    }

    /// <summary>
    /// Gets ID byte of the card
    /// </summary>
    /// <returns></returns>
		public byte GetID()
    {
      return 0x02;
    }

    /// <summary>
    /// Hardware reset of the card
    /// </summary>
		public void Reset()
    {
      // reset card
      m_reg_page = 0;
      m_reg_param = 0;
      m_fdc_reset_state = true;
      m_current_drive_index = 0;

      // reset FDC1793
      m_fdc_status = 0;
      m_fdc_status_mode = 1;
      m_fdc_command = 0;
      m_fdc_track = 0;
      m_fdc_sector = 0;
      m_fdc_data = 0;
      m_fdc_last_step_direction = 0;

      m_head_loaded = false;

      m_data_count = 0;
      m_data_length = 0;

      m_operation_state = OperationState.None;
    }

    /** Read1793() ***********************************************/
    /** Read value from a WD1793 register A. Returns read data  **/
    /** on success or 0xFF on failure (bad register address).   **/
    /*************************************************************/
    public void PortRead(ushort in_address, ref byte inout_data)
    {
      switch (in_address & 0x0f)
      {
        // read status register
        case PORT_STATUS:
          if (m_fdc_reset_state)
            return;

          // If no disk present, NOTREADY
          if (IsDriveReady())
            m_fdc_status &= ~StatusFlags.NOTREADY;
          else
            m_fdc_status |= StatusFlags.NOTREADY;

          // When reading status, clear interrupt
          m_reg_hw_status &= ~(HardwareFlags.INT);

          // in status mode 0 handle index pulses
          switch (m_fdc_status_mode)
          {
            // Status mode I
            case 1:
              {
                // index pulse
                if (IsIndexPulse())
                  m_fdc_status |= StatusFlags.INDEX;
                else
                  m_fdc_status &= ~StatusFlags.INDEX;

                // track 0 bit 
                if (m_current_drive_index == InvalidDriveIndex)
                {
                  m_fdc_status &= ~StatusFlags.TRACK0;
                }
                else
                {
                  if (m_disk_drives[m_current_drive_index].Track == 0)
                    m_fdc_status |= StatusFlags.TRACK0;
                  else
                    m_fdc_status &= ~StatusFlags.TRACK0;
                }

                // head loaded bit
                if (m_head_loaded)
                  m_fdc_status |= StatusFlags.HEADLOAD;
                else
                  m_fdc_status &= ~StatusFlags.HEADLOAD;
              }
              break;


            // Status mode II
            case 2:
              {
                // update hw status register
                UpdateHardwareStatus();

                // copy DRQ bit from HW register
                if ((m_reg_hw_status & HardwareFlags.DRQ) != 0)
                  m_fdc_status |= StatusFlags.DRQ;
                else
                  m_fdc_status &= ~StatusFlags.DRQ;
              }
              break;

            // Status mode III
            case 3:
              {
                // update hw status register
                UpdateHardwareStatus();

                // copy DRQ bit from HW register
                if ((m_reg_hw_status & HardwareFlags.DRQ) != 0)
                  m_fdc_status |= StatusFlags.DRQ;
                else
                  m_fdc_status &= ~StatusFlags.DRQ;
              }
              break;
          }

          // return status
          inout_data = (byte)m_fdc_status;

          return;

        // return track register
        case PORT_TRACK:
          if (m_fdc_reset_state)
            return;

          inout_data = m_fdc_track;

          return;

        // return sector register
        case PORT_SECTOR:
          if (m_fdc_reset_state)
            return;

          inout_data = m_fdc_sector;

          return;

        // return data register
        case PORT_DATA:
          switch (m_operation_state)
          {
            case OperationState.IDRead:
            case OperationState.SectorRead:
              m_reg_hw_status &= ~HardwareFlags.DRQ;
              break;
          }

          inout_data = m_fdc_data;

          return;

        // return HW stzatus register
        case PORT_HWSTATUS:
          UpdateHardwareStatus();
          inout_data = (byte)m_reg_hw_status;
          return;
      }
    }

    /** Write1793() **********************************************/
    /** Write value V into WD1793 register A. Returns DRQ/IRQ   **/
    /** values.                                                 **/
    /*************************************************************/
    public void PortWrite(ushort in_address, byte in_value)
    {
      if ((in_address & 0x0f) != 8)
      {
        if ((in_address & 0x0f) == 3)
        {
          //Debug.Write(in_value.ToString("X2") + " ");
        }
        else
        {
          Debug.Write((in_address & 0x0f).ToString("X2") + ":" + in_value.ToString("X2") + " = ");
        }
      }

      switch (in_address & 0x0f)
      {
        // command address
        case PORT_COMMAND:
          m_fdc_command = in_value;

          // Reset interrupt request
          m_reg_hw_status &= ~(HardwareFlags.INT);

          // If it is FORCE-IRQ command...
          if ((in_value & 0xF0) == 0xD0)
          {
            m_force_interrupt = (ForceInterruptFlags)(in_value & 0x0f);

            Debug.WriteLine("Force interrupt: {0}", m_force_interrupt);

            // Reset any executing command
            m_data_length = 0;
            m_data_count = 0;
            m_operation_state = OperationState.None;

            // Either reset BUSY flag or reset all flags if BUSY=0
            if ((m_fdc_status & StatusFlags.BUSY) != 0)
              m_fdc_status &= ~StatusFlags.BUSY;
            else
            {
              m_fdc_status = m_disk_drives[m_current_drive_index].Track == 0 ? StatusFlags.TRACK0 : 0;
              m_fdc_status_mode = 1;
            }

            // Cause immediate interrupt if requested
            if ((in_value & (byte)CommandFlags.IRQ) != 0)
              m_reg_hw_status = HardwareFlags.INT;

            // Done
            return;
          }

          // If busy, drop out
          if ((m_fdc_status & StatusFlags.BUSY) != 0)
            break;

          // Reset status register
          m_fdc_status = 0x00;
          m_reg_hw_status = 0x00;

          // Depending on the command...
          switch (in_value & 0xF0)
          {
            // RESTORE (seek track 0)
            case 0x00:
              Debug.WriteLine("Restore");

              // command group I
              m_fdc_status_mode = 1;

              // set head load
              m_head_loaded = ((in_value & (byte)CommandFlags.LOADHEAD) != 0);

              m_fdc_last_step_direction = 0;

              // if already at track zero
              if (m_disk_drives[m_current_drive_index].Track == 0)
              {
                m_fdc_status |= StatusFlags.TRACK0;
                m_reg_hw_status |= HardwareFlags.INT;
                m_fdc_track = 0;
                m_operation_state = OperationState.None;
              }
              else
              {
                // not on the first track -> start operation
                StartOperation((m_disk_drives[m_current_drive_index].Geometry.NumberOfTracks / 2) * SteppingDelays[in_value & (byte)CommandFlags.STEPRATE], StatusFlags.TRACK0 | (StatusFlags)(((in_value & (byte)CommandFlags.LOADHEAD) != 0) ? StatusFlags.HEADLOAD : 0), HardwareFlags.INT, 0);
              }
              break;

            // SEEK command
            case 0x10:
              Debug.WriteLine("Seek: {0:x2}", m_fdc_data);

              // command group I
              m_fdc_status_mode = 1;

              // set head load
              m_head_loaded = ((in_value & (byte)CommandFlags.LOADHEAD) != 0);

              if (m_fdc_data > m_fdc_track)
                m_fdc_last_step_direction = 0;
              else
                m_fdc_last_step_direction = 0x20;

              // Reset any executing command
              m_data_count = 0;
              m_data_length = 0;

              StartOperation(Math.Abs((int)m_fdc_data - (int)m_fdc_track) * SteppingDelays[in_value & (byte)CommandFlags.STEPRATE], (StatusFlags)(((in_value & (byte)CommandFlags.LOADHEAD) != 0) ? StatusFlags.HEADLOAD : 0), HardwareFlags.INT, m_fdc_data);
              break;

            case 0x20: // STEP
            case 0x30: // STEP-AND-UPDATE
            case 0x40: // STEP-IN
            case 0x50: // STEP-IN-AND-UPDATE
            case 0x60: // STEP-OUT
            case 0x70: // STEP-OUT-AND-UPDATE
              {
                Debug.Write(string.Format("Step: {0:x2}", (in_value & 0xF0)));

                // command group I
                m_fdc_status_mode = 1;

                // set head load
                m_head_loaded = ((in_value & (byte)CommandFlags.LOADHEAD) != 0);

                // Either store or fetch step direction
                if ((in_value & 0x40) != 0)
                  m_fdc_last_step_direction = (byte)(in_value & 0x20);
                else
                  in_value = (byte)((in_value & ~0x20) | m_fdc_last_step_direction);

                // Step the head, update track register if requested 
                byte target_track = m_disk_drives[m_current_drive_index].Track;
                if ((in_value & 0x20) != 0)
                {
                  if (m_disk_drives[m_current_drive_index].Track > 0)
                    target_track--;
                }
                else
                {
                  if (target_track < m_disk_drives[m_current_drive_index].Geometry.NumberOfTracks - 1)
                    target_track++;
                }
                Debug.WriteLine(" Track: {0}", target_track);

                //m_disk_drives[m_current_drive_index].Track = target_track;

                // Update track register if requested
                StatusFlags new_status = 0;
                if ((in_value & (byte)CommandFlags.SETTRACK) != 0)
                {
                  if (target_track >= m_disk_drives[m_current_drive_index].Geometry.NumberOfTracks)
                    new_status = StatusFlags.SEEKERR;

                  m_fdc_track = m_disk_drives[m_current_drive_index].Track;
                }

                StartOperation(SteppingDelays[in_value & (byte)CommandFlags.STEPRATE], new_status, HardwareFlags.INT, target_track);
              }
              break;

            // Track write
            case 0xF0:
              Debug.Write("TrackWrite ");
              // command group III
              m_fdc_status_mode = 3;

              m_operation_start_tick = m_tvcomputer.GetCPUTicks();
              m_operation_state = OperationState.TrackWriteWaitForIndex;
              m_prev_index_pulse_state = IsIndexPulse();

              m_fdc_status = StatusFlags.BUSY;
              m_reg_hw_status = HardwareFlags.DRQ;

              break;

            // Sector read
            case 0x80:  // single sector read
            case 0x90:  // multiple sector read
              Debug.WriteLine("Sector read, T:{0:d}, S:{1:d}, H:{2:d}", m_fdc_track, m_fdc_sector, GetCurrentDriveSide());

              // check drive
              if (m_current_drive_index == InvalidDriveIndex || !m_disk_drives[m_current_drive_index].IsDiskPresent())
              {
                m_fdc_status = StatusFlags.NOTREADY;
              }
              else
              {
                // check sector and track address
                if (m_fdc_track != m_disk_drives[m_current_drive_index].Track || m_fdc_sector < 1 || m_fdc_sector > m_disk_drives[m_current_drive_index].Geometry.SectorPerTrack)
                {
                  m_fdc_status = StatusFlags.NOTFOUND;
                  m_reg_hw_status = HardwareFlags.INT;
                  m_operation_state = OperationState.None;
                  m_fdc_status_mode = 2;
                  m_data_length = 0;
                }
                else
                {
                  m_disk_drives[m_current_drive_index].Track = m_fdc_track;

                  m_data_count = 0;
                  m_data_length = m_disk_drives[m_current_drive_index].Geometry.SectorLength * (((in_value & 0x10) != 0) ? m_disk_drives[m_current_drive_index].Geometry.SectorPerTrack - m_fdc_sector + 1 : 1);
                  m_operation_start_tick = m_tvcomputer.GetCPUTicks();
                  m_operation_state = OperationState.SectorRead;
                  m_fdc_status = StatusFlags.BUSY;
                  m_fdc_status_mode = 2;
                  m_head_loaded = true;

                  m_disk_drives[m_current_drive_index].SeekSector(m_fdc_sector, GetCurrentDriveSide());
                }
              }
              break;
#if false
						case 0xA0:
						case 0xB0: /* WRITE-SECTORS */
							if (D->Verbose) printf("WD1793: WRITE-SECTOR%s %c:%d:%d:%d (%02Xh)\n", V & 0x10 ? "S" : "", 'A' + D->Drive, D->Side, D->R[1], D->R[2], V);
							/* Seek to the requested sector */
							D->Ptr = SeekFDI(
								D->Disk[D->Drive], D->Side, D->Track[D->Drive],
								V & C_SIDECOMP ? !!(V & C_SIDE) : D->Side, D->R[1], D->R[2]
							);
							/* If seek successful, set up writing operation */
							if (!D->Ptr)
							{
								if (D->Verbose) printf("WD1793: WRITE ERROR\n");
								D->R[0] = (D->R[0] & ~F_ERRCODE) | F_NOTFOUND;
								m_irq_pending = true;;
							}
							else
							{
								m_wr_length = D->Disk[D->Drive]->SecSize
														* (V & 0x10 ? (D->Disk[D->Drive]->Sectors - D->R[2] + 1) : 1);
								D->R[0] |= F_BUSY | F_DRQ;
								D->IRQ = WD1793_DRQ;
								D->Wait = 255;
							}
							break;
#endif
            // Read address
            case 0xC0:
              Debug.WriteLine("Read address", in_value);

              m_crc_generator.Reset();
              m_address_buffer[0] = GetCurrentDriveTrack();
              m_address_buffer[1] = GetCurrentDriveSide();
              m_address_buffer[2] = 1;
              m_address_buffer[3] = GetCurrentSectorLength();
              m_crc_generator.Add(m_address_buffer, 4);
              m_address_buffer[4] = m_crc_generator.CRCLow;
              m_address_buffer[5] = m_crc_generator.CRCHigh;
              m_data_count = 0;
              m_data_length = 6;
              m_fdc_sector = GetCurrentDriveTrack();

              m_prev_index_pulse_state = IsIndexPulse();
              m_operation_state = OperationState.IDReadWaitForIndex;
              m_fdc_status_mode = 3;
              m_head_loaded = true;
              break;

#if false
							/* Read first sector address from the track */
							if (!D->Disk[D->Drive]) D->Ptr = 0;
							else
								for (J = 0; J < 256; ++J)
								{
									D->Ptr = SeekFDI(
										D->Disk[D->Drive],
										D->Side, D->Track[D->Drive],
										D->Side, D->Track[D->Drive], J
									);
									if (D->Ptr) break;
								}
							/* If address found, initiate data transfer */
							if (!D->Ptr)
							{
								if (D->Verbose) printf("WD1793: READ-ADDRESS ERROR\n");
								D->R[0] |= F_NOTFOUND;
								m_irq_pending = true;;
							}
							else
							{
								D->Ptr = D->Disk[D->Drive]->Header;
								m_rd_length = 6;
								D->R[0] |= F_BUSY | F_DRQ;
								D->IRQ = WD1793_DRQ;
								D->Wait = 255;
							}
							break;
#endif
#if false
						case 0xE0: /* READ-TRACK */
							if (D->Verbose) printf("WD1793: READ-TRACK %d (%02Xh) UNSUPPORTED!\n", D->R[1], V);
							break;

						case 0xF0: /* WRITE-TRACK */
							if (D->Verbose) printf("WD1793: WRITE-TRACK %d (%02Xh) UNSUPPORTED!\n", D->R[1], V);
							break;

						default: /* UNKNOWN */
							if (D->Verbose) printf("WD1793: UNSUPPORTED OPERATION %02Xh!\n", V);
							break;
#endif
          }
          break;

        // track register
        case PORT_TRACK:
          Debug.WriteLine("Track register set: {0:x2}", in_value);
          if ((m_fdc_status & StatusFlags.BUSY) == 0 && !m_fdc_reset_state)
            m_fdc_track = in_value;
          break;

        // sector register
        case PORT_SECTOR:
          Debug.WriteLine("Sector register set: {0:x2}", in_value);
          if ((m_fdc_status & StatusFlags.BUSY) == 0 && !m_fdc_reset_state)
            m_fdc_sector = in_value;
          break;

        case PORT_DATA:
          m_fdc_data = in_value;

          switch (m_operation_state)
          {
            case OperationState.TrackWriteGap:
              m_track_write_id_buffer = (m_track_write_id_buffer << 8) | in_value;
              break;
          }

          m_reg_hw_status &= ~HardwareFlags.DRQ;

          /*
              if ((m_reg_hw_status & HardwareFlags.DRQ) == 0)
              {
                m_track_write_id_buffer = (m_track_write_id_buffer << 8) | in_value;
              }
              else
              {
                // data overrun occured
                m_reg_hw_status = HardwareFlags.INT;
                m_fdc_status |= StatusFlags.LOSTDATA;
                m_operation_state = OperationState.None;
              }
          */
          break;

#if false
					// check track write mode
					if (m_read_write_mode != ReadWriteMode.TrackWrite)
					{
						TrackWriteData(in_value);
					}
					else
					{

						/* When writing data, store value to disk */
          if (m_wr_length > 0)
						{
							Debug.WriteLine(string.Format("WD1793: EXTRA DATA WRITE (%02Xh)\n", in_value));
						}
						else
						{
						/* Write data */
						*D->Ptr++ = V;
						/* Decrement length */
						if (--m_wr_length > 0)
						{
							/* Reset timeout watchdog */
							D->Wait = 255;
							/* Advance to the next sector as needed */
							if (!(m_wr_length & (D->Disk[D->Drive]->SecSize - 1))) ++D->R[2];
						}
						else
						{
							/* Write completed */
							if (D->Verbose) printf("WD1793: WRITE COMPLETED\n");
							D->R[0] &= ~(F_DRQ | F_BUSY);
							m_irq_pending = true; ;
						}

			}
		}

					// Save last written value
					m_fdc_data = in_value;
#endif


        // parameter register
        case PORT_PARAM:
          Debug.WriteLine("Param register set: {0}", (ParametersFlags)in_value);
          m_reg_param = (ParametersFlags)in_value;

          switch (m_reg_param & ParametersFlags.DriveSelectMask)
          {
            case ParametersFlags.DriveSelect0:
              m_current_drive_index = 0;
              break;

            case ParametersFlags.DriveSelect1:
              m_current_drive_index = 1;
              break;

            case ParametersFlags.DriveSelect2:
              m_current_drive_index = 2;
              break;

            case ParametersFlags.DriveSelect3:
              m_current_drive_index = 3;
              break;

            default:
              m_current_drive_index = InvalidDriveIndex;
              break;
          }
          break;

        // page register
        case PORT_PAGE:
          m_reg_page = in_value;
          m_fdc_reset_state = false;

          break;
      }
    }

    /// <summary>
    /// Starts head moving operaton in emulated speed (lengthty) or fast mode (immediate).
    /// </summary>
    /// <param name="in_delay_us">Operation length in us</param>
    /// <param name="in_new_status_flags">New status flag after the operation is complete</param>
    /// <param name="in_new_hardware_flags">New hardware status flag after the operation is complete</param>
    /// <param name="in_new_track">Track register value after the operation is complete</param>
		private void StartOperation(int in_delay_us, StatusFlags in_new_status_flags, HardwareFlags in_new_hardware_flags, byte in_new_track)
    {
      if (m_fast_operation)
      {
        // no delay -> immediately execute the operation
        m_fdc_status = in_new_status_flags;
        m_reg_hw_status = in_new_hardware_flags;
        m_fdc_track = in_new_track;

        m_operation_state = OperationState.None;
      }
      else
      {
        // start delaying operation
        m_fdc_status |= StatusFlags.BUSY;

        m_pending_status = in_new_status_flags;
        m_pending_hw_status = in_new_hardware_flags;
        m_pending_track = in_new_track;

        m_pending_delay = m_tvcomputer.MicrosecToCPUTicks(in_delay_us);

        m_operation_state = OperationState.Seek;
      }
    }

    public void PeriodicCallback(ulong in_cpu_tick)
    {
      switch (m_operation_state)
      {
        // Type I operation (seek)
        case OperationState.Seek:
          if (m_tvcomputer.GetTicksSince(m_operation_start_tick) > m_pending_delay)
          {
            // operation delay time is expired
            m_fdc_status = m_pending_status;
            m_reg_hw_status = m_pending_hw_status;
            m_fdc_track = m_pending_track;

            m_disk_drives[m_current_drive_index].Track = m_pending_track;

            m_operation_state = OperationState.None;
          }
          break;
      }
    }

    private bool IsDriveReady()
    {
      return m_current_drive_index != InvalidDriveIndex && m_disk_drives[m_current_drive_index].IsDiskPresent();
    }

    private bool IsIndexPulse()
    {
      // index pulse
      ulong index_ticks = m_tvcomputer.GetCPUTicks() % m_index_pulse_period;

      return index_ticks > m_index_pulse_width;
    }

    private void UpdateHardwareStatus()
    {
      switch (m_operation_state)
      {
        case OperationState.TrackWriteWaitForIndex:
          {
            bool index_pulse_state;

            index_pulse_state = IsIndexPulse();

            if (m_prev_index_pulse_state == false && index_pulse_state == true)
            {
              // index pulse rising edge
              m_operation_state = OperationState.TrackWriteGap;
              m_operation_start_tick = m_tvcomputer.GetCPUTicks();
              m_data_count = 0;
              m_reg_hw_status |= HardwareFlags.DRQ;
            }
            m_prev_index_pulse_state = index_pulse_state;
          }
          break;

        case OperationState.TrackWriteGap:
          {
            if(TrackWriteByteProcess())
            {
              switch(m_track_write_id_buffer)
              {
                case TrackIDADDMark:
                  m_data_length = m_data_count;
                  m_operation_state = OperationState.TrackWriteIDADDR;
                  break;

                case TrackDATAMark:
                  m_data_length = m_data_count;
                  m_operation_state = OperationState.TrackWriteData;
                  break;
              }
            }
          }
          break;

        case OperationState.TrackWriteIDADDR:
          {
            if(TrackWriteByteProcess())
            {
              switch (m_data_count - m_data_length)
              {
                // sector value
                case 3:
                  m_disk_drives[m_current_drive_index].SeekSector(m_fdc_data, GetCurrentDriveSide());
                  break;

                case 5:
                  m_operation_state = OperationState.TrackWriteGap;
                  break;
              }
            }
          }
          break;

        case OperationState.TrackWriteData:
          {
            if (TrackWriteByteProcess())
            {
              if (m_data_count - m_data_length <= m_disk_drives[m_current_drive_index].Geometry.SectorLength)
              {
                m_disk_drives[m_current_drive_index].WriteByte(m_fdc_data);
              }
              else
              {
                m_operation_state = OperationState.TrackWriteGap;
              }
            }
          }
          break;

        case OperationState.SectorRead:
          {
            int byte_index = TickToByteIndex(m_tvcomputer.GetTicksSince(m_operation_start_tick));

            if (byte_index > m_data_count)
            {
              m_fdc_data = m_disk_drives[m_current_drive_index].ReadByte();
              m_data_count++;
              m_reg_hw_status |= HardwareFlags.DRQ;

              if (m_data_count > m_data_length)
              {
                // stop operation
                m_data_length = 0;
                m_operation_state = OperationState.None;
                m_fdc_status &= ~StatusFlags.BUSY;
                m_reg_hw_status |= HardwareFlags.INT;
                //m_fdc_status_mode = 1;
              }
            }
          }
          break;

        case OperationState.IDReadWaitForIndex:
          {
            bool index_pulse_state = IsIndexPulse();

            if (m_prev_index_pulse_state == false && index_pulse_state == true)
            {
              // index pulse rising edge
              m_operation_state = OperationState.IDRead;
              m_operation_start_tick = m_tvcomputer.GetCPUTicks();
            }

            m_prev_index_pulse_state = index_pulse_state;
          }
          break;

        case OperationState.IDRead:
          if (m_data_count < m_data_length)
          {
            m_fdc_data = m_address_buffer[m_data_count];
            m_data_count++;
            m_reg_hw_status |= HardwareFlags.DRQ;
          }
          else
          {
            m_operation_state = OperationState.None;
            m_reg_hw_status |= HardwareFlags.INT;
          }
          break;

        default:
          {
            //int by
          }
          break;
      }
    }

    private bool TrackWriteByteProcess()
    {
      // stop operation at the next index pulse 
      bool index_pulse_state = IsIndexPulse();
      if (index_pulse_state != m_prev_index_pulse_state && index_pulse_state)
      {
        m_operation_state = OperationState.None;
        m_fdc_status &= ~StatusFlags.BUSY;
        m_reg_hw_status |= HardwareFlags.INT;
        //m_fdc_status_mode = 1;
        Debug.WriteLine(" END");
      }
      m_prev_index_pulse_state = index_pulse_state;

      int byte_count = TickToByteIndex(m_tvcomputer.GetTicksSince(m_operation_start_tick));

      if (byte_count > m_data_count)
      {
        m_data_count++;
        m_reg_hw_status |= HardwareFlags.DRQ;

        return true;
      }

      return false;
    }

    /// <summary>
    /// Converts CPU tick count to transferred byte count
    /// </summary>
    /// <param name="in_tick_count"></param>
    /// <returns></returns>
    private int TickToByteIndex(ulong in_tick_count)
    {
      return (int)(in_tick_count * DataTransferSpeed / 8ul / (ulong)m_tvcomputer.CPUClock);
    }

    /// <summary>
    /// Gets the selected side of the current drive
    /// </summary>
    /// <returns>Selected side: 0 or 1</returns>
		private byte GetCurrentDriveSide()
    {
      return (byte)((m_reg_param & ParametersFlags.SideSelect) != 0 ? 1 : 0);
    }

    /// <summary>
    /// Gets the head position (track) of the currently selected drive
    /// </summary>
    /// <returns>Track number</returns>
		private byte GetCurrentDriveTrack()
    {
      if (m_current_drive_index == InvalidDriveIndex)
        return 0;
      else
        return m_disk_drives[m_current_drive_index].Track;
    }

    /// <summary>
    /// Gets the physical sector length code of the current drive
    /// </summary>
    /// <returns>Sector length code</returns>
		private byte GetCurrentSectorLength()
    {
      int sector_length = 512;

      if (m_current_drive_index != -1)
        sector_length = m_disk_drives[m_current_drive_index].Geometry.SectorLength;

      switch (sector_length)
      {
        case 1024:
          return 3;

        case 512:
          return 2;

        case 256:
          return 1;

        case 128:
          return 0;

        default:
          return 0;
      }
    }
  }
}
