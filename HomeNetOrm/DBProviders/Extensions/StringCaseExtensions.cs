using System.Text;
using System.Text.RegularExpressions;

namespace HomeNetOrm.DBProviders.Extensions
{
    /// <summary>
    /// Расширения строк, созданные строго для нужд маппинга имен в SchemaAdapter.
    /// </summary>
    public static class StringCaseExtensions
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

            if (!name.Contains('_') && char.IsUpper(name[0]))
                return name;

            string safeName = char.ToUpper(name[0]) + name.Substring(1);

            return Regex.Replace(
                safeName,
                @"_([a-zA-Z])",
                match => match.Groups[1].Value.ToUpper()
            ).Replace("_", "");
        }
    }
}
