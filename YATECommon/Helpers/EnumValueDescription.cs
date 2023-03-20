namespace YATECommon.Helpers
{
  class EnumValueDescription
  {
    public object Value { get; set; }
    public string Description { get; set; }

    public EnumValueDescription(object in_value, string in_description)
    {
      Value = in_value;
      Description = in_description;
    }

    public override string ToString() => Description;
  }
}
