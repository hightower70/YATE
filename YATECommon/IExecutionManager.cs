namespace YATECommon
{
  public interface IExecutionManager
  {
    void Initialize();

    ITVComputer ITVC { get; }
  }
}
