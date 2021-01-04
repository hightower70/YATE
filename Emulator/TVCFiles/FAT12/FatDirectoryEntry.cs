using System;
using System.IO;
using System.Linq;
using System.Text;

namespace YATE.Emulator.TVCFiles.FAT12
{
    /// <summary>
    /// Entry in the FAT Directory Table
    /// </summary>
    /// <remarks>This can't yet handle long file names</remarks>
    public class FatDirectoryEntry
    {
        /// <summary>
        /// 8 char File Name
        /// </summary>
        /// <remarks>This is padded with spaces at the end if needed.</remarks>
        private string _fileName;
        /// <summary>
        /// 8 char Extension
        /// </summary>
        /// <remarks>
        /// This is padded with spaces at the end if needed.
        /// Files/directories without extension have this set to 3 spaces
        /// </remarks>
        private string _fileExt;
        /// <summary>
        /// Creation timestamp
        /// </summary>
        /// <remarks>Timestamp is only accurate to 2 seconds</remarks>
        private ushort _createTime;
        /// <summary>
        /// Creation Date
        /// </summary>
        private ushort _createDate;
        /// <summary>
        /// Last Access Date
        /// </summary>
        /// <remarks>This will almost always not be set for floppy disk images</remarks>
        private ushort _accessDate;
        /// <summary>
        /// Modification Date
        /// </summary>
        private ushort _modifyDate;
        /// <summary>
        /// Modification Time
        /// </summary>
        /// <remarks>Timestamp is only accurate to 2 seconds</remarks>
        private ushort _modifyTime;

        /// <summary>
        /// Gets or Sets the 8 char File name Entry
        /// </summary>
        /// <remarks>Trimmed Spaces</remarks>
        public string FileName
        {
            get
            {
                return _fileName.TrimEnd();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FileName");
                }
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("File name can't be exclusively made up of whitespace");
                }
                if (Encoding.Default.GetByteCount(value) > FatConstants.FAT_DIRECTORY_FILENAME_LENGTH)
                {
                    throw new FormatException("File name can't be longer than 8 chars");
                }
                _fileName = value.ToUpper().PadRight(FatConstants.FAT_DIRECTORY_FILENAME_LENGTH);
            }
        }
        /// <summary>
        /// Gets or Sets the 3 char File name Extension of the Entry
        /// </summary>
        /// <remarks>Trimmed Spaces</remarks>
        public string Extension
        {
            get
            {
                return _fileExt.TrimEnd();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FileName");
                }
                if (Encoding.Default.GetByteCount(value) > FatConstants.FAT_DIRECTORY_FILEEXT_LENGTH)
                {
                    throw new FormatException("File extension can't be longer than 3 chars");
                }
                _fileExt = value.ToUpper().PadRight(FatConstants.FAT_DIRECTORY_FILEEXT_LENGTH);
            }
        }
        /// <summary>
        /// Gets the Full name of this Entry
        /// </summary>
        /// <remarks>Trimmed Spaces</remarks>
        public string FullName
        {
            get
            {
                if (_fileExt.Trim().Length > 0)
                {
                    return string.Format("{0}.{1}", _fileName.Trim(), _fileExt.Trim());
                }
                return _fileName.Trim();
            }
        }
        /// <summary>
        /// Gets or Sets the Status of this Entry
        /// </summary>
        /// <remarks><see cref="DirectoryEntryStatus.InUse"/> must be set by setting the first char of the Entry properly.</remarks>
        public DirectoryEntryStatus EntryStatus
        {
            get
            {
                switch (_fileName[0])
                {
                    case '\0':
                        return DirectoryEntryStatus.Empty;
                    case '\x05':
                        return DirectoryEntryStatus.PendingDelete;
                    case '\xE5':
                        return DirectoryEntryStatus.Deleted;
                    case '.':
                        return DirectoryEntryStatus.DotEntry;
                }
                return DirectoryEntryStatus.InUse;
            }
            set
            {
                if (!Enum.IsDefined(value.GetType(), value))
                {
                    throw new ArgumentException($"Undefined Enum:DirectoryEntryStatus Value: {value}");
                }
                if (value == DirectoryEntryStatus.InUse)
                {
                    throw new ArgumentException("'InUse' can't be set directly. Instead properly set the first char of the File Name");
                }
                //Replace first char with appropriate Value
                var Name = _fileName.ToCharArray();
                Name[0] = (char)value;
                _fileName = new string(Name);
            }
        }
        /// <summary>
        /// Attributes of the Entry
        /// </summary>
        public DirectoryEntryAttribute Attributes;
        /// <summary>
        /// Additional Attributes
        /// </summary>
        /// <remarks>Only in use by some Operating Systems</remarks>
        public byte AdditionalAttributes;
        /// <summary>
        /// The Fine Resolution (x*10 ms) of the Create Time.
        /// The First char of the Name if the File was deleted.
        /// </summary>
        public byte UndeleteCharOrCreateFineResolution;
        /// <summary>
        /// First Cluster of the File
        /// </summary>
        /// <remarks>In Combination with the Clustermap, allows to read the file</remarks>
        public ushort FirstCluster;
        /// <summary>
        /// Size in bytes of the File
        /// </summary>
        public uint FileSize;

        /// <summary>
        /// Gets the Create Time
        /// </summary>
        public TimeSpan CreateTime
        {
            get
            {
                return ParseTimestamp(_createTime);
            }
            set
            {
            }
        }
        /// <summary>
        /// Gets the Create Date
        /// </summary>
        public DateTime CreateDate
        {
            get
            {
                return ParseDate(_createDate);
            }
        }
        /// <summary>
        /// Gets the Modify Time
        /// </summary>
        public TimeSpan ModifyTime
        {
            get
            {
                return ParseTimestamp(_modifyTime);
            }
            set
            {
            }
        }
        /// <summary>
        /// Gets the Modify Date
        /// </summary>
        public DateTime ModifyDate
        {
            get
            {
                return ParseDate(_modifyDate);
            }
        }
        /// <summary>
        /// Extended Attributes used by some OS
        /// </summary>
        public ushort ExtendedAttributes;

        /// <summary>
        /// Creates a Fat Directory Entry from a byte array
        /// </summary>
        /// <param name="Entry">
        /// Directory Emtry.
        /// Must be <see cref="FatConstants.FAT_BYTES_PER_DIRECTORY_ENTRY"/> bytes in length.
        /// </param>
        public FatDirectoryEntry(byte[] Entry)
        {
            if (Entry == null)
            {
                throw new ArgumentNullException("FileName");
            }
            if (Entry.Length != FatConstants.FAT_BYTES_PER_DIRECTORY_ENTRY)
            {
                throw new ArgumentOutOfRangeException($"Entry must be {FatConstants.FAT_BYTES_PER_DIRECTORY_ENTRY} bytes in Length");
            }
            using (var BR = new BinaryReader(new MemoryStream(Entry, false)))
            {
                _fileName = Encoding.Default.GetString(BR.ReadBytes(FatConstants.FAT_DIRECTORY_FILENAME_LENGTH));
                _fileExt = Encoding.Default.GetString(BR.ReadBytes(FatConstants.FAT_DIRECTORY_FILEEXT_LENGTH));
                Attributes = (DirectoryEntryAttribute)BR.ReadByte();
                AdditionalAttributes = BR.ReadByte();
                UndeleteCharOrCreateFineResolution = BR.ReadByte();

                _createTime = BR.ReadUInt16();
                _createDate = BR.ReadUInt16();

                _accessDate = BR.ReadUInt16();

                ExtendedAttributes = BR.ReadUInt16();

                _modifyTime = BR.ReadUInt16();
                _modifyDate = BR.ReadUInt16();

                FirstCluster = BR.ReadUInt16();
                FileSize = BR.ReadUInt32();
            }
        }

        /// <summary>
        /// Checks if the given file name Segment (name OR extension)
        /// is only made up of supported characters
        /// </summary>
        /// <param name="FileNameSegment">File Name OR Extension</param>
        /// <returns>true, if valid chars used only</returns>
        public static bool IsValidFileName(string FileNameSegment)
        {
            return FileNameSegment != null && FileNameSegment.ToCharArray().All(m => FatConstants.VALID_FAT_NAME_CHARS.Contains(m));
        }

        /// <summary>
        /// Parses a Date Component of a Directory Entry in a DateTime Structure
        /// </summary>
        /// <param name="Date">Date Component</param>
        /// <returns>DateTime Structure</returns>
        public static DateTime ParseDate(ushort Date)
        {
            if (Date == 0)
            {
                return DateTime.MinValue;
            }
            //Bitmap: yyyyyyymmmmddddd
            var Days = Date & 0x1F;
            var Months = (Date >> 5) & 0xF;
            var Years = Date >> 9;
            return new DateTime(1980 + Years, Months, Days, 0, 0, 0, DateTimeKind.Local);
        }

        /// <summary>
        /// Parses a Time Component of a Directory Entry in a TimeSpan Structure
        /// </summary>
        /// <param name="Timestamp">Time Component</param>
        /// <returns>TimeSpan Structure</returns>
        public static TimeSpan ParseTimestamp(ushort Timestamp)
        {
            //Bitmap: hhhhhmmmmmmsssss
            var seconds = (Timestamp & 0x1F) / 2;
            var minutes = (Timestamp >> 5) & 0x7e0;
            var hours = Timestamp >> 11;
            return TimeSpan.FromSeconds(seconds + minutes * 60 + hours * 3600);
        }
    }

    /// <summary>
    /// Attributes for an Entry
    /// </summary>
    /// <remarks>
    /// While possible to set any combination of attributes,
    /// not all make sense
    /// </remarks>
    [Flags]
    public enum DirectoryEntryAttribute : byte
    {
        /// <summary>
        /// No Attributes designate a regular File
        /// </summary>
        Normal = 0,
        /// <summary>
        /// File is Readonly
        /// </summary>
        Readonly = 1,
        /// <summary>
        /// File/Directory is Hidden
        /// </summary>
        Hidden = Readonly << 1,
        /// <summary>
        /// File/Directory is important to the System.
        /// Defragmenters will ignore that file
        /// </summary>
        System = Hidden << 1,
        /// <summary>
        /// Entry is a Volume Label
        /// </summary>
        VolumeLabel = System << 1,
        /// <summary>
        /// Entry is a Directory and not a File
        /// </summary>
        Directory = VolumeLabel << 1,
        /// <summary>
        /// Entry has been modified since the last Backup
        /// </summary>
        Archive = Directory << 1,
        /// <summary>
        /// Entry represents a Device
        /// </summary>
        Device = Archive << 1,
        /// <summary>
        /// Reserved, must never be set
        /// </summary>
        Reserved = Archive << 1
    }

    /// <summary>
    /// Status of a Directory Entry
    /// </summary>
    /// <remarks>This is detected from the first character in the file name
    /// (apart from <see cref="InUse"/>)
    /// </remarks>
    public enum DirectoryEntryStatus : byte
    {
        /// <summary>
        /// Entry is not used
        /// </summary>
        Empty = 0,
        /// <summary>
        /// File is pending for removal
        /// </summary>
        PendingDelete = 5,
        /// <summary>
        /// File is "." or ".." entry
        /// </summary>
        DotEntry = 0x2E,
        /// <summary>
        /// File is deleted. The reason, why 0xE5 was chosen for this purpose in 86-DOS is down to the fact, that 8-inch CP/M floppies came pre-formatted with this value filled and so could be used to store files out-of-the box.
        /// </summary>
        Deleted = 0xE5,
        /// <summary>
        /// Regular Entry that is in use
        /// </summary>
        InUse = 0xFF
    }
}