using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RushRefactor
{
    /// <summary>Shared paths and helpers for the Rush refactor safety net (Editor only, no gameplay impact).</summary>
    public static class RushCommon
    {
        static readonly string[] s_AssetRoots = { "Assets/_dev", "Assets/_dev2" };
        static readonly Dictionary<string, Type> s_TypeCache = new Dictionary<string, Type>();
        static readonly CultureInfo s_Inv = CultureInfo.InvariantCulture;

        public static string[] AssetRoots { get { return s_AssetRoots; } }
        public static CultureInfo Inv { get { return s_Inv; } }

        public static string ProjectRoot
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); }
        }

        public static string BaselineDir
        {
            get
            {
                string dir = Path.Combine(ProjectRoot, "RushBaseline");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string FileIn(string name)
        {
            return Path.Combine(BaselineDir, name);
        }

        public static bool IsProjectPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            foreach (string root in s_AssetRoots)
            {
                if (assetPath.StartsWith(root + "/", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static bool IsProjectAssembly(string name)
        {
            string[] skip =
            {
                "Unity", "System", "mscorlib", "netstandard", "Mono.", "Microsoft", "nunit", "Newtonsoft",
                "Bee.", "ExCSS", "JetBrains", "SyntaxTree", "ICSharpCode", "Accessibility", "Library",
                "I18N", "Google", "spine", "PsdPlugin", "NaughtyAttributes", "EasyTextEffects", "MCPForUnity",
                "ScriptablePacker", "mcp-codedom", "Anonymously"
            };
            foreach (string s in skip)
            {
                if (name.StartsWith(s, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        public static IEnumerable<Type> ProjectTypes()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsProjectAssembly(asm.GetName().Name)) continue;
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(x => x != null).ToArray(); }
                catch (Exception) { continue; }
                foreach (Type t in types) yield return t;
            }
        }

        public static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            Type cached;
            if (s_TypeCache.TryGetValue(fullName, out cached)) return cached;
            Type found = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsProjectAssembly(asm.GetName().Name)) continue;
                try { found = asm.GetType(fullName, false); }
                catch (Exception) { found = null; }
                if (found != null) break;
            }
            s_TypeCache[fullName] = found;
            return found;
        }

        public static string Esc(string v)
        {
            if (v == null) return "";
            return v.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "").Replace("\n", "\\n");
        }

        public static string F(float f) { return f.ToString("R", s_Inv); }
        public static string F(double d) { return d.ToString("R", s_Inv); }

        public static string Clip(string s, int max)
        {
            if (s == null) return "";
            s = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            return s.Length > max ? s.Substring(0, max) : s;
        }
    }
}
