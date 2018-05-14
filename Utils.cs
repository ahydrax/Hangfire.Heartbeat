using System.IO;

namespace Hangfire.Heartbeat
{
    public static class Utils
    {
        public static string ReadStringResource(string resourceName)
        {
            var assembly = typeof(Utils).Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
