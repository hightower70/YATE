namespace YATE.Emulator.TVCFiles.FAT12
{
    /// <summary>
    /// Cluster Status of a Value
    /// </summary>
    /// <remarks>
    /// Some 12 bit values in a cluster have a special meaning.
    /// These values are all possible meanings
    /// </remarks>
    public enum ClusterStatus : byte
    {
        /// <summary>
        /// Cluster is empty
        /// </summary>
        /// <remarks>This cluster is usable for files and directories</remarks>
        Empty = 0,
        /// <summary>
        /// This cluster is reserved
        /// </summary>
        /// <remarks>Don't use this cluster</remarks>
        Reserved = 1,
        /// <summary>
        /// Cluster is occupied by a file or directory
        /// </summary>
        /// <remarks>
        /// This cluster is part of a file that spans multiple clusters.
        /// The last part will have another value.
        /// </remarks>
        Occupied = 2,
        /// <summary>
        /// Cluster is damaged
        /// </summary>
        /// <remarks>
        /// No attempts should be made to read/write this cluster as it can lead to hardware problems.
        /// Sometimes accessing a damaged cluster can "spread" the damage.
        /// </remarks>
        Damaged = 3,
        /// <summary>
        /// Cluster is the last part of a file
        /// </summary>
        /// <remarks>
        /// Files that span only one cluster will have this value also.
        /// </remarks>
        EOF = 4
    }
}
