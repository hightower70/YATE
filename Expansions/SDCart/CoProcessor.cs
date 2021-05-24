namespace SDCart
{
  public class CoProcessor
  {
    public delegate byte CoProcessorReadHandler();
    public delegate void CoProcessorWriteHandler(byte in_data);

    public CoProcessorReadHandler CoProcessorRead { get; set; }
    public CoProcessorWriteHandler CoProcessorWrite { get; set; }

    public byte Status { get; set; }

    public SDCart Cart { get; private set; }
    private IFunctionGroup[] m_function_groups;
    private bool m_function_code_expected;

    public CoProcessor(SDCart in_parent)
    {
      Cart = in_parent;

      m_function_groups = new IFunctionGroup[CoProcessorConstants.FunctionGroupCount];
      m_function_groups[0] = new Cassette(this);

      CoProcessorRead = null;
      CoProcessorWrite = null;
      Status = CoProcessorConstants.ERR_OK;
    }

    /// <summary>
    /// Handles coprocessor read operation
    /// </summary>
    /// <returns></returns>
    public byte CoProcRead()
    {
      if (CoProcessorRead == null)
        return Status;
      else
        return CoProcessorRead();
    }

    /// <summary>
    /// Handles coprocessor write operation
    /// </summary>
    /// <param name="in_data"></param>
    public void CoProcWrite(byte in_data)
    {
      IFunctionGroup function_group = m_function_groups[in_data >> 4];

      if (m_function_code_expected)
      {
        m_function_code_expected = false;
        if (function_group != null)
        {
          CoProcessorRead = function_group.CoProcessorRead;
          CoProcessorWrite = function_group.CoProcessorWrite;
        }
      }

      CoProcessorWrite?.Invoke(in_data);
    }

    /// <summary>
    /// Handles coprocessor register read operation (Initialzes communiction with the CoProc)
    /// </summary>
    public void CoProcInt()
    {
      m_function_code_expected = true;
      CoProcessorRead = null;
      CoProcessorWrite = null;
    }
  }
}
