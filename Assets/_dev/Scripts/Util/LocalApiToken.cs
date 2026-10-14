#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace EduGame
{
    /// <summary>
    /// Membaca token API lokal untuk Editor dari berkas ApiToken.local.txt di root project.
    /// Berkas itu di-.gitignore sehingga token tidak ikut ter-commit. Kelas ini tidak ikut build.
    /// </summary>
    public static class LocalApiToken
    {
        private const string c_FileName = "ApiToken.local.txt";

        public static string Read()
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string filePath = Path.Combine(projectPath, c_FileName);

            if (!File.Exists(filePath))
                return string.Empty;

            return File.ReadAllText(filePath).Trim();
        }
    }
}
#endif
