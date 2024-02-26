using Newtonsoft.Json;

namespace Omukade.Cheyenne.Encoding
{
    public static class FasterJson
    {
        public static T JsonClone<T>(T obj, JsonSerializerSettings? settings = null)
        {
            JsonSerializer serializer = JsonSerializer.Create(settings);
            using MemoryStream ms = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(ms, leaveOpen: true))
            {
                serializer.Serialize(writer, obj);
            }

            using(StreamReader reader = new StreamReader(ms))
            {
                return (T) serializer.Deserialize(reader, typeof(T));
            }
        }
    }
}
