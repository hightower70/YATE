using System.IO;
using System.Text;

namespace YATE.Emulator.TVCFiles.FAT12
{
    /// <summary>
    /// The Part of the Bootsector named the Bios Parameters
    /// </summary>
    public class BiosParameterBlock
    {
        /// <summary>
        /// Bytes per Sector
        /// </summary>
        /// <remarks>For most disks in use in the United States, the value of this field is 512.</remarks>
        public ushort BytesPerSector;
        /// <summary>
        /// Sectors per Clusters
        /// </summary>
        /// <remarks>This is usually 1 but not necessarily</remarks>
        public byte SectorsPerCluster;
        /// <summary>
        /// The number of sectors from the Partition Boot Sector to the start of the first file allocation table,
        /// including the Partition Boot Sector.
        /// </summary>
        /// <remarks>Because this Value includes the Boot Sector, it has to be at least 1</remarks>
        public ushort ReservedSectors;
        /// <summary>
        /// Number of Cluster Maps
        /// </summary>
        /// <remarks>Typically, the value of this field is 2.</remarks>
        public byte NumberOfFatTables;
        /// <summary>
        /// Number of Entries in the Root Directory
        /// </summary>
        public ushort NumberOfRootEntries;
        /// <summary>
        /// Number of Sectors on Disk. If 0, use <see cref="LargeSectors"/> instead
        /// </summary>
        public ushort SmallSectors;
        /// <summary>
        /// Type of Media
        /// </summary>
        /// <remarks>A value of 0xF8 indicates a hard disk.</remarks>
        public byte MediaType;
        /// <summary>
        /// Sectors in each FAT
        /// </summary>
        /// <remarks>
        /// Number of sectors occupied by each of the file allocation tables on the volume.
        /// By using this information, together with the Number of FATs and Reserved Sectors,
        /// you can compute where the root folder begins.
        /// By using the number of entries in the root folder,
        /// you can also compute where the user data area of the volume begins.
        /// </remarks>
        public ushort SectorsPerFat;
        /// <summary>
        /// Sectors in each Track
        /// </summary>
        public ushort SectorsPerTrack;
        /// <summary>
        /// Number of Heads (Sides)
        /// </summary>
        public ushort NumberOfHeads;
        /// <summary>
        /// Same as the Relative Sector field in the Partition Table.
        /// </summary>
        public uint HiddenSectors;
        /// <summary>
        /// If the <see cref="SmallSectors"/> field is zero,
        /// this field contains the total number of sectors in the volume.
        /// If Small Sectors is nonzero, this field contains zero.
        /// </summary>
        public uint LargeSectors;

        /// <summary>
        /// Initializes a new BiosParameterBlock
        /// </summary>
        /// <param name="S">Stream that is positioned at the start of the Bios Parameter Block</param>
        public BiosParameterBlock(Stream S)
        {
            using (var BR = new BinaryReader(S, Encoding.Default, true))
            {
                BytesPerSector = BR.ReadUInt16();
                SectorsPerCluster = BR.ReadByte();
                ReservedSectors = BR.ReadUInt16();
                NumberOfFatTables = BR.ReadByte();
                NumberOfRootEntries = BR.ReadUInt16();
                SmallSectors = BR.ReadUInt16();
                MediaType = BR.ReadByte();
                SectorsPerFat = BR.ReadUInt16();
                SectorsPerTrack = BR.ReadUInt16();
                NumberOfHeads = BR.ReadUInt16();
                HiddenSectors = BR.ReadUInt32();
                LargeSectors = BR.ReadUInt32();
            }
        }
    }
}
