using System.IO;
using System.Text;

namespace YATE.Emulator.Files.FAT12
{
    /// <summary>
    /// Extended BIOS Parameter Block
    /// </summary>
    public class ExtendedBiosParameterBlock
    {
        /// <summary>
        /// Physical Disk Number for the BIOS
        /// </summary>
        /// <remarks>
        /// This is related to the BIOS physical disk number.
        /// Floppy drives are numbered starting with 0x00 for the A disk.
        /// Physical hard disks are numbered starting with 0x80.
        /// The value is typically 0x80 for hard disks,
        /// regardless of how many physical disk drives exist,
        /// because the value is only relevant if the device is the startup disk.
        /// </remarks>
        public byte PhysicalDiskNumber;
        /// <summary>
        /// Current Head. Not used by the FAT file system.
        /// </summary>
        public byte CurrentHead;
        /// <summary>
        /// Signature. Must be either 0x28 or 0x29 in order to be recognized by Windows NT.
        /// </summary>
        public byte Signature;
        /// <summary>
        /// Volume Serial Number. A unique number that is created when you format the volume.
        /// </summary>
        public uint VolumeSerialNumber;
        /// <summary>
        /// Volume Label. This field was used to store the volume label,
        /// but the volume label is now stored as special file in the root directory.
        /// </summary>
        /// <remarks>Use this as Label only if there is no such Entry in the Root Directory Table</remarks>
        public string VolumeLabel;
        /// <summary>
        /// System ID
        /// </summary>
        /// <remarks>Usually the name of the File system. Some faulty applications depend on this Value.</remarks>
        public string SystemID;

        /// <summary>
        /// Initializes a new ExtendedBiosParameterBlock
        /// </summary>
        /// <param name="S">
        /// Stream that is positioned at the start of the Extended Bios Parameter Block
        /// </param>
        public ExtendedBiosParameterBlock(Stream S)
        {
            using (var BR = new BinaryReader(S, Encoding.Default, true))
            {
                PhysicalDiskNumber = BR.ReadByte();
                CurrentHead = BR.ReadByte();
                Signature = BR.ReadByte();
                VolumeSerialNumber = BR.ReadUInt32();
                VolumeLabel = Encoding.Default.GetString(BR.ReadBytes(11)).TrimEnd();
                SystemID = Encoding.Default.GetString(BR.ReadBytes(8)).TrimEnd();
            }
        }
    }
}
