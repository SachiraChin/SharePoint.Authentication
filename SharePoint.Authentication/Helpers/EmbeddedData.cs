using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SharePoint.Authentication.Helpers
{
    public class EmbeddedData
    {
        public static T Get<T, TS>(string name)
        {
            using (var stream = Get<TS>(name))
            {
                if (typeof(T) == typeof(string))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return (T)(object)reader.ReadToEnd();
                    }
                }

                var xmlSerializer = new XmlSerializer(typeof(T));
                var obj = (T)xmlSerializer.Deserialize(stream ?? throw new InvalidOperationException());
                return obj;
            }
        }

        public static Stream Get<TS>(string name)
        {
            var type = typeof(TS);
            var assembly = Assembly.GetAssembly(type);
            var stream = assembly.GetManifestResourceStream(name);
            if (stream != null)
                return stream;

            var ns = type.Namespace;
            stream = assembly.GetManifestResourceStream($"{ns}.{name}");
            return stream;
        }

    }
}
