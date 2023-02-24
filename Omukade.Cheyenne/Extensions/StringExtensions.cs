namespace Omukade.Cheyenne.Extensions
{
    static internal class StringExtensions
    {
        public static int GetUtf8Length(this string str) => System.Text.Encoding.UTF8.GetByteCount(str);
        public static byte[] GetUtf8Bytes(this string str) => System.Text.Encoding.UTF8.GetBytes(str);
    }
}
