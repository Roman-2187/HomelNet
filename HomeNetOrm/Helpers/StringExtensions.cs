using System;
using System.Text;
using System.Text.RegularExpressions;

namespace HomeNetOrm.Helpers
{
    public static class StringExtensions
    {
        public static string? ToSnakeCase(this string? name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var builder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) builder.Append('_');
                    builder.Append(char.ToLower(c));
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        public static string? ToCamelCase(this string? name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Безопасно проверяем первый символ, компилятор уверен, что name не null
            if (!name.Contains('_') && char.IsUpper(name[0]))
                return name;

            // Делаем первую букву заглавной
            string safeName = char.ToUpper(name[0]) + name.Substring(1);

            return Regex.Replace(
                safeName,
                @"_([a-zA-Z])",
                match => match.Groups[1].Value.ToUpper()
            ).Replace("_", "");
        }
    }
}
