using CustomControls.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Markup;

namespace CustomControls
{
  [MarkupExtensionReturnType(typeof(IEnumerable<Enum>))]
  public sealed class EnumDescriptionExtension : MarkupExtension
  {
    public EnumDescriptionExtension()
    {
    }

    public EnumDescriptionExtension(Type enumType)
    {
      EnumType = enumType;
    }

    [ConstructorArgument("enumType")]
    public Type EnumType { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      string description;
      Array enumValues = EnumType.GetEnumValues();
      List<EnumValueDescription> retval = new List<EnumValueDescription>();
      retval.Capacity = enumValues.Length;

      foreach (object enumValue in enumValues)
      {
        var fieldInfo = EnumType.GetField(enumValue.ToString());
        DescriptionAttribute displayAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
        if (displayAttribute != null)
        {
          description = displayAttribute.Description;
        }
        else
        {
          description = enumValue.ToString();
        }


        retval.Add(new EnumValueDescription(enumValue, description));
      }

      return retval;
    }
  }
}
