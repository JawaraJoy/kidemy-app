using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RushRefactor
{
    /// <summary>Stage 3, read-only part: inventory of the remaining Rush rules (#1 leftovers, #5/#6/#7/#8/#9). Does not touch any game file.</summary>
    public static partial class RushRename
    {
        static string S3Line(Model m, int off)
        {
            int a = Math.Min(off, m.Text.Length);
            while (a > 0 && m.Text[a - 1] != '\n') a--;
            int b = Math.Min(off, m.Text.Length);
            while (b < m.Text.Length && m.Text[b] != '\n' && m.Text[b] != '\r') b++;
            return RushCommon.Clip(m.Text.Substring(a, b - a).Trim(), 150);
        }

        static string S3Span(Model m, int a, int b)
        {
            if (a < 0 || b < a || b >= m.Toks.Count) return "?";
            return m.Text.Substring(m.Toks[a].S, m.Toks[b].E - m.Toks[a].S);
        }

        static string S3Owner(Model m, int k)
        {
            TypeDecl td = m.OwnerType != null ? m.OwnerType[k] : null;
            Member mem = m.MemberAt != null ? m.MemberAt[k] : null;
            string tn = td != null ? td.Name : "?";
            string mn = "";
            if (mem != null)
            {
                if ((mem.Kind == "Method" || mem.Kind == "Property") && mem.Term > 0) mn = m.Toks[mem.Term - 1].T;
                else if (mem.Decls.Count > 0) mn = m.Toks[mem.Decls[0]].T;
            }
            return tn + "." + mn;
        }

        static bool S3InAttr(Member mem, int k)
        {
            if (mem == null) return false;
            foreach (int[] a in mem.Attrs) if (k >= a[0] && k <= a[1]) return true;
            return false;
        }

        static bool S3Trivial(string v)
        {
            string s = v.ToLowerInvariant().TrimEnd('f', 'd', 'm', 'u', 'l');
            return s == "0" || s == "1" || s == "2" || s == "0.0" || s == "1.0" || s == "2.0" || s == "0.5" || s == ".5";
        }

        public static string Stage3Inventory()
        {
            List<Model> models = LoadProjectModels();
            var modelTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (Model m in models) foreach (TypeDecl td in m.Types) modelTypes.Add(td.FullName);

            var refs = new Dictionary<string, int>(StringComparer.Ordinal);
            var writes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Model m in models)
            {
                List<Tok> t = m.Toks;
                for (int i = 1; i + 1 < t.Count; i++)
                {
                    if (t[i].Kind != TK.Ident || t[i - 1].T != ".") continue;
                    Bump(refs, t[i].T);
                    string nx = t[i + 1].T;
                    if (nx == "=" || nx == "+=" || nx == "-=" || nx == "*=" || nx == "/=" || nx == "++" || nx == "--") Bump(writes, t[i].T);
                }
            }

            var fieldRows = new List<string>();
            var staticRows = new List<string>();
            var gcRows = new List<string>();
            var coupRows = new List<string>();
            var hardRows = new List<string>();
            var inhRows = new List<string>();
            var singletonRows = new List<string>();
            var unparsable = new List<string>();
            var gcCount = new Dictionary<string, int>(StringComparer.Ordinal);
            var coupCount = new Dictionary<string, int>(StringComparer.Ordinal);
            var hardStrCallee = new Dictionary<string, int>(StringComparer.Ordinal);
            var hardNumCallee = new Dictionary<string, int>(StringComparer.Ordinal);
            var hardPerFile = new Dictionary<string, int>(StringComparer.Ordinal);
            int pubFieldNonConst = 0, pubFieldDtoLike = 0, pubFieldComponent = 0, pubConst = 0, pubStaticField = 0;
            int staticVoid = 0, staticNonGet = 0, staticTotal = 0, staticExt = 0;
            int hardStr = 0, hardNum = 0, hardNumTrivial = 0, hardNumIndex = 0, hardInit = 0, hardDebugSkipped = 0;

            foreach (Model m in models)
            {
                if (m.Problems.Count > 0) { unparsable.Add(m.Rel); continue; }
                List<Tok> toks = m.Toks;

                // ---- members
                foreach (TypeDecl td in m.Types)
                {
                    if (td.Kind != "class" && td.Kind != "struct") continue;
                    Type rt = RushCommon.FindType(td.FullName);
                    bool unityObj = rt != null && typeof(UnityEngine.Object).IsAssignableFrom(rt);
                    bool serial = rt != null && rt.IsSerializable;
                    string tkind = unityObj
                        ? (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(rt) ? "MonoBehaviour" : typeof(UnityEngine.ScriptableObject).IsAssignableFrom(rt) ? "ScriptableObject" : "UnityObject")
                        : (td.Kind == "struct" ? "struct" : "plain") + (serial ? "+Serializable" : "");

                    // inheritance
                    if (rt != null && rt.BaseType != null && modelTypes.Contains(rt.BaseType.FullName))
                    {
                        int virt = td.Members.Count(x => x.Mods.Contains("virtual") || x.Mods.Contains("abstract"));
                        int ovr = td.Members.Count(x => x.Mods.Contains("override"));
                        inhRows.Add(rt.BaseType.Name + "\t" + td.Name + "\t" + m.Rel + "\tabstract=" + rt.IsAbstract + "\tvirtual/abstract=" + virt + "\toverride=" + ovr + "\tmembers=" + td.Members.Count);
                    }

                    foreach (Member mem in td.Members)
                    {
                        string vis = mem.Mods.Contains("public") ? "public" : mem.Mods.Contains("protected") ? "protected" : mem.Mods.Contains("internal") ? "internal" : "private";
                        if (mem.Kind == "Field" && (vis == "public" || vis == "internal"))
                        {
                            int a0 = mem.HeaderStart + mem.Mods.Count;
                            int b0 = mem.Decls[0] - 1;
                            string ftype = S3Span(m, a0, b0);
                            string attrs = string.Join(" ", mem.Attrs.Select(a => S3Span(m, a[0], a[1])).ToArray());
                            foreach (int d in mem.Decls)
                            {
                                string name = toks[d].T;
                                int r, w; refs.TryGetValue(name, out r); writes.TryGetValue(name, out w);
                                string flags = (mem.Mods.Contains("static") ? "static " : "") + (mem.Mods.Contains("readonly") ? "readonly " : "") + (mem.Mods.Contains("const") ? "const " : "");
                                fieldRows.Add(m.Rel + ":" + LineOf(m, toks[d].S) + "\t" + td.FullName + "\t" + tkind + "\t" + vis + "\t" + flags.Trim() + "\t" + ftype + "\t" + name + "\t" + attrs + "\trefs~" + r + "\twrites~" + w);
                                if (mem.Mods.Contains("const")) pubConst++;
                                else
                                {
                                    pubFieldNonConst++;
                                    if (mem.Mods.Contains("static")) pubStaticField++;
                                    if (unityObj) pubFieldComponent++; else pubFieldDtoLike++;
                                }
                            }
                        }
                        else if (mem.Kind == "Method" && mem.Mods.Contains("static") && mem.Term > 0)
                        {
                            Tok nameTok = toks[mem.Term - 1];
                            if (nameTok.Kind != TK.Ident) continue;
                            if (mem.Term >= 2 && (toks[mem.Term - 2].T == "operator" || toks[mem.Term - 2].T == "~")) continue;
                            int a0 = mem.HeaderStart + mem.Mods.Count;
                            int b0 = mem.Term - 2;
                            if (a0 > b0) continue; // static constructor
                            string ret = S3Span(m, a0, b0);
                            bool isVoid = ret == "void";
                            bool ext = false;
                            int pe = m.Match[mem.Term];
                            if (mem.Term + 1 < toks.Count && toks[mem.Term + 1].T == "this") ext = true;
                            bool isGet = nameTok.T.StartsWith("Get", StringComparison.Ordinal) || nameTok.T.StartsWith("Is", StringComparison.Ordinal) || nameTok.T.StartsWith("Has", StringComparison.Ordinal) || nameTok.T.StartsWith("Try", StringComparison.Ordinal);
                            staticTotal++;
                            if (isVoid) staticVoid++;
                            else if (!isGet) staticNonGet++;
                            if (ext) staticExt++;
                            int r; refs.TryGetValue(nameTok.T, out r);
                            staticRows.Add(m.Rel + ":" + LineOf(m, nameTok.S) + "\t" + td.FullName + "\t" + tkind + "\t" + vis + "\t" + (isVoid ? "VOID" : (isGet ? "get-like" : "non-get")) + (ext ? " ext" : "") + "\t" + ret + " " + nameTok.T + "\trefs~" + r);
                        }
                        else if ((mem.Kind == "Property" || mem.Kind == "Field") && mem.Mods.Contains("static") && mem.Decls.Count + 1 > 0)
                        {
                            string nm = mem.Kind == "Property" && mem.Term > 0 ? toks[mem.Term - 1].T : (mem.Decls.Count > 0 ? toks[mem.Decls[0]].T : "");
                            if (nm == "Instance") singletonRows.Add(m.Rel + ":" + LineOf(m, toks[mem.Start].S) + "\t" + td.FullName + "\t" + tkind + "\t" + vis + "\t" + mem.Kind);
                        }
                    }
                }

                // ---- token walk: GetComponent, coupling, hard code
                for (int k = 0; k < toks.Count; k++)
                {
                    Tok tk = toks[k];
                    Member mem = m.MemberAt != null ? m.MemberAt[k] : null;
                    TypeDecl otd = m.OwnerType != null ? m.OwnerType[k] : null;
                    if (otd == null) continue;

                    if (tk.Kind == TK.Ident)
                    {
                        string n = tk.T;
                        bool isGc = n.StartsWith("GetComponent", StringComparison.Ordinal) || n == "TryGetComponent";
                        if (isGc && k + 1 < toks.Count && (toks[k + 1].T == "<" || toks[k + 1].T == "("))
                        {
                            string gen = "";
                            if (toks[k + 1].T == "<")
                            {
                                int j = k + 2; int depth = 1;
                                while (j < toks.Count && depth > 0) { if (toks[j].T == "<") depth++; else if (toks[j].T == ">") depth--; j++; }
                                gen = S3Span(m, k + 2, Math.Max(k + 2, j - 2));
                            }
                            else if (k + 2 < toks.Count && toks[k + 2].Kind == TK.Str) gen = "STRING " + toks[k + 2].T;
                            string recv = (k >= 2 && toks[k - 1].T == ".") ? toks[k - 2].T : "(this)";
                            gcRows.Add(m.Rel + ":" + LineOf(m, tk.S) + "\t" + S3Owner(m, k) + "\t" + n + "\t<" + gen + ">\trecv=" + recv + "\t" + S3Line(m, tk.S));
                            Bump(gcCount, n);
                        }

                        string prev2 = k >= 2 && toks[k - 1].T == "." ? toks[k - 2].T : "";
                        string key = null;
                        if (n == "FindObjectOfType" || n == "FindObjectsOfType" || n == "FindFirstObjectByType" || n == "FindAnyObjectByType" || n == "FindObjectsByType" ||
                            n == "FindWithTag" || n == "FindGameObjectWithTag" || n == "FindGameObjectsWithTag" || n == "SendMessage" || n == "SendMessageUpwards" || n == "BroadcastMessage" ||
                            n == "GetComponentInParent" || n == "GetComponentsInParent")
                            key = n;
                        else if (n == "Find" && (prev2 == "GameObject" || prev2 == "transform" || prev2 == "Transform")) key = "Find(by name)";
                        else if (n == "Load" && prev2 == "Resources") key = "Resources.Load";
                        else if (n == "main" && prev2 == "Camera") key = "Camera.main";
                        else if ((n == "parent" || n == "root") && prev2.Length > 0 && (prev2 == "transform" || prev2 == "Transform" || prev2.EndsWith("transform", StringComparison.OrdinalIgnoreCase))) key = "transform." + n;
                        else if ((n == "parent" || n == "root") && k >= 1 && toks[k - 1].T == ".") key = "x." + n;
                        else if (n == "Instance" && prev2.Length > 0) key = prev2 + ".Instance";
                        else if (n == "GetChild" || n == "GetSiblingIndex" || n == "SetParent") key = n;
                        if (key != null)
                        {
                            Bump(coupCount, key);
                            coupRows.Add(m.Rel + ":" + LineOf(m, tk.S) + "\t" + S3Owner(m, k) + "\t" + key + "\t" + S3Line(m, tk.S));
                        }
                    }
                    else if ((tk.Kind == TK.Str || tk.Kind == TK.Num) && mem != null)
                    {
                        if (S3InAttr(mem, k)) continue;
                        if (mem.Mods.Contains("const")) continue;
                        if (tk.Kind == TK.Str && (tk.T == "\"\"" || tk.T == "\" \"")) continue;
                        int encl = m.Encl != null ? m.Encl[k] : -1;
                        string callee = "";
                        string openT = encl >= 0 ? toks[encl].T : "";
                        if (openT == "(" && encl >= 1 && toks[encl - 1].Kind == TK.Ident) callee = toks[encl - 1].T;
                        string lineText = S3Line(m, tk.S);
                        if (tk.Kind == TK.Str)
                        {
                            if (lineText.Contains("Debug.") || lineText.Contains("nameof(") || callee == "nameof") { hardDebugSkipped++; continue; }
                            hardStr++;
                            Bump(hardStrCallee, callee.Length == 0 ? "(" + (mem.Kind == "Field" ? "field-init" : "expr") + ")" : callee);
                        }
                        else
                        {
                            if (S3Trivial(tk.T)) { hardNumTrivial++; continue; }
                            if (openT == "[") { hardNumIndex++; continue; }
                            hardNum++;
                            if (mem.Kind == "Field") hardInit++;
                            Bump(hardNumCallee, callee.Length == 0 ? "(" + (mem.Kind == "Field" ? "field-init" : "expr") + ")" : callee);
                        }
                        Bump(hardPerFile, m.Rel);
                        hardRows.Add(m.Rel + ":" + LineOf(m, tk.S) + "\t" + S3Owner(m, k) + "\t" + (tk.Kind == TK.Str ? "STR" : "NUM") + "\t" + RushCommon.Clip(tk.T, 60) + "\t" + callee + "\t" + (mem.Kind == "Field" ? "field-init" : "code") + "\t" + lineText);
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("# STAGE 3 INVENTORY (read-only)");
            sb.AppendLine();
            sb.AppendLine("# [A] PUBLIC/INTERNAL FIELDS (rule #1)  file:line | type | typeKind | vis | flags | fieldType | name | attrs | refs~ | writes~");
            foreach (string s in fieldRows.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine(s);
            sb.AppendLine();
            sb.AppendLine("# [B] STATIC METHODS (rule #7)  file:line | type | typeKind | vis | class | signature | refs~");
            foreach (string s in staticRows.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine(s);
            sb.AppendLine();
            sb.AppendLine("# [B2] STATIC 'Instance' MEMBERS");
            foreach (string s in singletonRows.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine(s);
            sb.AppendLine();
            sb.AppendLine("# [C] GetComponent* CALLS (rule #8)  file:line | owner | method | generic | receiver | source line");
            foreach (string s in gcRows.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine(s);
            sb.AppendLine();
            sb.AppendLine("# [D] COUPLING CANDIDATES (rule #5)  file:line | owner | kind | source line");
            foreach (string s in coupRows.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine(s);
            sb.AppendLine();
            sb.AppendLine("# [E] HARD-CODED LITERALS (rule #6; trivial 0/1/2/0.5, empty strings, Debug lines, attributes, const, array indexes excluded)  file:line | owner | STR/NUM | value | callee | where | source line");
            foreach (string s in hardRows) sb.AppendLine(s);
            sb.AppendLine();
            sb.AppendLine("# [F] PROJECT INHERITANCE (rule #9)  base | derived | file | abstract | virtual/abstract | override | members");
            foreach (string s in inhRows.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine(s);
            sb.AppendLine();
            sb.AppendLine("# unparsable files: " + string.Join(", ", unparsable.ToArray()));
            File.WriteAllText(RushCommon.FileIn("stage3_inventory.txt"), sb.ToString(), new UTF8Encoding(false));

            Func<Dictionary<string, int>, string> fmt = d => string.Join(", ", d.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key + "=" + x.Value).ToArray());
            var o = new StringBuilder();
            o.AppendLine("[A] public/internal non-const fields: " + pubFieldNonConst + " (on Unity objects=" + pubFieldComponent + ", on plain types=" + pubFieldDtoLike + ", static=" + pubStaticField + "); public/internal const: " + pubConst);
            o.AppendLine("[B] static methods: " + staticTotal + " (void=" + staticVoid + ", non-void non-get=" + staticNonGet + ", extension=" + staticExt + "); static Instance members: " + singletonRows.Count);
            o.AppendLine("[C] GetComponent*: " + gcRows.Count + " -> " + fmt(gcCount));
            o.AppendLine("[D] coupling candidates: " + coupRows.Count + " -> " + fmt(coupCount));
            o.AppendLine("[E] hard-coded: strings=" + hardStr + ", numbers=" + hardNum + " (in field initializers=" + hardInit + "), trivial numbers skipped=" + hardNumTrivial + ", array indexes skipped=" + hardNumIndex + ", debug/nameof strings skipped=" + hardDebugSkipped);
            o.AppendLine("    string callees: " + fmt(hardStrCallee));
            o.AppendLine("    number callees: " + fmt(hardNumCallee));
            o.AppendLine("    per file (top 12): " + string.Join(", ", hardPerFile.OrderByDescending(x => x.Value).Take(12).Select(x => Path.GetFileName(x.Key) + "=" + x.Value).ToArray()));
            o.AppendLine("[F] derived classes with a project base class: " + inhRows.Count);
            o.AppendLine("unparsable files: " + unparsable.Count);
            return o.ToString();
        }
    }
}
