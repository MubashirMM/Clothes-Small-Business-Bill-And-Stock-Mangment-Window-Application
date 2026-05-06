using System.IO;
using System.Xml.Serialization;

namespace WpfApp2.Helpers
{
    public class TokenStorage
    {
        private static string TokenFilePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "WpfApp2",
            "tokens.xml"
        );

        public class TokenData
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public int UserId { get; set; }
            public string UserRole { get; set; }
        }

        public static void SaveTokens(TokenData data)
        {
            var directory = Path.GetDirectoryName(TokenFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var serializer = new XmlSerializer(typeof(TokenData));
            using (var writer = new StreamWriter(TokenFilePath))
            {
                serializer.Serialize(writer, data);
            }
        }

        public static TokenData LoadTokens()
        {
            if (!File.Exists(TokenFilePath))
                return new TokenData();

            try
            {
                var serializer = new XmlSerializer(typeof(TokenData));
                using (var reader = new StreamReader(TokenFilePath))
                {
                    return (TokenData)serializer.Deserialize(reader);
                }
            }
            catch
            {
                return new TokenData();
            }
        }

        public static void ClearTokens()
        {
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }
    }
}