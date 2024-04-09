using ICSharpCode.SharpZipLib.GZip;
using MatchLogic;
using Newtonsoft.Json;

namespace Omukade.Cheyenne.Encoding
{
    public static class FasterJson
    {
        private static readonly System.Text.Encoding ENCODING_UTF8_NOBOM = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static T JsonClone<T>(T obj, JsonSerializerSettings? settings = null)
        {
            JsonSerializer serializer = JsonSerializer.Create(settings);
            using MemoryStream ms = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(ms, encoding: ENCODING_UTF8_NOBOM, leaveOpen: true))
            {
                serializer.Serialize(writer, obj);
            }
            ms.Position = 0;
            using(StreamReader reader = new StreamReader(ms))
            {
                return (T) serializer.Deserialize(reader, typeof(T));
            }
        }

        public static byte[] FastSerializeToBytes(object obj)
        {
            using MemoryStream ms = new MemoryStream();
            using (StreamWriter textWriter = new StreamWriter(ms, encoding: ENCODING_UTF8_NOBOM, leaveOpen: true))
            {
                JsonSerializer serializer = JsonSerializer.Create();
                serializer.Serialize(textWriter, obj);
            }
            return ms.ToArray();
        }

        public static T FastDeserializeFromBytes<T>(byte[] buffer)
        {
            using MemoryStream ms = new MemoryStream(buffer);
            using(StreamReader reader = new StreamReader(ms, ENCODING_UTF8_NOBOM))
            {
                JsonSerializer serializer = JsonSerializer.Create();
                return (T) serializer.Deserialize(reader, typeof(T))!;
            }
        }
    }
}
