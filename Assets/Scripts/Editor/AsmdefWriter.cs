using System.IO;

namespace DiamondTilt.Core.EditorTools
{
    internal static class AsmdefWriter
    {
        internal static void WriteIfMissing(string path, string json)
        {
            if (File.Exists(path)) return;
            File.WriteAllText(path, json);
        }
    }
}
