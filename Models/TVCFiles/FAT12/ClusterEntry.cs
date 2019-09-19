namespace TVCEmu.Models.TVCFiles.FAT12
{
    public struct ClusterEntry
    {
        /// <summary>
        /// Raw Value of the Cluster
        /// </summary>
        /// <remarks>
        /// For clusters holding files and directories,
        /// this points to the next cluster,
        /// if it is not the last in the chain.
        /// </remarks>
        public ushort RawValue;
        /// <summary>
        /// <see cref="RawValue"/> interpreted as Cluster Status
        /// </summary>
        public ClusterStatus Status;

        /// <summary>
        /// Creates a new Cluster Entry from the given Value
        /// </summary>
        /// <param name="Raw">Value</param>
        public ClusterEntry(ushort Raw)
        {
            RawValue = Raw;
            Status = GetFat12Status(Raw);
        }

        /// <summary>
        /// Gets the Cluster Status from a Value
        /// </summary>
        /// <param name="u">Value</param>
        /// <returns>Cluster Status</returns>
        public static ClusterStatus GetFat12Status(ushort u)
        {
            if (u == 0)
            {
                return ClusterStatus.Empty;
            }
            if (u == 1 || u == 0xFF6)
            {
                return ClusterStatus.Reserved;
            }
            if (u <= 0xFEF || u == 0xFF0)
            {
                return ClusterStatus.Occupied;
            }
            if (u == 0xFF7)
            {
                return ClusterStatus.Damaged;
            }
            //End of file marker otherwise
            return ClusterStatus.EOF;
        }

    }
}
