using System.Collections.Generic;
using System.Text;

namespace GrassSimulation.Editor
{
    internal static class CsvRows
    {
        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            var fields = new List<string>();
            var field = new StringBuilder();
            var isQuoted = false;
            var length = text.Length;

            for (var i = 0; i < length; i++)
            {
                var c = text[i];

                if (isQuoted)
                {
                    if (c == '"' && i + 1 < length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        isQuoted = false;
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                    {
                        isQuoted = true;
                        break;
                    }

                    case ',':
                    {
                        fields.Add(field.ToString());
                        field.Clear();
                        break;
                    }

                    case '\n':
                    {
                        EndRow(rows, fields, field);
                        break;
                    }

                    case '\r':
                    {
                        break;
                    }

                    default:
                    {
                        field.Append(c);
                        break;
                    }
                }
            }

            EndRow(rows, fields, field);
            return rows;
        }

        private static void EndRow(List<string[]> rows, List<string> fields, StringBuilder field)
        {
            if (fields.Count == 0 && field.Length == 0)
            {
                return;
            }

            fields.Add(field.ToString());
            field.Clear();
            rows.Add(fields.ToArray());
            fields.Clear();
        }
    }
}
