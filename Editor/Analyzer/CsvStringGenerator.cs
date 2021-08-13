using System.Text;

namespace UTJ.ProfilerReader.Analyzer
{
    public class CsvStringGenerator
    {
        private StringBuilder stringBuilder;

        public CsvStringGenerator()
        {
            stringBuilder = new StringBuilder(1024 * 1024);
        }

        public CsvStringGenerator(StringBuilder sb)
        {
            stringBuilder = sb;
        }

        public CsvStringGenerator AppendColumn(string val)
        {
            if (val == null)
            {
                val = "";
            }

            if (val.Contains(","))
            {
                val = val.Replace(',', '.');
            }

            if (val.Contains("\n"))
            {
                val = val.Replace('\n', ' ');
            }

            AppendCommaIfNotFirstColumnOfLine();

            stringBuilder.Append(val);
            return this;
        }

        public CsvStringGenerator AppendColumn(int val)
        {
            AppendCommaIfNotFirstColumnOfLine();

            stringBuilder.Append(val);
            return this;
        }

        public CsvStringGenerator AppendColumn(bool val)
        {
            AppendCommaIfNotFirstColumnOfLine();

            stringBuilder.Append(val);
            return this;
        }

        public CsvStringGenerator AppendColumn(float val)
        {
            AppendCommaIfNotFirstColumnOfLine();

            stringBuilder.Append(val);
            return this;
        }

        public CsvStringGenerator AppendColumn(ulong val)
        {
            AppendCommaIfNotFirstColumnOfLine();

            stringBuilder.Append(val);
            return this;
        }

        public CsvStringGenerator AppendColumnAsAddr(ulong val)
        {
            AppendCommaIfNotFirstColumnOfLine();

            stringBuilder.Append("0x");
            AppendAddrStr(stringBuilder, val, 16);
            return this;
        }

        private void AppendCommaIfNotFirstColumnOfLine()
        {
            if (stringBuilder.Length > 0 && stringBuilder[stringBuilder.Length - 1] != '\n')
            {
                stringBuilder.Append(',');
            }
        }

        public CsvStringGenerator NextRow()
        {
            stringBuilder.Append("\n");
            return this;
        }

        public override string ToString()
        {
            return stringBuilder.ToString();
        }

        private static readonly char[] AddrChars =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        };

        // AppendFormat("0x{0,0:X16}", val) <- allocate a lot of managed memory...
        public static StringBuilder AppendAddrStr(StringBuilder sb, ulong val, int num)
        {
            for (int i = num - 1; i >= 0; --i)
            {
                ulong masked = (val >> (i * 4)) & 0xf;
                sb.Append(AddrChars[masked]);
            }

            return sb;
        }
    }
}