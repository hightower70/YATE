using System;
using System.Diagnostics;
using System.IO;

namespace TVCHardware
{
	public class HBFCard		 : ITVCCard
	{

		private int NumberOfDrives = 4;
		private int InvalidDriveIndex = -1;

		// Register addresses
		private const int PORT_COMMAND = 0;
		private const int PORT_STATUS = 0;
		private const int PORT_TRACK = 1;
		private const int PORT_SECTOR = 2;
		private const int PORT_DATA = 3;
		private const int PORT_HWSTATUS = 4;
		private const int PORT_PARAM = 4;
		private const int PORT_PAGE = 8;

		[Flags]
		enum StatusFlags : byte
		{
			// Common status bits:
			F_BUSY = 0x01, // Controller is executing a command
			F_ = 0x40, // The disk is write-protected
			F_NOTREADY = 0x80, // The drive is not ready

			// Type-1 command status:
			F_INDEX = 0x02, // Index mark detected
			F_TRACK0 = 0x04, // Head positioned at track #0
			F_CRCERR = 0x08, // CRC error in ID field
			F_SEEKERR = 0x10, // Seek error, track not verified
			F_HEADLOAD = 0x20, // Head loaded

			// Type-2 and Type-3 command status:
			F_DRQ = 0x02, // Data request pending
			F_LOSTDATA = 0x04, // Data has been lost (missed DRQ)
			F_ERRCODE = 0x18, // Error code bits:
			F_BADDATA = 0x08, // 1 = bad data CRC
			F_NOTFOUND = 0x10, // 2 = sector not found
			F_BADID = 0x18, // 3 = bad ID field CRC
			F_DELETED = 0x20, // Deleted data mark (when reading)
			F_WRFAULT = 0x20 // Write fault (when writing)
	}

		/// <summary>FD1793 registers</summary>
		private byte m_fdc_status;
		private byte m_fdc_command;
		private byte m_fdc_track;
		private byte m_fdc_sector;
		private byte m_fdc_data;
		private bool m_fdc_reset_state;

		/// <summary>HBU card registers</summary>
		private byte m_reg_hw_status;
		private byte m_reg_param;
		private byte m_reg_page;

		/// <summary>Virtual disk images</summary>
		private DiskDrive[] m_disk_drives;
		private int m_current_drive_index;


		private bool m_irq_pending = false;
		private int m_rd_length = 0;
		private int m_wr_length = 0;

		private byte[] m_card_rom;
		private byte[] m_card_ram;

		private const int CardROMSize = 16384;
		private const int CardRAMSize = 4096;
		private const int CardROMPageSize = 4096;

		public HBFCard()
		{
			// reserve memory
			m_card_ram = new byte[CardRAMSize];
			m_card_rom = new byte[CardROMSize];

			// create disk drives
			m_disk_drives = new DiskDrive[NumberOfDrives];

			for (int i = 0; i < NumberOfDrives; i++)
			{
				m_disk_drives[i] = new DiskDrive();
			}

			LoadCardRom(@"..\..\roms\VT-DOS12-DISK.ROM");
		}


		public void LoadCardRom(string in_file_name)
		{
			byte[] data = File.ReadAllBytes(in_file_name);

			Array.Copy(data, 0, m_card_rom, 0, data.Length);
		}

		public byte CardMemoryRead(ushort in_address)
		{
			if (in_address < CardROMPageSize)
				return m_card_rom[in_address + ((m_reg_page >> 4)  & 0x03) * CardROMPageSize];
			else
				return m_card_ram[in_address - CardROMPageSize];
		}

		public void CardMemoryWrite(ushort in_address, byte in_byte)
		{
			if (in_address < CardROMPageSize)
				return; // no ROM write
			else
				m_card_ram[in_address - CardROMPageSize] = in_byte;
		}

		public void Reset()
		{
			m_reg_page = 0;
			m_reg_param = 0;
			m_fdc_reset_state = true;
			m_current_drive_index = 0;
			FD1793Reset();
		}

		public byte GetCardID()
		{
			return 0x02;
		}

		private void FD1793Reset()
		{
			int J;

			/*
		D->R[0] = 0x00;
		D->R[1] = 0x00;
		D->R[2] = 0x00;
		D->R[3] = 0x00;
		D->R[4] = S_RESET | S_HALT;
		D->Drive = 0;
		D->Side = 0;
		D->LastS = 0;
		m_irq_pending = false;;
		m_wr_length = 0;
		m_rd_length = 0;	*/
		}

		/** Read1793() ***********************************************/
		/** Read value from a WD1793 register A. Returns read data  **/
		/** on success or 0xFF on failure (bad register address).   **/
		/*************************************************************/
		public void CardPortRead(ushort in_address, ref byte inout_data)
		{
			switch (in_address & 0x0f)
			{
				// read status register
				case PORT_STATUS:
					if (m_fdc_reset_state)
						return;

					// If no disk present, set F_NOTREADY
					if (m_current_drive_index == InvalidDriveIndex  || !m_disk_drives[m_current_drive_index].IsDiskPresent())
						m_fdc_status |= (byte)StatusFlags.F_NOTREADY;

					// When reading status, clear all bits but F_BUSY and F_NOTREADY
					m_fdc_status &= (byte)(StatusFlags.F_BUSY | StatusFlags.F_NOTREADY);

					inout_data = m_fdc_status;

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

					inout_data =  m_fdc_sector;

					return;

				case PORT_DATA:
					// When reading data, load value from disk
					if (m_rd_length > 0)
					{
						Debug.WriteLine("WD1793: EXTRA DATA READ");
					}
					else
					{
						// Read data
						m_fdc_data = m_disk_drives[m_current_drive_index].ReadByte();

						// Decrement length
						m_rd_length--;
						if (m_rd_length != 0)
						{
							// Reset timeout watchdog
							//D->Wait = 255;

							// Advance to the next sector if needed
							if ((m_rd_length & (m_disk_drives[m_current_drive_index].Geometry.SectorLength - 1)) == 0)
								m_fdc_sector++;
						}
						else
						{
							// Read completed
							m_fdc_status &= (byte)(~(StatusFlags.F_DRQ | StatusFlags.F_BUSY));
							m_irq_pending = true;
						}
					}

					inout_data = m_fdc_data;

					return;

				case PORT_HWSTATUS:
					inout_data = m_reg_hw_status;
					return;
			}
		}

		/** Write1793() **********************************************/
		/** Write value V into WD1793 register A. Returns DRQ/IRQ   **/
		/** values.                                                 **/
		/*************************************************************/
		public void CardPortWrite(ushort in_address, byte in_value)
		{
			int J;
			switch (in_address & 0x0f)
			{
#if false
				// command address
				case PORT_COMMAND:
					/* Reset interrupt request */
					m_irq_pending = false;;
					/* If it is FORCE-IRQ command... */
					if ((in_value & 0xF0) == 0xD0)
					{
						if (D->Verbose) printf("WD1793: FORCE-INTERRUPT (%02Xh)\n", V);
						/* Reset any executing command */
						m_rd_length = m_wr_length = 0;
						/* Either reset BUSY flag or reset all flags if BUSY=0 */
						if (D->R[0] & F_BUSY) D->R[0] &= ~F_BUSY;
						else D->R[0] = D->Track[D->Drive] ? 0 : F_TRACK0;
						/* Cause immediate interrupt if requested */
						if (V & C_IRQ) m_irq_pending = true;;
						/* Done */
						return (D->IRQ);
					}
					/* If busy, drop out */
					if (D->R[0] & F_BUSY) break;
					/* Reset status register */
					D->R[0] = 0x00;
					/* Depending on the command... */
					switch (V & 0xF0)
					{
						case 0x00: /* RESTORE (seek track 0) */
							D->Track[D->Drive] = 0;
							D->R[0] = F_INDEX | F_TRACK0 | (V & C_LOADHEAD ? F_HEADLOAD : 0);
							m_fdc_track = 0;
							m_irq_pending = true;;
							break;

						case 0x10: /* SEEK */
							if (D->Verbose) printf("WD1793: SEEK-TRACK %d (%02Xh)\n", D->R[3], V);
							/* Reset any executing command */
							m_rd_length = m_wr_length = 0;
							D->Track[D->Drive] = D->R[3];
							D->R[0] = F_INDEX
											| (D->Track[D->Drive] ? 0 : F_TRACK0)
											| (V & C_LOADHEAD ? F_HEADLOAD : 0);
							D->R[1] = D->Track[D->Drive];
							m_irq_pending = true;;
							break;

						case 0x20: /* STEP */
						case 0x30: /* STEP-AND-UPDATE */
						case 0x40: /* STEP-IN */
						case 0x50: /* STEP-IN-AND-UPDATE */
						case 0x60: /* STEP-OUT */
						case 0x70: /* STEP-OUT-AND-UPDATE */
							if (D->Verbose) printf("WD1793: STEP%s%s (%02Xh)\n",
								 V & 0x40 ? (V & 0x20 ? "-OUT" : "-IN") : "",
								 V & 0x10 ? "-AND-UPDATE" : "",
								 V
							 );
							/* Either store or fetch step direction */
							if (V & 0x40) D->LastS = V & 0x20; else V = (V & ~0x20) | D->LastS;
							/* Step the head, update track register if requested */
							if (V & 0x20) { if (D->Track[D->Drive]) --D->Track[D->Drive]; }
							else ++D->Track[D->Drive];
							/* Update track register if requested */
							if (V & C_SETTRACK) D->R[1] = D->Track[D->Drive];
							/* Update status register */
							D->R[0] = F_INDEX | (D->Track[D->Drive] ? 0 : F_TRACK0);
							// @@@ WHY USING J HERE?
							//                  | (J&&(V&C_VERIFY)? 0:F_SEEKERR);
							/* Generate IRQ */
							m_irq_pending = true;;
							break;

						case 0x80:
						case 0x90: /* READ-SECTORS */
							if (D->Verbose) printf("WD1793: READ-SECTOR%s %c:%d:%d:%d (%02Xh)\n", V & 0x10 ? "S" : "", D->Drive + 'A', D->Side, D->R[1], D->R[2], V);
							/* Seek to the requested sector */
							D->Ptr = SeekFDI(
								D->Disk[D->Drive], D->Side, D->Track[D->Drive],
								V & C_SIDECOMP ? !!(V & C_SIDE) : D->Side, D->R[1], D->R[2]
							);
							/* If seek successful, set up reading operation */
							if (!D->Ptr)
							{
								if (D->Verbose) printf("WD1793: READ ERROR\n");
								D->R[0] = (D->R[0] & ~F_ERRCODE) | F_NOTFOUND;
								m_irq_pending = true;;
							}
							else
							{
								m_rd_length = D->Disk[D->Drive]->SecSize
														* (V & 0x10 ? (D->Disk[D->Drive]->Sectors - D->R[2] + 1) : 1);
								D->R[0] |= F_BUSY | F_DRQ;
								D->IRQ = WD1793_DRQ;
								D->Wait = 255;
							}
							break;

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

						case 0xC0: /* READ-ADDRESS */
							if (D->Verbose) printf("WD1793: READ-ADDRESS (%02Xh)\n", V);
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

						case 0xE0: /* READ-TRACK */
							if (D->Verbose) printf("WD1793: READ-TRACK %d (%02Xh) UNSUPPORTED!\n", D->R[1], V);
							break;

						case 0xF0: /* WRITE-TRACK */
							if (D->Verbose) printf("WD1793: WRITE-TRACK %d (%02Xh) UNSUPPORTED!\n", D->R[1], V);
							break;

						default: /* UNKNOWN */
							if (D->Verbose) printf("WD1793: UNSUPPORTED OPERATION %02Xh!\n", V);
							break;
					}
					break;

				// track register
				case PORT_TRACK:
					if ((m_fdc_status & F_BUSY) == 0 && !m_fdc_reset_state)
						m_fdc_track = in_value;
					break;

				// sector register
				case PORT_SECTOR:
					if ((m_fdc_status & F_BUSY) == 0 && !m_fdc_reset_state)
						m_fdc_sector = in_value;
					break;

				case PORT_DATA:
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
						if (--m_wr_length)
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

					// Save last written value
					m_fdc_data = in_value;
					break;
#endif
				// parameter register
				case PORT_PARAM:
					m_reg_param = in_value;

					switch(m_reg_param & 0x0f)
					{
						case 1:
							m_current_drive_index = 0;
							break;

						case 2:
							m_current_drive_index = 1;
							break;

						case 4:
							m_current_drive_index = 2;
							break;

						case 8:
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
	}
}
