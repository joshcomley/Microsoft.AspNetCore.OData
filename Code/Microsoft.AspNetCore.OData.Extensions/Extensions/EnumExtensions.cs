namespace Brandless.AspNetCore.OData.Extensions.Extensions
{
    public static class EnumExtensions
    {
        public static bool IsValid<TEnum>(this TEnum enumValue)
            where TEnum : struct
        {
            return IsValidEnumValue(enumValue);
        }

        public static bool IsValidEnumValue(object enumValue)
        {
            if (enumValue == null)
            {
                return false;
            }
            var firstChar = enumValue.ToString()[0];
            return (firstChar < '0' || firstChar > '9') && firstChar != '-';
        }
    }
}