namespace YATE.Emulator.TVCFiles.FAT12
{
    /// <summary>
    /// Holds various constants for the FAT File system
    /// </summary>
    /// <remarks>This is focused on FAT12 with 1.44 MB 3.5" floppy disks</remarks>
    public static class FatConstants
    {
        /// <summary>
        /// Number of Bytes in a FAT Directory Entry
        /// </summary>
        public const int FAT_BYTES_PER_DIRECTORY_ENTRY = 32;
        /// <summary>
        /// Number of Bytes in a FAT Directory File Name
        /// </summary>
        public const int FAT_DIRECTORY_FILENAME_LENGTH = 8;
        /// <summary>
        /// Number of Bytes in a FAT Directory File Extension
        /// </summary>
        public const int FAT_DIRECTORY_FILEEXT_LENGTH = 3;

        /// <summary>
        /// Characters that are valid in File names and volume Labels
        /// </summary>
        public const string VALID_FAT_NAME_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 !#$%&'()-@^_`{}~\x80\x81\x82\x83\x84\x85\x86\x87\x88\x89\x8a\x8b\x8c\x8d\x8e\x8f\x90\x91\x92\x93\x94\x95\x96\x97\x98\x99\x9a\x9b\x9c\x9d\x9e\x9f\xa0\xa1\xa2\xa3\xa4\xa5\xa6\xa7\xa8\xa9\xaa\xab\xac\xad\xae\xaf\xb0\xb1\xb2\xb3\xb4\xb5\xb6\xb7\xb8\xb9\xba\xbb\xbc\xbd\xbe\xbf\xc0\xc1\xc2\xc3\xc4\xc5\xc6\xc7\xc8\xc9\xca\xcb\xcc\xcd\xce\xcf\xd0\xd1\xd2\xd3\xd4\xd5\xd6\xd7\xd8\xd9\xda\xdb\xdc\xdd\xde\xdf\xe0\xe1\xe2\xe3\xe4\xe6\xe7\xe8\xe9\xea\xeb\xec\xed\xee\xef\xf0\xf1\xf2\xf3\xf4\xf5\xf6\xf7\xf8\xf9\xfa\xfb\xfc\xfd\xfe\xff";

        /// <summary>
        /// Tracks on a 3.5" Floppy disk
        /// </summary>
        public const int FLOPPY_TRACKS = 80;
        /// <summary>
        /// Sectors per Track on a 3.5" Floppy Disk
        /// </summary>
        public const int FLOPPY_SECTORS_PER_TRACK = 18;
        /// <summary>
        /// Bytes per Sector of a Floppy Disk
        /// </summary>
        public const int FLOPPY_BYTES_PER_SECTOR = 512;
        /// <summary>
        /// Sides of a Floppy Disk
        /// </summary>
        public const int FLOPPY_SIDES = 2;

        /// <summary>
        /// Size of an 1.44 MD 3.5" Floppy
        /// </summary>
        public const int FLOPPY_FORMAT_3_5 = FLOPPY_TRACKS * FLOPPY_SECTORS_PER_TRACK * FLOPPY_BYTES_PER_SECTOR * FLOPPY_SIDES;
    }
}
