using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TVCEmu.Models.TVCFiles.FAT12
{
    /// <summary>
    /// Represents a FAT12 Partition
    /// </summary>
    public class FatPartition
    {
        /// <summary>
        /// Boot Jump instructions found by default on most partitions
        /// </summary>
        public static byte[] DefaultBootJumpInstructions
        {
            get
            {
                return new byte[] { 0xEB, 0x3C, 0x90 };
            }
        }

        /// <summary>
        /// OEM Name found often on FAT12 Disk images
        /// </summary>
        public static string DefaultOemName
        {
            get
            {
                return "MSDOS5.0";
            }
        }

        /// <summary>
        /// Boot Jump Instruction of the image
        /// </summary>
        private byte[] _bootJumpInstructions;
        /// <summary>
        /// OEM Name
        /// </summary>
        private string _oemName;
        /// <summary>
        /// BIOS Parameters
        /// </summary>
        private BiosParameterBlock _biosParameters;
        /// <summary>
        /// Extended BIOS Parameters
        /// </summary>
        private ExtendedBiosParameterBlock _extendedBiosParameters;
        /// <summary>
        /// Code for Boot Routine
        /// </summary>
        private byte[] _bootCode;
        /// <summary>
        /// Signature of the Boot Sector
        /// </summary>
        /// <remarks>
        /// This is almost always {0x55,0xAA} and if it is not,
        /// many computers will ignore it in the boot order.
        /// </remarks>
        private byte[] _bootSectorSignature;
        /// <summary>
        /// Cluster Map
        /// </summary>
        private ClusterEntry[] _clusterMap;
        /// <summary>
        /// FAT Root Directory
        /// </summary>
        private FatDirectoryEntry[] _rootDirectory;

        /// <summary>
        /// Boot Jump Instructions found in Image
        /// </summary>
        /// <remarks>Always 3 Bytes</remarks>
        public byte[] BootJumpInstructions
        {
            get
            {
                return (byte[])_bootJumpInstructions.Clone();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("BootJumpInstructions");
                }
                if (value.Length != 3)
                {
                    throw new FormatException("BootJumpInstructions must be 3 bytes long");
                }
                _bootJumpInstructions = (byte[])value.Clone();
            }
        }
        /// <summary>
        /// OEM Name. Meant for the OS/application to set freely
        /// </summary>
        /// <remarks>
        /// Always 8 bytes.
        /// Some (faulty) applications need this to be a certain value.
        /// Often this is <see cref="DefaultOemName"/>.
        /// </remarks>
        public string OemName
        {
            get
            {
                return _oemName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("OemName");
                }
                if (Encoding.Default.GetByteCount(value) > 8)
                {
                    throw new FormatException("OemName must not be longer than 8 ASCII characters");
                }
                _oemName = value.PadRight(8);
            }
        }
        /// <summary>
        /// Values of the BIOS Parameter Block
        /// </summary>
        public BiosParameterBlock BiosParameters
        {
            get
            {
                return _biosParameters;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("BiosParameters");
                }
                _biosParameters = value;
            }
        }
        /// <summary>
        /// Values of the Extended BIOS Parameter Block
        /// </summary>
        public ExtendedBiosParameterBlock ExtendedBiosParameters
        {
            get
            {
                return _extendedBiosParameters;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("BiosParameters");
                }
                _extendedBiosParameters = value;
            }
        }
        /// <summary>
        /// FAT12 Cluster Map
        /// </summary>
        public ClusterEntry[] ClusterMap
        {
            get
            {
                return (ClusterEntry[])_clusterMap.Clone();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ClusterMap");
                }
                if (value.Length != _clusterMap.Length)
                {
                    throw new FormatException($"Cluster map must have {_clusterMap.Length} entries");
                }
                _clusterMap = (ClusterEntry[])value.Clone();
            }
        }
        /// <summary>
        /// FAT Root Directory
        /// </summary>
        public FatDirectoryEntry[] RootDirectory
        {
            get
            {
                return (FatDirectoryEntry[])_rootDirectory.Clone();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("RootDirectory");
                }
                if (value.Length != _rootDirectory.Length)
                {
                    throw new FormatException($"Root Directory must have {_clusterMap.Length} entries");
                }
                _rootDirectory = (FatDirectoryEntry[])value.Clone();
            }
        }

        /// <summary>
        /// Reads a Stream into a FAT12 Partition
        /// </summary>
        /// <param name="S">Stream Positioned at the Boot Sector</param>
        public FatPartition(Stream S)
        {
            using (var BR = new BinaryReader(S, Encoding.Default, true))
            {
                //Read the first Sector
                _bootJumpInstructions = BR.ReadBytes(3);
                _oemName = Encoding.Default.GetString(BR.ReadBytes(8)).TrimEnd();
                _biosParameters = new BiosParameterBlock(S);
                _extendedBiosParameters = new ExtendedBiosParameterBlock(S);
                _bootCode = BR.ReadBytes(448);
                _bootSectorSignature = BR.ReadBytes(2);

                //Read the FAT12 Entry
                using (var MS = new MemoryStream(BR.ReadBytes(_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat)))
                {
                    using (var MR = new BinaryReader(MS))
                    {
                        _clusterMap = new ClusterEntry[_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat / 3 * 2];
                        for (int i = 0; i < _clusterMap.Length; i += 2)
                        {
                            byte[] Clusters = MR.ReadBytes(3);
                            //Swap nibbles of byte array
                            Clusters[1] = (byte)((Clusters[1] << 4) + (Clusters[1] >> 4));

														ushort Cluster1 = (ushort)(Clusters[0] + ((Clusters[1] >> 4) * 256));
                            ushort Cluster2 = (ushort)((Clusters[2] << 4) + (Clusters[1] & 0xF));

                            _clusterMap[i] = new ClusterEntry(Cluster1);
                            _clusterMap[i + 1] = new ClusterEntry(Cluster2);
                        }
                        //Hardcode first two clusters to "reserved"
                        _clusterMap[0].Status = _clusterMap[1].Status = ClusterStatus.Reserved;
                    }
                }
                //Skip all other FAT tables
                for (var i = 0; i < _biosParameters.NumberOfFatTables - 1; i++)
                {
                    BR.ReadBytes(_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat);
                }
                //Root Directory
                _rootDirectory = ReadDirectory(BR.ReadBytes(FatConstants.FAT_BYTES_PER_DIRECTORY_ENTRY * _biosParameters.NumberOfRootEntries));
            }
        }

        /// <summary>
        /// Calculates the Physical Stream Offset of a given Cluster
        /// </summary>
        /// <param name="Cluster">Cluster Number</param>
        /// <returns>Stream Offset in Bytes</returns>
        public int CalculateOffset(ushort Cluster)
        {
			return _biosParameters.BytesPerSector + _biosParameters.SectorsPerCluster * _biosParameters.BytesPerSector * (Cluster - 2) +
								_biosParameters.NumberOfFatTables * _biosParameters.SectorsPerFat * _biosParameters.BytesPerSector +
								_biosParameters.NumberOfRootEntries * FatConstants.FAT_BYTES_PER_DIRECTORY_ENTRY;
        }

        /// <summary>
        /// Reads a cluster chain into one contiguous data block
        /// </summary>
        /// <param name="ClusterChain">Cluster Chain</param>
        /// <param name="FATStream">Stream to read From</param>
        /// <returns>Cluster data</returns>
        public byte[] ReadClusters(ushort[] ClusterChain, Stream FATStream)
        {
            int BlockSize = _biosParameters.SectorsPerCluster * _biosParameters.BytesPerSector;
            byte[] Data = new byte[BlockSize];
            using (var MS = new MemoryStream())
            {
                foreach (var Cluster in ClusterChain)
                {
                    FATStream.Seek(CalculateOffset(Cluster), SeekOrigin.Begin);
                    FATStream.Read(Data, 0, Data.Length);
                    MS.Write(Data, 0, Data.Length);
                }
                return MS.ToArray();
            }
        }

        /// <summary>
        /// Reads a cluster chain of a file
        /// </summary>
        /// <param name="ClusterChain">Cluster Chain</param>
        /// <param name="FileSize">File Size in bytes</param>
        /// <param name="FATStream">Stream to read from</param>
        /// <returns>File content</returns>
        public byte[] ReadFile(ushort[] ClusterChain, int FileSize, Stream FATStream)
        {
            return ReadClusters(ClusterChain, FATStream).Take(FileSize).ToArray();
        }

        /// <summary>
        /// Reads a File from a Directory Entry
        /// </summary>
        /// <param name="Entry">Directory Entry</param>
        /// <param name="FatStream">FAT Image Stream</param>
        /// <returns>File data</returns>
        /// <remarks>Only works for Files and Directories</remarks>
        public byte[] ReadFile(FatDirectoryEntry Entry, Stream FatStream)
        {
            //Bitmask of all unsupported flags
            var InvalidFlags =
                DirectoryEntryAttribute.Device |
                DirectoryEntryAttribute.Reserved |
                DirectoryEntryAttribute.VolumeLabel;
            //Masking with unsupported flags should return "Normal" (0)
            if ((Entry.Attributes & InvalidFlags) != DirectoryEntryAttribute.Normal)
            {
                throw new ArgumentException("This function only supports files and directories");
            }
            if (Entry.FileSize > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("Entry.FileSize is way too large for FAT12. This hints at a corrupted file entry.");
            }
            return ReadFile(GetClusterChain(Entry.FirstCluster), (int)Entry.FileSize, FatStream);
        }

        /// <summary>
        /// Reads a FAT Directory from a byte Array
        /// </summary>
        /// <param name="RawDirectory">FAT Directory Data</param>
        /// <returns>FAT Directory Entries</returns>
        public static FatDirectoryEntry[] ReadDirectory(byte[] RawDirectory)
        {
            using (var MS = new MemoryStream(RawDirectory, false))
            {
                using (var BR = new BinaryReader(MS))
                {
                    return Enumerable.Range(0, RawDirectory.Length / FatConstants.FAT_BYTES_PER_DIRECTORY_ENTRY).Select(m => new FatDirectoryEntry(BR.ReadBytes(FatConstants.FAT_BYTES_PER_DIRECTORY_ENTRY))).ToArray();
                }
            }
        }

        /// <summary>
        /// Gets a Cluster chain given a start Cluster
        /// </summary>
        /// <param name="Start">Cluster to start reading</param>
        /// <returns>Cluster chain</returns>
        public ushort[] GetClusterChain(ushort Start)
        {
            if (Start >= _clusterMap.Length)
            {
                throw new ArgumentOutOfRangeException("Start", "Start must be smaller than ClusterMap.Length");
            }
            if (_clusterMap[Start].Status == ClusterStatus.Reserved)
            {
                throw new ArgumentException("Start refers to reserved cluster");
            }
            if (_clusterMap[Start].Status == ClusterStatus.Damaged)
            {
                throw new ArgumentException("Start refers to damaged cluster");
            }
            if (_clusterMap[Start].Status == ClusterStatus.Empty)
            {
                throw new ArgumentException("Start refers to empty cluster");
            }
            List<ushort> L = new List<ushort>();

            L.Add(Start);
            var Current = Start;
            while (_clusterMap[Current].Status == ClusterStatus.Occupied)
            {
                Current = _clusterMap[Current].RawValue;
                L.Add(Current);
            }

            return L.ToArray();
        }
    }
}
