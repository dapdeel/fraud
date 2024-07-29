using System;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
sealed class EnumStringValueAttribute : Attribute
{
    public string StringValue { get; }

    public EnumStringValueAttribute(string stringValue)
    {
        StringValue = stringValue;
    }
}
