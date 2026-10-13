using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RushRefactor
{
    /// <summary>
    /// IL gate. A pure refactor must not change the opcode sequence of any method. This tool reads the compiled
    /// assemblies straight from disk (no reflection loading), hashes the IL opcodes of every method (operands ignored, so
    /// field/method names do not matter) and compares them with a baseline. A local variable that silently starts pointing at
    /// a field (ldloc becomes ldfld) is caught here even though the code still compiles.
    /// Two sources are checked: the Editor assemblies (Library/ScriptAssemblies) and the WebGL player assembly produced by
    /// PlayerBuildInterface.CompilePlayerScripts, which also covers code inside #if UNITY_WEBGL blocks.
    ///
    /// Stage 2 additions (RushBaseline/il_map.txt) describe the only structural changes that are allowed, and each one is
    /// verified instead of ignored:
    ///   Split|Type|X        X became a thin wrapper that only calls XInternal; XInternal must equal the old X body.
    ///   Move|Type|X         an override X moved to XInternal (no wrapper in this type); body must equal the old X body.
    ///   Rename|Type|old|new method renamed; body must be identical.
    ///   Gone|Type|name      method removed (for example a private auto-property setter).
    ///   Field|Type|m_Field  stfld/stsfld to this field counts as the call to the removed setter it replaced.
    /// </summary>
    public static partial class RushIL
    {
        const string c_EditorBaseline = "il_editor_baseline.txt";
        const string c_PlayerBaseline = "il_player_baseline.txt";
        const string c_DiffFile = "il_diff.txt";
        const string c_AllowFile = "il_allow.txt";
        const string c_MapFile = "il_map.txt";
        const string c_PlayerDir = "playercompile";
        const string c_WrapperPrefix = "W>";

        static readonly string[] s_EditorDlls = { "Assembly-CSharp.dll", "Assembly-CSharp-Editor.dll" };

        static Dictionary<short, OpCode> s_OpCodes;

        public sealed class MapRule
        {
            public string Kind;
            public string Type;
            public string A;
            public string B;
        }

        struct Ins
        {
            public short Op;
            public int Arg;
        }

        public sealed class Result
        {
            public List<string> Missing = new List<string>();
            public List<string> Changed = new List<string>();
            public List<string> Added = new List<string>();
            public int Allowed;
            public int Total;
            public int Mapped;
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
            List<string> editor = ScanEditor(null);
            string err;
            List<string> player = ScanPlayer(null, out err);
            if (player == null) return "ABORT: player compile failed: " + err;
            Write(c_EditorBaseline, editor);
            Write(c_PlayerBaseline, player);
            return "IL BASELINES SAVED (editor=" + editor.Count + " methods, player=" + player.Count + " methods)";
        }

        static string CheckAllCore()
        {
            string stale = StaleReason();
            if (stale != null) return "IL ABORT: " + stale;
            HashSet<string> allow = LoadAllow();
            List<MapRule> map = LoadMap();
            HashSet<string> fields = SetterFields(map);
            HashSet<string> splits = SplitMethods(map);
            var res = new StringBuilder();
            var diff = new StringBuilder();
            bool pass = true;

            List<string> editor = ScanEditor(fields, splits);
            pass &= Report(res, diff, "editor", c_EditorBaseline, editor, allow, map);

            string err;
            List<string> player = ScanPlayer(fields, out err, splits);
            if (player == null) { res.AppendLine("IL player: COMPILE FAILED " + err); pass = false; }
            else pass &= Report(res, diff, "player(WebGL)", c_PlayerBaseline, player, allow, map);

            File.WriteAllText(RushCommon.FileIn(c_DiffFile), diff.ToString(), new UTF8Encoding(false));
            return (pass ? "IL PASS" : "IL FAIL") + "\n" + res;
        }

        static bool Report(StringBuilder res, StringBuilder diff, string label, string baseFile, List<string> cur, HashSet<string> allow, List<MapRule> map)
        {
            string bp = RushCommon.FileIn(baseFile);
            if (!File.Exists(bp)) { res.AppendLine("IL " + label + ": NO BASELINE"); return false; }
            Result r = Diff(File.ReadAllLines(bp), cur, allow, map);
            res.AppendLine("IL " + label + " " + r.Verdict + " (methods=" + r.Total + ", missing=" + r.Missing.Count +
                           ", changed=" + r.Changed.Count + ", added=" + r.Added.Count + ", allowed=" + r.Allowed + ", mapped=" + r.Mapped + ")");
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

        static List<string> ScanEditor(HashSet<string> fields, HashSet<string> splits = null)
        {
            var all = new List<string>();
            foreach (string dll in s_EditorDlls)
                all.AddRange(ScanDll(Path.Combine(ScriptAssembliesDir, dll), dll == "Assembly-CSharp.dll" ? "E" : "EE", fields, splits));
            all.Sort(StringComparer.Ordinal);
            return all;
        }

        static List<string> ScanPlayer(HashSet<string> fields, out string error, HashSet<string> splits = null)
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
            List<string> lines = ScanDll(dll, "P", fields, splits);
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

        public static List<MapRule> LoadMap()
        {
            var list = new List<MapRule>();
            string f = RushCommon.FileIn(c_MapFile);
            if (!File.Exists(f)) return list;
            foreach (string raw in File.ReadAllLines(f))
            {
                string l = raw.Trim();
                if (l.Length == 0 || l.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] p = l.Split('|');
                if (p.Length < 3) continue;
                list.Add(new MapRule { Kind = p[0], Type = p[1], A = p[2], B = p.Length > 3 ? p[3] : null });
            }
            return list;
        }

        public static HashSet<string> SetterFields(List<MapRule> map)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (map == null) return set;
            foreach (MapRule r in map) if (r.Kind == "Field") set.Add(r.Type + "::" + r.A);
            return set;
        }

        public static HashSet<string> SplitMethods(List<MapRule> map)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (map == null) return set;
            foreach (MapRule r in map) if (r.Kind == "Split") set.Add(r.Type + "::" + r.A);
            return set;
        }

        // ------------------------------------------------------------ diff

        public static Result Diff(IList<string> baseline, IList<string> current, HashSet<string> allow)
        {
            return Diff(baseline, current, allow, null);
        }

        public static Result Diff(IList<string> baseline, IList<string> current, HashSet<string> allow, List<MapRule> rules)
        {
            var r = new Result();
            Dictionary<string, string> b = NormalizeGenerated(ToMap(baseline));
            Dictionary<string, string> c = NormalizeGenerated(ToMap(current));
            ApplyRules(b, c, rules, r);
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

        // key format:  tag:Namespace.Type+Nested::method#ordinal
        static bool ParseKey(string key, out string tag, out string type, out string name, out string ord)
        {
            tag = type = name = ord = "";
            int p = key.IndexOf(':');
            if (p <= 0) return false;
            tag = key.Substring(0, p);
            string rest = key.Substring(p + 1);
            int q = rest.LastIndexOf("::", StringComparison.Ordinal);
            if (q < 0) return false;
            type = rest.Substring(0, q);
            string nameOrd = rest.Substring(q + 2);
            int h = nameOrd.LastIndexOf('#');
            if (h < 0) return false;
            name = nameOrd.Substring(0, h);
            ord = nameOrd.Substring(h + 1);
            return true;
        }

        static string MakeKey(string tag, string type, string name, string ord)
        {
            return tag + ":" + type + "::" + name + "#" + ord;
        }

        static List<string> KeysOf(Dictionary<string, string> d, string type, string name)
        {
            var list = new List<string>();
            foreach (string k in d.Keys)
            {
                string tag, t, n, o;
                if (ParseKey(k, out tag, out t, out n, out o) && t == type && n == name) list.Add(k);
            }
            return list;
        }

        static void ApplyRules(Dictionary<string, string> b, Dictionary<string, string> c, List<MapRule> rules, Result r)
        {
            s_Renamed.Clear();
            if (rules == null) return;
            foreach (MapRule rule in rules)
            {
                if (rule.Kind == "Gone")
                {
                    foreach (string k in KeysOf(b, rule.Type, rule.A)) { b.Remove(k); r.Mapped++; }
                    continue;
                }
                if (rule.Kind != "Split" && rule.Kind != "Move" && rule.Kind != "Rename") continue;
                string fromName = rule.Kind == "Rename" ? rule.B : rule.A + "Internal"; // name in the current assembly
                string toName = rule.A;                                                  // name in the baseline
                foreach (string key in KeysOf(c, rule.Type, fromName))
                {
                    string tag, t, n, o;
                    ParseKey(key, out tag, out t, out n, out o);
                    string target = MakeKey(tag, t, toName, o);
                    if (rule.Kind == "Split")
                    {
                        string wv;
                        if (c.TryGetValue(target, out wv) && wv == c_WrapperPrefix + fromName) c.Remove(target);
                        else
                        {
                            r.Changed.Add(target + "  expected a thin wrapper calling " + fromName + ", found " + (wv ?? "no method"));
                            continue;
                        }
                    }
                    else if (c.ContainsKey(target))
                    {
                        r.Changed.Add(target + "  name collision while mapping " + fromName);
                        continue;
                    }
                    string val = c[key];
                    c.Remove(key);
                    c[target] = val;
                    s_Renamed[target] = key;
                    r.Mapped++;
                }
            }
            ApplyReviewed(b, c, rules, r);
        }

        // Roslyn numbers compiler generated types and methods (closures, iterators, lambdas, local functions) by the ordinal of
        // the method that owns them. Removing or adding an unrelated method shifts every number although the bodies are the same.
        // Per outer type the distinct numbers are replaced by their rank, which only changes when a generator really appears or vanishes.
        static readonly Regex s_GenIndex = new Regex(@"(DisplayClass|>d__|>b__|g__[^|<>:#\s]*\|)(\d+)");

        static bool GenMatchValid(string key, Match m)
        {
            string pre = m.Groups[1].Value;
            if (pre == ">d__") return true;
            int next = m.Index + m.Length;
            return next < key.Length && key[next] == '_';
        }

        static string OuterOf(string key)
        {
            int p = key.IndexOf(':');
            string rest = p >= 0 ? key.Substring(p + 1) : key;
            int plus = rest.IndexOf('+');
            int colons = rest.IndexOf("::", StringComparison.Ordinal);
            int cut = plus >= 0 && (colons < 0 || plus < colons) ? plus : colons;
            return cut >= 0 ? rest.Substring(0, cut) : rest;
        }

        static Dictionary<string, string> NormalizeGenerated(Dictionary<string, string> d)
        {
            var perOuter = new Dictionary<string, SortedSet<int>>(StringComparer.Ordinal);
            foreach (string key in d.Keys)
            {
                string outer = OuterOf(key);
                foreach (Match m in s_GenIndex.Matches(key))
                {
                    if (!GenMatchValid(key, m)) continue;
                    SortedSet<int> set;
                    if (!perOuter.TryGetValue(outer, out set)) { set = new SortedSet<int>(); perOuter[outer] = set; }
                    set.Add(int.Parse(m.Groups[2].Value));
                }
            }
            if (perOuter.Count == 0) return d;
            var ranks = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, SortedSet<int>> kv in perOuter) ranks[kv.Key] = kv.Value.ToList();
            var res = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> kv in d)
            {
                string key = kv.Key;
                List<int> list;
                string nk = key;
                if (ranks.TryGetValue(OuterOf(key), out list))
                    nk = s_GenIndex.Replace(key, m => GenMatchValid(key, m) ? m.Groups[1].Value + "~" + list.IndexOf(int.Parse(m.Groups[2].Value)) : m.Value);
                res[nk] = kv.Value;
            }
            return res;
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
            return ScanDll(path, tag, null);
        }

        public static List<string> ScanDll(string path, string tag, HashSet<string> setterFields, HashSet<string> splitMethods = null)
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
                            try
                            {
                                byte[] il = pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes();
                                string wt = ThinWrapperTarget(il, md);
                                fp = (wt != null && wt == name + "Internal" && splitMethods != null && splitMethods.Contains(full + "::" + name)) ? c_WrapperPrefix + wt : Fingerprint(il, md, setterFields, splitMethods);
                                if (s_RevSink != null) s_RevSink[tag + ":" + full + "::" + name + "#" + ord] = ReviewedSig(il, md);
                            }
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

        static string FieldKey(MetadataReader md, int token)
        {
            try
            {
                EntityHandle h = MetadataTokens.EntityHandle(token);
                if (h.Kind == HandleKind.FieldDefinition)
                {
                    FieldDefinition fd = md.GetFieldDefinition((FieldDefinitionHandle)h);
                    return TypeName(md, md.GetTypeDefinition(fd.GetDeclaringType())) + "::" + md.GetString(fd.Name);
                }
            }
            catch (Exception) { }
            return null;
        }

        static string MethodKey(MetadataReader md, int token)
        {
            try
            {
                EntityHandle h = MetadataTokens.EntityHandle(token);
                if (h.Kind == HandleKind.MethodDefinition)
                {
                    MethodDefinition m = md.GetMethodDefinition((MethodDefinitionHandle)h);
                    return TypeName(md, md.GetTypeDefinition(m.GetDeclaringType())) + "::" + md.GetString(m.Name);
                }
            }
            catch (Exception) { }
            return null;
        }

        static string MethodNameOf(MetadataReader md, int token)
        {
            try
            {
                EntityHandle h = MetadataTokens.EntityHandle(token);
                if (h.Kind == HandleKind.MethodDefinition) return md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)h).Name);
                if (h.Kind == HandleKind.MemberReference) return md.GetString(md.GetMemberReference((MemberReferenceHandle)h).Name);
            }
            catch (Exception) { }
            return null;
        }

        static string Fingerprint(byte[] il, MetadataReader md, HashSet<string> setterFields, HashSet<string> splitMethods)
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
                short val = op.Value;
                if (setterFields != null && setterFields.Count > 0 && (op == OpCodes.Stfld || op == OpCodes.Stsfld) && i + 4 <= il.Length)
                {
                    string fk = FieldKey(md, BitConverter.ToInt32(il, i));
                    if (fk != null && setterFields.Contains(fk)) val = OpCodes.Call.Value; // a former call to the removed setter
                }
                if (splitMethods != null && splitMethods.Count > 0 && op == OpCodes.Call && i + 4 <= il.Length)
                {
                    string mk = MethodKey(md, BitConverter.ToInt32(il, i));
                    if (mk != null && splitMethods.Contains(mk)) val = OpCodes.Callvirt.Value; // the former virtual method is now a non-virtual wrapper: call instead of callvirt
                }
                if (s_Getters != null && s_Getters.Count > 0 && (op == OpCodes.Call || op == OpCodes.Callvirt) && i + 4 <= il.Length)
                {
                    string gk = MethodKey(md, BitConverter.ToInt32(il, i));
                    if (gk != null && s_Getters.Contains(gk)) val = OpCodes.Ldfld.Value; // a former read of the public field is now the getter call
                }
                ops.Append(val).Append(';');
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

        static List<Ins> Decode(byte[] il)
        {
            var list = new List<Ins>();
            int i = 0;
            while (i < il.Length)
            {
                short code = il[i++];
                if (code == 0xFE && i < il.Length) code = (short)(0xFE00 | il[i++]);
                var ins = new Ins { Op = code };
                OpCode op;
                if (!s_OpCodes.TryGetValue(code, out op)) { list.Add(ins); continue; }
                switch (op.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: ins.Arg = i < il.Length ? il[i] : 0; i += 1; break;
                    case OperandType.InlineVar: ins.Arg = i + 2 <= il.Length ? BitConverter.ToUInt16(il, i) : 0; i += 2; break;
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
                    default: ins.Arg = i + 4 <= il.Length ? BitConverter.ToInt32(il, i) : 0; i += 4; break;
                }
                list.Add(ins);
            }
            return list;
        }

        static bool IsLdarg(Ins x, int n)
        {
            switch (n)
            {
                case 0: if (x.Op == OpCodes.Ldarg_0.Value) return true; break;
                case 1: if (x.Op == OpCodes.Ldarg_1.Value) return true; break;
                case 2: if (x.Op == OpCodes.Ldarg_2.Value) return true; break;
                case 3: if (x.Op == OpCodes.Ldarg_3.Value) return true; break;
            }
            return (x.Op == OpCodes.Ldarg_S.Value || x.Op == OpCodes.Ldarg.Value) && x.Arg == n;
        }

        /// <summary>
        /// A thin wrapper is: [nop] ldarg.0, ldarg.1 ... ldarg.n, call/callvirt target, [debug-build return shuffle], ret.
        /// Returns the name of the target method, or null when the body is anything else.
        /// </summary>
        static string ThinWrapperTarget(byte[] il, MetadataReader md)
        {
            if (il == null || il.Length == 0 || il.Length > 64) return null;
            List<Ins> ins = Decode(il).Where(x => x.Op != OpCodes.Nop.Value).ToList();
            int i = 0;
            if (i >= ins.Count || !IsLdarg(ins[i], 0)) return null;
            i++;
            int n = 1;
            while (i < ins.Count && IsLdarg(ins[i], n)) { i++; n++; }
            if (i >= ins.Count) return null;
            if (ins[i].Op != OpCodes.Call.Value && ins[i].Op != OpCodes.Callvirt.Value) return null;
            string target = MethodNameOf(md, ins[i].Arg);
            i++;
            if (i + 2 < ins.Count && ins[i].Op == OpCodes.Stloc_0.Value && ins[i + 1].Op == OpCodes.Br_S.Value && ins[i + 2].Op == OpCodes.Ldloc_0.Value) i += 3;
            if (i != ins.Count - 1 || ins[i].Op != OpCodes.Ret.Value) return null;
            return target;
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
            List<string> a = ScanEditor(null);
            List<string> b = ScanEditor(null);
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

            // ---- map rules on synthetic data
            var bs = new List<string> { "E:N.T::X#0\taaa", "E:N.T::X#1\tbbb", "E:N.T::Other#0\tccc", "E:N.T::set_P#0\tddd", "E:N.S::Reset#0\teee" };
            var cs = new List<string> { "E:N.T::X#0\tW>XInternal", "E:N.T::X#1\tW>XInternal", "E:N.T::XInternal#0\taaa", "E:N.T::XInternal#1\tbbb",
                                        "E:N.T::Other#0\tccc", "E:N.S::ResetInternal#0\teee" };
            var rs = new List<MapRule> { new MapRule { Kind = "Split", Type = "N.T", A = "X" }, new MapRule { Kind = "Gone", Type = "N.T", A = "set_P" },
                                         new MapRule { Kind = "Move", Type = "N.S", A = "Reset" } };
            ok &= Expect(sb, "Split + Gone + Move => PASS", Diff(bs, cs, none, rs).Verdict == "PASS(with additions)" || Diff(bs, cs, none, rs).Verdict == "PASS");
            ok &= Expect(sb, "without rules => FAIL", Diff(bs, cs, none, null).Verdict == "FAIL");
            var cbad = new List<string>(cs);
            cbad[4] = "E:N.T::Other#0\tchanged";
            ok &= Expect(sb, "unrelated change with rules => FAIL", Diff(bs, cbad, none, rs).Verdict == "FAIL");
            var cbody = new List<string>(cs);
            cbody[2] = "E:N.T::XInternal#0\tzzz";
            ok &= Expect(sb, "Split with different body => FAIL", Diff(bs, cbody, none, rs).Verdict == "FAIL");
            var cwrap = new List<string>(cs);
            cwrap[0] = "E:N.T::X#0\t3:abcdef";
            ok &= Expect(sb, "Split where X is not a thin wrapper => FAIL", Diff(bs, cwrap, none, rs).Verdict == "FAIL");
            var gb = new List<string> { "E:N.T+<M>d__18::MoveNext#0\taaa", "E:N.T+<>c__DisplayClass12_0::.ctor#0\tbbb", "E:N.T+<>c::<Start>b__12_0#0\tbbb", "E:N.T::Other#0\tccc" };
            var gc = new List<string> { "E:N.T+<M>d__17::MoveNext#0\taaa", "E:N.T+<>c__DisplayClass11_0::.ctor#0\tbbb", "E:N.T+<>c::<Start>b__11_0#0\tbbb", "E:N.T::Other#0\tccc" };
            ok &= Expect(sb, "shifted compiler generated numbers => PASS", Diff(gb, gc, none).Verdict == "PASS");
            var gd = new List<string>(gc);
            gd[0] = "E:N.T+<M>d__17::MoveNext#0\tzzz";
            ok &= Expect(sb, "shifted numbers with changed body => FAIL", Diff(gb, gd, none).Verdict == "FAIL");
            var ge = new List<string>(gc);
            ge.RemoveAt(1);
            ok &= Expect(sb, "vanished generated type => FAIL", Diff(gb, ge, none).Verdict == "FAIL");
            var rename = new List<MapRule> { new MapRule { Kind = "Rename", Type = "N.T", A = "set_P", B = "SetP" } };
            ok &= Expect(sb, "Rename keeps body check => PASS", Diff(new List<string> { "E:N.T::set_P#0\tddd" }, new List<string> { "E:N.T::SetP#0\tddd" }, none, rename).Verdict == "PASS");
            ok &= Expect(sb, "Rename with different body => FAIL", Diff(new List<string> { "E:N.T::set_P#0\tddd" }, new List<string> { "E:N.T::SetP#0\teee" }, none, rename).Verdict == "FAIL");

            string err;
            List<string> p1 = ScanPlayer(null, out err);
            ok &= Expect(sb, "player compile + scan works (" + (p1 == null ? err : p1.Count + " methods") + ")", p1 != null && p1.Count > 300);
            if (p1 != null)
            {
                List<string> p2 = ScanPlayer(null, out err);
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
