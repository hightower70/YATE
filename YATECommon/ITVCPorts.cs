namespace YATECommon
{
  public delegate void IORead(ushort in_port_address, ref byte inout_data);
  public delegate void IOWrite(ushort in_port_address, byte in_data);
  public delegate void IOReset();

  public interface ITVCPorts
  {
    void AddPortReader(ushort in_address, IORead in_reader);
    void RemovePortReader(ushort in_address, IORead in_reader);

    void AddPortWriter(ushort in_address, IOWrite in_writer);
    void RemovePortWriter(ushort in_address, IOWrite in_writer);

    void AddPortReset(ushort in_address, IOReset in_reset);
    void RemovePortReset(ushort in_address, IOReset in_reset);

    void Reset();
  }
}
