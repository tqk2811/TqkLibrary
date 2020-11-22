using System;
using System.ComponentModel;
using System.Reflection;

namespace AspMvcLibrary.Attribute
{
  public class ResourceDisplayNameAttribute : DisplayNameAttribute
  {
    private readonly PropertyInfo nameProperty;

    public ResourceDisplayNameAttribute(string displayNameKey, Type resourceType = null) : base(displayNameKey)
    {
      if (resourceType != null)
      {
        nameProperty = resourceType.GetProperty(base.DisplayName, BindingFlags.Static | BindingFlags.Public);
      }
    }

    public override string DisplayName
    {
      get
      {
        if (nameProperty == null) return base.DisplayName;
        return (string)nameProperty.GetValue(nameProperty.DeclaringType, null);
      }
    }
  }
}
