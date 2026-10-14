using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;

namespace RushRefactor
{
    /// <summary>
    /// Stage 3 additions to the IL gate (rules in RushBaseline/il_map.txt):
    ///   Get|Type|Prop         a call to Type::get_Prop counts as the ldfld of the public field it replaced.
    ///   Reviewed|Type|Method|ignored,tokens
    ///                         the opcode hash of Method may differ, but its "reviewed signature" (multiset of call targets,
    ///                         string/number literals and fields touched; TryGetComponent counts as GetComponent) must equal the one
    ///                         captured before Stage 3 (il_reviewed_baseline.txt), after removing the explicitly listed tokens
    ///                         from both sides. Every Reviewed line is checked by hand in a before/after text dry run first.
    /// </summary>
    public static partial class RushIL
    {
        const string c_ReviewedFile = "il_reviewed_baseline.txt";

        static HashSet<string> s_Getters;
        static Dictionary<string, string> s_RevSink;
        static Dictionary<string, string> s_RevBase;
        static readonly Dictionary<string, string> s_Renamed = new Dictionary<string, string>(StringComparer.Ordinal);

        public static HashSet<string> GetterCalls(List<MapRule> map)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (map == null) return set;
            foreach (MapRule r in map) if (r.Kind == "Get") set.Add(r.Type + "::get_" + r.A);
            return set;
        }

        public static string CheckAll()
        {
            try
            {
                s_Getters = GetterCalls(LoadMap());
                s_RevBase = LoadReviewedBase();
                s_RevSink = new Dictionary<string, string>(StringComparer.Ordinal);
                return CheckAllCore();
            }
            finally
            {
                s_Getters = null;
                s_RevBase = null;
                s_RevSink = null;
            }
        }

        public static string CaptureReviewed()
        {
            string stale = StaleReason();
            if (stale != null) return "ABORT: " + stale;
            s_RevSink = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                ScanEditor(null);
                string err;
                List<string> player = ScanPlayer(null, out err);
                if (player == null) return "ABORT: player compile failed: " + err;
                List<string> lines = s_RevSink.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + "\t" + kv.Value).ToList();
                File.WriteAllText(RushCommon.FileIn(c_ReviewedFile), string.Join("\n", lines.ToArray()) + "\n", new UTF8Encoding(false));
                return "IL REVIEWED BASELINE SAVED (" + lines.Count + " methods)";
            }
            finally
            {
                s_RevSink = null;
            }
        }

        static Dictionary<string, string> LoadReviewedBase()
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            string f = RushCommon.FileIn(c_ReviewedFile);
            if (!File.Exists(f)) return d;
            foreach (string l in File.ReadAllLines(f))
            {
                int tab = l.IndexOf('\t');
                if (tab <= 0) continue;
                d[l.Substring(0, tab)] = l.Substring(tab + 1);
            }
            return d;
        }

        // ------------------------------------------------------------ rule application

        static HashSet<string> ParseIgnore(string text)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text)) return set;
            foreach (string t in text.Split(';')) if (t.Length > 0 && !t.StartsWith("alias:", StringComparison.Ordinal)) set.Add(t);
            return set;
        }

        // alias:oldToken=newToken  maps a token of the baseline signature to its renamed form (for example a renamed field)
        static Dictionary<string, string> ParseAlias(string text)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text)) return d;
            foreach (string t in text.Split(';'))
            {
                if (!t.StartsWith("alias:", StringComparison.Ordinal)) continue;
                int eq = t.IndexOf('=');
                if (eq < 0) continue;
                d[t.Substring(6, eq - 6)] = t.Substring(eq + 1);
            }
            return d;
        }

        static readonly Regex s_GenOrdinal = new Regex(@"(?<=\+<[^<>]+>d__)\d+");

        // nomor urut kelas iterator buatan compiler (d__31) bergeser kalau jumlah anggota kelas berubah; di signature Reviewed dianggap sama (d__~)
        static string NormTok(string t)
        {
            return s_GenOrdinal.Replace(t, "~");
        }

        static string SigDiff(string baseSig, string curSig, HashSet<string> ignore, Dictionary<string, string> alias)
        {
            var bm = new Dictionary<string, int>(StringComparer.Ordinal);
            var cm = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string raw0 in (baseSig ?? "").Split('~'))
            {
                string a; string raw = NormTok(raw0);
                string t = (alias != null && alias.TryGetValue(raw, out a)) ? a : raw;
                if (t.Length == 0 || ignore.Contains(t)) continue;
                int n; bm.TryGetValue(t, out n); bm[t] = n + 1;
            }
            foreach (string t0 in (curSig ?? "").Split('~')) { string t = NormTok(t0); if (t.Length == 0 || ignore.Contains(t)) continue; int n; cm.TryGetValue(t, out n); cm[t] = n + 1; }
            var onlyB = new List<string>();
            var onlyC = new List<string>();
            foreach (KeyValuePair<string, int> kv in bm) { int n; cm.TryGetValue(kv.Key, out n); for (int i = n; i < kv.Value; i++) onlyB.Add(kv.Key); }
            foreach (KeyValuePair<string, int> kv in cm) { int n; bm.TryGetValue(kv.Key, out n); for (int i = n; i < kv.Value; i++) onlyC.Add(kv.Key); }
            if (onlyB.Count == 0 && onlyC.Count == 0) return null;
            return "before-only [" + string.Join(", ", onlyB.Take(6).ToArray()) + "] after-only [" + string.Join(", ", onlyC.Take(6).ToArray()) + "]";
        }

        static void ApplyReviewed(Dictionary<string, string> b, Dictionary<string, string> c, List<MapRule> rules, Result r)
        {
            if (rules == null) return;
            Dictionary<string, string> revBaseN = s_RevBase == null ? null : NormalizeGenerated(s_RevBase);
            Dictionary<string, string> revSinkN = s_RevSink == null ? null : NormalizeGenerated(s_RevSink);
            foreach (MapRule rule in rules)
            {
                if (rule.Kind != "Reviewed") continue;
                HashSet<string> ignore = ParseIgnore(rule.B);
                Dictionary<string, string> alias = ParseAlias(rule.B);
                foreach (string key in KeysOf(c, rule.Type, rule.A))
                {
                    if (!b.ContainsKey(key)) continue;
                    string curKey;
                    if (!s_Renamed.TryGetValue(key, out curKey)) curKey = key;
                    string cs, bs;
                    if (revBaseN == null || revSinkN == null || !revSinkN.TryGetValue(curKey, out cs) || !revBaseN.TryGetValue(curKey, out bs))
                    {
                        r.Changed.Add(key + "  Reviewed rule has no signature data for " + curKey);
                        continue;
                    }
                    string diff = SigDiff(bs, cs, ignore, alias);
                    if (diff != null) { r.Changed.Add(key + "  reviewed signature differs: " + diff); continue; }
                    b[key] = c[key];
                    r.Mapped++;
                }
            }
        }

        // ------------------------------------------------------------ reviewed signature

        static string EscTok(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("~", "\\x7e").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "");
        }

        static string Hex(byte[] b)
        {
            var sb = new StringBuilder();
            foreach (byte x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }

        static string ParentName(MetadataReader md, EntityHandle p)
        {
            try
            {
                if (p.Kind == HandleKind.TypeReference)
                {
                    TypeReference tr = md.GetTypeReference((TypeReferenceHandle)p);
                    string name = md.GetString(tr.Name);
                    if (tr.ResolutionScope.Kind == HandleKind.TypeReference) return ParentName(md, (TypeReferenceHandle)tr.ResolutionScope) + "+" + name;
                    string ns = md.GetString(tr.Namespace);
                    return ns.Length > 0 ? ns + "." + name : name;
                }
                if (p.Kind == HandleKind.TypeDefinition) return TypeName(md, md.GetTypeDefinition((TypeDefinitionHandle)p));
                if (p.Kind == HandleKind.TypeSpecification) return "spec:" + Hex(md.GetBlobBytes(md.GetTypeSpecification((TypeSpecificationHandle)p).Signature));
            }
            catch (Exception) { }
            return "?";
        }

        static uint ReadCompressed(byte[] b, ref int p)
        {
            byte first = b[p++];
            if ((first & 0x80) == 0) return first;
            if ((first & 0xC0) == 0x80) return (uint)(((first & 0x3F) << 8) | b[p++]);
            uint v = (uint)(((first & 0x1F) << 24) | (b[p] << 16) | (b[p + 1] << 8) | b[p + 2]);
            p += 3;
            return v;
        }

        static string DecodeSigType(MetadataReader md, byte[] b, ref int p)
        {
            if (p >= b.Length) return null;
            byte e = b[p++];
            switch (e)
            {
                case 0x02: return "bool";
                case 0x03: return "char";
                case 0x04: return "sbyte";
                case 0x05: return "byte";
                case 0x06: return "short";
                case 0x07: return "ushort";
                case 0x08: return "int";
                case 0x09: return "uint";
                case 0x0A: return "long";
                case 0x0B: return "ulong";
                case 0x0C: return "float";
                case 0x0D: return "double";
                case 0x0E: return "string";
                case 0x1C: return "object";
                case 0x1D:
                {
                    string inner = DecodeSigType(md, b, ref p);
                    return inner == null ? null : inner + "[]";
                }
                case 0x11:
                case 0x12:
                {
                    uint v = ReadCompressed(b, ref p);
                    int tag = (int)(v & 3);
                    int row = (int)(v >> 2);
                    if (tag == 0) return TypeName(md, md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(row)));
                    if (tag == 1) return ParentName(md, MetadataTokens.TypeReferenceHandle(row));
                    return null;
                }
                default: return null;
            }
        }

        static string DecodeSpec(MetadataReader md, byte[] b)
        {
            try
            {
                if (b.Length < 2 || b[0] != 0x0A) return Hex(b);
                int n = b[1];
                int p = 2;
                var parts = new List<string>();
                for (int k = 0; k < n; k++)
                {
                    string t = DecodeSigType(md, b, ref p);
                    if (t == null) return Hex(b);
                    parts.Add(t);
                }
                return string.Join(",", parts.ToArray());
            }
            catch (Exception) { return Hex(b); }
        }

        static string MethodTok(MetadataReader md, int token)
        {
            try
            {
                EntityHandle h = MetadataTokens.EntityHandle(token);
                switch (h.Kind)
                {
                    case HandleKind.MethodDefinition:
                    {
                        MethodDefinition m = md.GetMethodDefinition((MethodDefinitionHandle)h);
                        return TypeName(md, md.GetTypeDefinition(m.GetDeclaringType())) + "::" + md.GetString(m.Name);
                    }
                    case HandleKind.MemberReference:
                    {
                        MemberReference mr = md.GetMemberReference((MemberReferenceHandle)h);
                        return ParentName(md, mr.Parent) + "::" + md.GetString(mr.Name);
                    }
                    case HandleKind.MethodSpecification:
                    {
                        MethodSpecification ms = md.GetMethodSpecification((MethodSpecificationHandle)h);
                        return MethodTok(md, MetadataTokens.GetToken(ms.Method)) + "<" + DecodeSpec(md, md.GetBlobBytes(ms.Signature)) + ">";
                    }
                }
            }
            catch (Exception) { }
            return "?" + token.ToString("x");
        }

        static string FieldTok(MetadataReader md, int token)
        {
            string fk = FieldKey(md, token);
            if (fk != null) return fk;
            try
            {
                EntityHandle h = MetadataTokens.EntityHandle(token);
                if (h.Kind == HandleKind.MemberReference)
                {
                    MemberReference mr = md.GetMemberReference((MemberReferenceHandle)h);
                    return ParentName(md, mr.Parent) + "::" + md.GetString(mr.Name);
                }
            }
            catch (Exception) { }
            return "?" + token.ToString("x");
        }

        static string ConstToken(OpCode op, int arg, long wide, double real)
        {
            if (op == OpCodes.Ldc_I4_M1) return "num:-1";
            if (op == OpCodes.Ldc_I4_0) return "num:0";
            if (op == OpCodes.Ldc_I4_1) return "num:1";
            if (op == OpCodes.Ldc_I4_2) return "num:2";
            if (op == OpCodes.Ldc_I4_3) return "num:3";
            if (op == OpCodes.Ldc_I4_4) return "num:4";
            if (op == OpCodes.Ldc_I4_5) return "num:5";
            if (op == OpCodes.Ldc_I4_6) return "num:6";
            if (op == OpCodes.Ldc_I4_7) return "num:7";
            if (op == OpCodes.Ldc_I4_8) return "num:8";
            if (op == OpCodes.Ldc_I4_S || op == OpCodes.Ldc_I4) return "num:" + arg;
            if (op == OpCodes.Ldc_I8) return "num:" + wide;
            return "num:" + real.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        static string ReviewedSig(byte[] il, MetadataReader md)
        {
            var toks = new List<string>();
            int i = 0;
            while (i < il.Length)
            {
                short code = il[i++];
                if (code == 0xFE && i < il.Length) code = (short)(0xFE00 | il[i++]);
                OpCode op;
                if (!s_OpCodes.TryGetValue(code, out op)) { toks.Add("?op"); continue; }
                int arg = 0;
                long wide = 0;
                double real = 0;
                switch (op.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineVar: i += 1; break;
                    case OperandType.ShortInlineI: arg = i < il.Length ? (sbyte)il[i] : 0; i += 1; break;
                    case OperandType.InlineVar: i += 2; break;
                    case OperandType.InlineI8: wide = i + 8 <= il.Length ? BitConverter.ToInt64(il, i) : 0; i += 8; break;
                    case OperandType.InlineR: real = i + 8 <= il.Length ? BitConverter.ToDouble(il, i) : 0; i += 8; break;
                    case OperandType.ShortInlineR: real = i + 4 <= il.Length ? BitConverter.ToSingle(il, i) : 0; i += 4; break;
                    case OperandType.InlineSwitch:
                        if (i + 4 <= il.Length) { int n = BitConverter.ToInt32(il, i); i += 4 + 4 * Math.Max(n, 0); }
                        else i = il.Length;
                        break;
                    default: arg = i + 4 <= il.Length ? BitConverter.ToInt32(il, i) : 0; i += 4; break;
                }

                if (op == OpCodes.Call || op == OpCodes.Callvirt || op == OpCodes.Newobj || op == OpCodes.Ldftn || op == OpCodes.Ldvirtftn)
                {
                    string mt = MethodTok(md, arg).Replace("::TryGetComponent", "::GetComponent");
                    toks.Add("call:" + EscTok(mt));
                }
                else if (op == OpCodes.Ldstr)
                {
                    string s = "?";
                    try { s = md.GetUserString(MetadataTokens.UserStringHandle(arg & 0x00FFFFFF)); } catch (Exception) { }
                    toks.Add("lit:" + EscTok(s));
                }
                else if (op == OpCodes.Ldfld || op == OpCodes.Ldflda || op == OpCodes.Stfld || op == OpCodes.Ldsfld || op == OpCodes.Ldsflda || op == OpCodes.Stsfld)
                {
                    toks.Add("fld:" + EscTok(FieldTok(md, arg)));
                }
                else if (op == OpCodes.Ldc_I4 || op == OpCodes.Ldc_I4_S || op == OpCodes.Ldc_I8 || op == OpCodes.Ldc_R4 || op == OpCodes.Ldc_R8 ||
                         op == OpCodes.Ldc_I4_M1 || op == OpCodes.Ldc_I4_0 || op == OpCodes.Ldc_I4_1 || op == OpCodes.Ldc_I4_2 || op == OpCodes.Ldc_I4_3 ||
                         op == OpCodes.Ldc_I4_4 || op == OpCodes.Ldc_I4_5 || op == OpCodes.Ldc_I4_6 || op == OpCodes.Ldc_I4_7 || op == OpCodes.Ldc_I4_8)
                {
                    toks.Add(ConstToken(op, arg, wide, real));
                }
            }
            toks.Sort(StringComparer.Ordinal);
            return string.Join("~", toks.ToArray());
        }

        // ------------------------------------------------------------ self test of the Stage 3 rules (synthetic, no assemblies needed)

        public static string SelfTestReviewed()
        {
            var sb = new StringBuilder();
            bool ok = true;
            var none = new HashSet<string>();
            var bs = new List<string> { "E:N.T::A#0\taaa", "E:N.T::B#0\tbbb" };
            var cs = new List<string> { "E:N.T::A#0\tzzz", "E:N.T::B#0\tbbb" };
            var rules = new List<MapRule> { new MapRule { Kind = "Reviewed", Type = "N.T", A = "A", B = "" } };

            s_RevBase = new Dictionary<string, string> { { "E:N.T::A#0", "call:X::GetComponent<Image>~fld:N.T::m_A~lit:hi" } };
            s_RevSink = new Dictionary<string, string> { { "E:N.T::A#0", "call:X::GetComponent<Image>~fld:N.T::m_A~lit:hi" } };
            ok &= Expect(sb, "Reviewed with equal signatures => PASS", Diff(bs, cs, none, rules).Verdict == "PASS");
            ok &= Expect(sb, "same change without Reviewed rule => FAIL", Diff(bs, cs, none, null).Verdict == "FAIL");

            s_RevSink = new Dictionary<string, string> { { "E:N.T::A#0", "call:X::GetComponent<Image>~call:Y::Extra~fld:N.T::m_A~lit:hi" } };
            ok &= Expect(sb, "Reviewed with an extra call => FAIL", Diff(bs, cs, none, rules).Verdict == "FAIL");

            var rulesIgnore = new List<MapRule> { new MapRule { Kind = "Reviewed", Type = "N.T", A = "A", B = "call:Y::Extra" } };
            ok &= Expect(sb, "Reviewed with the extra call ignored explicitly => PASS", Diff(bs, cs, none, rulesIgnore).Verdict == "PASS");

            s_RevSink = new Dictionary<string, string> { { "E:N.T::A#0", "call:X::GetComponent<Image>~fld:N.T::m_A~lit:changed" } };
            ok &= Expect(sb, "Reviewed with a changed literal => FAIL", Diff(bs, cs, none, rules).Verdict == "FAIL");

            s_RevBase = new Dictionary<string, string>();
            ok &= Expect(sb, "Reviewed without baseline signature => FAIL", Diff(bs, cs, none, rules).Verdict == "FAIL");

            s_RevBase = new Dictionary<string, string> { { "E:N.T+<M>d__18::MoveNext#0", "call:X::A~lit:hi" } };
            s_RevSink = new Dictionary<string, string> { { "E:N.T+<M>d__17::MoveNext#0", "call:X::A~lit:hi" } };
            var itB = new List<string> { "E:N.T+<M>d__18::MoveNext#0\taaa" };
            var itC = new List<string> { "E:N.T+<M>d__17::MoveNext#0\tzzz" };
            var itRules = new List<MapRule> { new MapRule { Kind = "Reviewed", Type = "N.T+<M>d__~0", A = "MoveNext", B = "" } };
            ok &= Expect(sb, "Reviewed on iterator MoveNext (generated index shifted) => PASS", Diff(itB, itC, none, itRules).Verdict == "PASS");
            s_RevSink = new Dictionary<string, string> { { "E:N.T+<M>d__17::MoveNext#0", "call:X::A~call:Y::Extra~lit:hi" } };
            ok &= Expect(sb, "Reviewed on iterator MoveNext with an extra call => FAIL", Diff(itB, itC, none, itRules).Verdict == "FAIL");
            s_RevBase = new Dictionary<string, string> { { "E:N.T+<M>d__18::MoveNext#0", "fld:N.T+<M>d__18::<>1__state~lit:hi" } };
            s_RevSink = new Dictionary<string, string> { { "E:N.T+<M>d__17::MoveNext#0", "fld:N.T+<M>d__17::<>1__state~lit:hi" } };
            ok &= Expect(sb, "iterator tokens with a shifted d__ ordinal => PASS", Diff(itB, itC, none, itRules).Verdict == "PASS");
            s_RevSink = new Dictionary<string, string> { { "E:N.T+<M>d__17::MoveNext#0", "fld:N.T+<M>d__17::<>2__current~lit:hi" } };
            ok &= Expect(sb, "iterator tokens with a different field => FAIL", Diff(itB, itC, none, itRules).Verdict == "FAIL");

            s_RevBase = new Dictionary<string, string> { { "E:N.T::A#0", "fld:N.T::old~lit:hi" } };
            s_RevSink = new Dictionary<string, string> { { "E:N.T::A#0", "fld:N.T::m_Old~lit:hi" } };
            ok &= Expect(sb, "renamed field without alias => FAIL", Diff(bs, cs, none, rules).Verdict == "FAIL");
            var rulesAlias = new List<MapRule> { new MapRule { Kind = "Reviewed", Type = "N.T", A = "A", B = "alias:fld:N.T::old=fld:N.T::m_Old" } };
            ok &= Expect(sb, "renamed field with alias => PASS", Diff(bs, cs, none, rulesAlias).Verdict == "PASS");

            s_RevBase = null;
            s_RevSink = null;
            var getters = GetterCalls(new List<MapRule> { new MapRule { Kind = "Get", Type = "N.S", A = "Start" } });
            ok &= Expect(sb, "GetterCalls collects Type::get_Prop", getters.Contains("N.S::get_Start") && getters.Count == 1);
            ok &= Expect(sb, "token escaping is stable", EscTok("a~b\tc") == "a\\x7eb\\tc");

            sb.Insert(0, ok ? "IL REVIEWED SELFTEST PASS\n" : "IL REVIEWED SELFTEST FAIL\n");
            return sb.ToString();
        }
    }
}
