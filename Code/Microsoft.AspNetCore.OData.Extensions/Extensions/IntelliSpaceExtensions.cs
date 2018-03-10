using System.Text;
using System.Text.RegularExpressions;

namespace Brandless.AspNetCore.OData.Extensions.Extensions
{
    public static class IntelliSpaceExtensions
    {
        /// <summary>
        /// Takes a string like: "SomeWords" and converts it to "Some Words"
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string IntelliSpace(this string str)
        {
            if (str == null)
                return null;
            if (str.ToUpper() == str) return str;
            str = str.Replace('_', ' ');
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                // if it's upper case, inject a space
                if (i > 0)
                {
                    if (str[i].IsUpperCase() && str[i].IsAlpha())
                    {
                        // only if the last letter WAS NOT uppercase
                        if (!str[i - 1].IsUpperCase() && (str[i - 1].IsAlpha() || str[i - 1].IsNumeric()))
                        {
                            sb.Append(' ');
                            sb.Append(str[i]);
                        }
                        // or only if it is not the last letter and the next letter IS NOT uppercase
                        else if (i != str.Length - 1 && !str[i + 1].IsUpperCase() && (str[i - 1].IsAlpha() || str[i - 1].IsNumeric()))
                        {
                            sb.Append(' ');
                            sb.Append(str[i]);
                        }
                        else
                        {
                            sb.Append(str[i]);
                        }
                    }
                    else if (str[i].IsNumeric() && str[i - 1].IsAlpha())
                    {
                        sb.Append(' ');
                        sb.Append(str[i]);
                    }
                    else if (
                        i != str.Length - 1 &&
                        !str[i].IsAlpha() && !str[i].IsNumeric() && str[i] != ' ' &&
                        (str[i + 1].IsAlpha() || str[i + 1].IsNumeric() || str[i + 1] == ' '))
                    {
                        sb.Append(' ');
                        sb.Append(str[i]);
                    }
                    else
                    {
                        sb.Append(str[i]);
                    }
                }
                else
                {
                    sb.Append(str[i]);
                }
            }

            return sb.ToString();
        }



        public static bool IsAlpha(this char c)
        {
            return Regex.IsMatch(c.ToString(), "[A-Za-z]");
        }

        public static bool IsNumeric(this string str, bool allowDecimal = false)
        {
            return Regex.IsMatch(str, @"^[0-9]+(\.[0-9]+){0,1}$");
        }

        public static bool IsNumeric(this char c)
        {
            return Regex.IsMatch(c.ToString(), "[0-9]");
        }

        public static bool IsUpperCase(this char c)
        {
            return c.ToString().ToUpper() == c.ToString();
        }
    }
}