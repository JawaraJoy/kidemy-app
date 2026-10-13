using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RushRefactor
{
    /// <summary>
    /// Stage 1 gate. A pure rename must not change the opcode sequence of any method. This tool reads the compiled
    /// assemblies straight from disk (no reflection loading), hashes the IL opcodes of every method (operands ignored, so
    /// field/method names do not matter) and compares them with a baseline. A local variable that silently starts pointing at
    /// a field (ldloc becomes ldfld) is caught here even though the code still compiles.
    /// Two sources are checked: the Editor assemblies (Library/ScriptAssemblies) and the WebGL player assembly produced by
    /// PlayerBuildInterface.CompilePlayerScripts, which also covers code inside #if UNITY_WEBGL blocks.
    /// </summary>
    public static class RushIL
    {
        const string c_EditorBaseline = "il_editor_baseline.txt";
        const string c_PlayerBaseline = "il_player_baseline.txt";
        const string c_DiffFile = "il_diff.txt";
        const string c_AllowFile = "il_allow.txt";
        const string c_PlayerDir = "playercompile";

        static readonly string[] s_EditorDlls = { "Assembly-CSharp.dll", "Assembly-CSharp-Editor.dll" };

        static Dictionary<short, OpCode> s_OpCodes;

        public sealed class Result
        {
            public List<string> Missing = new List<string>();
            public List<string> Changed = new List<string>();
            public List<string> Added = new List<string>();
            public int Allowed;
            public int Total;
            public string Verdict
            {
                get { return Missing.Count > 0 || Changed.Count > 0 ? "FAIL" : (Added.Count > 0 ? "PASS(with additions)" : "PASS"); }
            }
        }

        // ------------------------------------------------------------ menu

        [MenuItem("Tools/Rush Refactor/IL/Capture Baselines (editor + WebGL player)")]
        static void MenuCapture() { Debug.Log("[Rush] " + CaptureBaselines()); }

        [MenuItem("Tools/Rush Refactor/IL/Check Against Baselines")]
        static void MenuCheck() { Debug.Log("[Rush] " + CheckAll()); }

        [MenuItem("Tools/Rush Refactor/IL/Self Test")]
        static void MenuSelfTest() { Debug.Log("[Rush] " + SelfTest()); }

        // ------------------------------------------------------------ public API

        public static string CaptureBaselines()
        {
            string stale = StaleReason();
            if (stale != null) return "ABORT: " + stale;
            List<string> editor = ScanEditor();
            string err;
            List<string> player = ScanPlayer(out err);
            if (player == null) return "ABORT: player compile failed: " + err;
            Write(c_EditorBaseline, editor);
            Write(c_PlayerBaseline, player);
            return "IL BASELINES SAVED (editor=" + editor.Count + " methods, player=" + player.Count + " methods)";
        }

        public static string CheckAll()
        {
            string stale = StaleReason();
            if (stale != null) return "IL ABORT: " + stale;
            HashSet<string> allow = LoadAllow();
            var res = new StringBuilder();
            var diff = new StringBuilder();
            bool pass = true;

            List<string> editor = ScanEditor();
            pass &= Report(res, diff, "editor", c_EditorBaseline, editor, allow);

            string err;
            List<string> player = ScanPlayer(out err);
            if (player == null) { res.AppendLine("IL player: COMPILE FAILED " + err); pass = false; }
            else pass &= Report(res, diff, "player(WebGL)", c_PlayerBaseline, player, allow);

            File.WriteAllText(RushCommon.FileIn(c_DiffFile), diff.ToString(), new UTF8Encoding(false));
            return (pass ? "IL PASS" : "IL FAIL") + "\n" + res;
        }

        static bool Report(StringBuilder res, StringBuilder diff, string label, string baseFile, List<string> cur, HashSet<string> allow)
        {
            string bp = RushCommon.FileIn(baseFile);
            if (!File.Exists(bp)) { res.AppendLine("IL " + label + ": NO BASELINE"); return false; }
            Result r = Diff(File.ReadAllLines(bp), cur, allow);
            res.AppendLine("IL " + label + " " + r.Verdict + " (methods=" + r.Total + ", missing=" + r.Missing.Count +
                           ", changed=" + r.Changed.Count + ", added=" + r.Added.Count + ", allowed=" + r.Allowed + ")");
            foreach (string s in r.Missing.Take(10)) res.AppendLine("  MISSING " + RushCommon.Clip(s, 200));
            foreach (string s in r.Changed.Take(15)) res.AppendLine("  CHANGED " + RushCommon.Clip(s, 200));
            foreach (string s in r.Missing) diff.AppendLine(label + "\tMISSING\t" + s);
            foreach (string s in r.Changed) diff.AppendLine(label + "\tCHANGED\t" + s);
            foreach (string s in r.Added) diff.AppendLine(label + "\tADDED\t" + s);
            return r.Missing.Count == 0 && r.Changed.Count == 0;
        }

        // ------------------------------------------------------------ sources

        static string ScriptAssembliesDir
        {
            get { return Path.Combine(RushCommon.ProjectRoot, "Library/ScriptAssemblies"); }
        }

        static string PlayerDir
        {
            get
            {
                string d = Path.Combine(RushCommon.BaselineDir, c_PlayerDir);
                Directory.CreateDirectory(d);
                return d;
            }
        }

        /// <summary>Returns a reason when compiled output may be older than the source, otherwise null.</summary>
        static string StaleReason()
        {
            if (EditorApplication.isCompiling) return "editor is compiling";
            if (EditorApplication.isUpdating) return "asset database is updating";
            DateTime newestSource = DateTime.MinValue;
            foreach (string root in RushCommon.AssetRoots)
            {
                string abs = Path.Combine(RushCommon.ProjectRoot, root);
                foreach (string f in Directory.EnumerateFiles(abs, "*.cs", SearchOption.AllDirectories))
                {
                    DateTime t = File.GetLastWriteTimeUtc(f);
                    if (t > newestSource) newestSource = t;
                }
            }
            foreach (string dll in s_EditorDlls)
            {
                string p = Path.Combine(ScriptAssembliesDir, dll);
                if (!File.Exists(p)) return dll + " not found in Library/ScriptAssemblies";
                if (dll == "Assembly-CSharp.dll" && File.GetLastWriteTimeUtc(p) < newestSource)
                    return "Assembly-CSharp.dll is older than the newest source file (compile not finished?)";
            }
            return null;
        }

        static List<string> ScanEditor()
        {
            var all = new List<string>();
            foreach (string dll in s_EditorDlls)
                all.AddRange(ScanDll(Path.Combine(ScriptAssembliesDir, dll), dll == "Assembly-CSharp.dll" ? "E" : "EE"));
            all.Sort(StringComparer.Ordinal);
            return all;
        }

        static List<string> ScanPlayer(out string error)
        {
            error = "";
            var settings = new UnityEditor.Build.Player.ScriptCompilationSettings();
            settings.group = BuildTargetGroup.WebGL;
            settings.target = BuildTarget.WebGL;
            settings.options = UnityEditor.Build.Player.ScriptCompilationOptions.None;
            string dir = PlayerDir;
            string dll = Path.Combine(dir, "Assembly-CSharp.dll");
            foreach (string stale in Directory.GetFiles(dir, "Assembly-CSharp.*")) File.Delete(stale); // never read an old copy
            UnityEditor.Build.Player.ScriptCompilationResult result;
            try { result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, dir); }
            catch (Exception e) { error = e.GetType().Name + ": " + e.Message; return null; }
            if (result.assemblies == null || !result.assemblies.Contains("Assembly-CSharp.dll")) { error = "no Assembly-CSharp.dll produced"; return null; }
            if (!File.Exists(dll)) { error = "player dll missing after compile (compile errors? see console)"; return null; }
            List<string> lines = ScanDll(dll, "P");
            lines.Sort(StringComparer.Ordinal);
            return lines;
        }

        static void Write(string name, List<string> lines)
        {
            File.WriteAllText(RushCommon.FileIn(name), string.Join("\n", lines.ToArray()) + "\n", new UTF8Encoding(false));
        }

        static HashSet<string> LoadAllow()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            string f = RushCommon.FileIn(c_AllowFile);
            if (!File.Exists(f)) return set;
            foreach (string raw in File.ReadAllLines(f))
            {
                string l = raw.Trim();
                if (l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal)) set.Add(l);
            }
            return set;
        }

        // ------------------------------------------------------------ diff

        public static Result Diff(IList<string> baseline, IList<string> current, HashSet<string> allow)
        {
            var r = new Result();
            var b = ToMap(baseline);
            var c = ToMap(current);
            r.Total = c.Count;
            foreach (KeyValuePair<string, string> kv in b)
            {
                string cv;
                if (!c.TryGetValue(kv.Key, out cv)) { if (IsAllowed(allow, kv.Key)) r.Allowed++; else r.Missing.Add(kv.Key); }
                else if (cv != kv.Value) { if (IsAllowed(allow, kv.Key)) r.Allowed++; else r.Changed.Add(kv.Key + "  " + kv.Value + " => " + cv); }
            }
            foreach (string k in c.Keys) if (!b.ContainsKey(k)) r.Added.Add(k);
            return r;
        }

        // An allow entry is either an exact key or a prefix ending with '*' (for example "E:EduGame.SceneLoader::*").
        static bool IsAllowed(HashSet<string> allow, string key)
        {
            if (allow.Count == 0) return false;
            if (allow.Contains(key)) return true;
            foreach (string a in allow)
            {
                if (a.EndsWith("*", StringComparison.Ordinal) && key.StartsWith(a.Substring(0, a.Length - 1), StringComparison.Ordinal)) return true;
            }
            return false;
        }

        static Dictionary<string, string> ToMap(IList<string> lines)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string l in lines)
            {
                int tab = l.LastIndexOf('\t');
                if (tab <= 0) continue;
                d[l.Substring(0, tab)] = l.Substring(tab + 1);
            }
            return d;
        }

        // ------------------------------------------------------------ PE scanning

        public static List<string> ScanDll(string path, string tag)
        {
            EnsureOpCodes();
            var lines = new List<string>();
            byte[] bytes = File.ReadAllBytes(path);
            using (var ms = new MemoryStream(bytes))
            using (var pe = new PEReader(ms))
            {
                MetadataReader md = pe.GetMetadataReader();
                foreach (TypeDefinitionHandle th in md.TypeDefinitions)
                {
                    TypeDefinition td = md.GetTypeDefinition(th);
                    string full = TypeName(md, td);
                    if (full.StartsWith("RushRefactor", StringComparison.Ordinal) || full == "<Module>") continue;
                    var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (MethodDefinitionHandle mh in td.GetMethods())
                    {
                        MethodDefinition m = md.GetMethodDefinition(mh);
                        string name = md.GetString(m.Name);
                        int ord;
                        ordinals.TryGetValue(name, out ord);
                        ordinals[name] = ord + 1;
                        string fp = "nobody";
                        if (m.RelativeVirtualAddress != 0)
                        {
                            try { fp = Fingerprint(pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes()); }
                            catch (Exception) { fp = "unreadable"; }
                        }
                        lines.Add(tag + ":" + full + "::" + name + "#" + ord + "\t" + fp);
                    }
                }
            }
            return lines;
        }

        static string TypeName(MetadataReader md, TypeDefinition td)
        {
            string name = md.GetString(td.Name);
            TypeDefinitionHandle outer = td.GetDeclaringType();
            if (!outer.IsNil) return TypeName(md, md.GetTypeDefinition(outer)) + "+" + name;
            string ns = md.GetString(td.Namespace);
            return ns.Length > 0 ? ns + "." + name : name;
        }

        static string Fingerprint(byte[] il)
        {
            if (il == null) return "nobody";
            var ops = new StringBuilder();
            int count = 0;
            int i = 0;
            while (i < il.Length)
            {
                short code = il[i++];
                if (code == 0xFE && i < il.Length) code = (short)(0xFE00 | il[i++]);
                OpCode op;
                if (!s_OpCodes.TryGetValue(code, out op)) { ops.Append('?').Append(code).Append(';'); count++; continue; }
                ops.Append(op.Value).Append(';');
                count++;
                switch (op.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: i += 1; break;
                    case OperandType.InlineVar: i += 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: i += 8; break;
                    case OperandType.InlineSwitch:
                        if (i + 4 <= il.Length)
                        {
                            int n = BitConverter.ToInt32(il, i);
                            i += 4 + 4 * Math.Max(n, 0);
                        }
                        else i = il.Length;
                        break;
                    default: i += 4; break;
                }
            }
            using (MD5 md5 = MD5.Create())
            {
                byte[] h = md5.ComputeHash(Encoding.ASCII.GetBytes(ops.ToString()));
                var hex = new StringBuilder();
                for (int k = 0; k < 8; k++) hex.Append(h[k].ToString("x2"));
                return count + ":" + hex;
            }
        }

        static void EnsureOpCodes()
        {
            if (s_OpCodes != null) return;
            s_OpCodes = new Dictionary<short, OpCode>();
            foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fi.FieldType != typeof(OpCode)) continue;
                var oc = (OpCode)fi.GetValue(null);
                s_OpCodes[oc.Value] = oc;
            }
        }

        // ------------------------------------------------------------ self test

        public static string SelfTest()
        {
            var sb = new StringBuilder();
            bool ok = true;
            string stale = StaleReason();
            if (stale != null) return "IL SELFTEST ABORT: " + stale;
            List<string> a = ScanEditor();
            List<string> b = ScanEditor();
            ok &= Expect(sb, "two editor scans identical", a.SequenceEqual(b));
            ok &= Expect(sb, "editor scan has methods (" + a.Count + ")", a.Count > 500);
            ok &= Expect(sb, "scan includes GameManager.Submit", a.Any(l => l.StartsWith("E:EduGame.GameManager::Submit#0", StringComparison.Ordinal)));

            var none = new HashSet<string>();
            ok &= Expect(sb, "identical => PASS", Diff(a, a, none).Verdict == "PASS");

            var altered = new List<string>(a);
            int idx = altered.FindIndex(l => l.StartsWith("E:EduGame.GameManager::Submit#0", StringComparison.Ordinal));
            if (idx >= 0)
            {
                string[] p = altered[idx].Split('\t');
                altered[idx] = p[0] + "\tdeadbeef";
                Result r = Diff(a, altered, none);
                ok &= Expect(sb, "changed fingerprint => FAIL", r.Verdict == "FAIL" && r.Changed.Count == 1);
                ok &= Expect(sb, "allow exact key => PASS", Diff(a, altered, new HashSet<string> { p[0] }).Verdict == "PASS");
                ok &= Expect(sb, "allow prefix* => PASS", Diff(a, altered, new HashSet<string> { "E:EduGame.GameManager::*" }).Verdict == "PASS");
            }
            var removed = new List<string>(a);
            removed.RemoveAt(3);
            ok &= Expect(sb, "removed method => FAIL", Diff(a, removed, none).Verdict == "FAIL");

            string err;
            List<string> p1 = ScanPlayer(out err);
            ok &= Expect(sb, "player compile + scan works (" + (p1 == null ? err : p1.Count + " methods") + ")", p1 != null && p1.Count > 300);
            if (p1 != null)
            {
                List<string> p2 = ScanPlayer(out err);
                ok &= Expect(sb, "two player scans identical", p2 != null && p1.SequenceEqual(p2));
            }

            sb.Insert(0, ok ? "IL SELFTEST PASS\n" : "IL SELFTEST FAIL\n");
            return sb.ToString();
        }

        static bool Expect(StringBuilder sb, string what, bool cond)
        {
            sb.AppendLine((cond ? "  ok   " : "  FAIL ") + what);
            return cond;
        }
    }
}
