using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RushRefactor
{
    /// <summary>Stage 2, read-only part: inventory of rule #2 (get+set properties) and rule #3 (public methods called inside their own class).</summary>
    public static partial class RushRename
    {
        sealed class PropRow
        {
            public string File;
            public int Line;
            public string Type;
            public string Name;
            public string PropType;
            public string Vis;
            public string SetVis;
            public bool Auto;
            public bool Static;
            public bool Init;
            public int ExternalWrites;
        }

        sealed class MethodRow
        {
            public string File;
            public int Line;
            public string Type;
            public string Name;
            public string Flags;
            public int Overloads;
            public int LocalCalls;
            public int GroupRefs;
            public int ExternalCalls;
            public bool Static;
        }

        static void Bump(Dictionary<string, int> d, string key)
        {
            int c;
            d.TryGetValue(key, out c);
            d[key] = c + 1;
        }

        static HashSet<string> InterfaceMethodNames(TypeDecl td)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            Type t = RushCommon.FindType(td.FullName);
            if (t == null) return set;
            foreach (Type i in t.GetInterfaces())
                foreach (MethodInfo mi in i.GetMethods()) set.Add(mi.Name);
            return set;
        }

        public static string Stage2Inventory()
        {
            List<Model> models = LoadProjectModels();
            var extCalls = new Dictionary<string, int>(StringComparer.Ordinal);
            var writes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Model m in models)
            {
                List<Tok> t = m.Toks;
                for (int i = 1; i + 1 < t.Count; i++)
                {
                    if (t[i].Kind != TK.Ident || t[i - 1].T != ".") continue;
                    string nx = t[i + 1].T;
                    if (nx == "(") Bump(extCalls, t[i].T);
                    else if (nx == "=" || nx == "+=" || nx == "-=" || nx == "*=" || nx == "/=" || nx == "++" || nx == "--") Bump(writes, t[i].T);
                }
            }

            var props = new List<PropRow>();
            var meths = new List<MethodRow>();
            var unparsable = new List<string>();
            foreach (Model m in models)
            {
                if (m.Problems.Count > 0) { unparsable.Add(m.Rel); continue; }
                List<Tok> toks = m.Toks;
                foreach (TypeDecl td in m.Types)
                {
                    if (td.Kind != "class" && td.Kind != "struct") continue;
                    var declToks = new HashSet<int>();
                    foreach (Member mm in td.Members) if (mm.Kind == "Method" && mm.Term > 0) declToks.Add(mm.Term - 1);
                    HashSet<string> ifaceNames = InterfaceMethodNames(td);

                    foreach (Member mem in td.Members)
                    {
                        string vis = mem.Mods.Contains("public") ? "public" : mem.Mods.Contains("protected") ? "protected" :
                                     mem.Mods.Contains("internal") ? "internal" : "private";
                        if (mem.Kind == "Property" && toks[mem.Term].T == "{")
                        {
                            int open = mem.Term;
                            int close = m.Match[open];
                            bool hasGet = false, hasSet = false, init = false, auto = false;
                            string setVis = "";
                            for (int k = open + 1; k < close; k++)
                            {
                                if (m.Encl[k] != open || toks[k].Kind != TK.Ident) continue;
                                string a = toks[k].T;
                                if (a == "get") { hasGet = true; if (toks[k + 1].T == ";") auto = true; }
                                else if (a == "set" || a == "init")
                                {
                                    hasSet = true;
                                    init = a == "init";
                                    string pv = toks[k - 1].Kind == TK.Ident ? toks[k - 1].T : "";
                                    setVis = (pv == "private" || pv == "protected" || pv == "internal" || pv == "public") ? pv : "same";
                                }
                            }
                            if (!(hasGet && hasSet)) continue;
                            Tok nameTok = toks[mem.Term - 1];
                            string name = nameTok.Kind == TK.Ident ? nameTok.T : "this[]";
                            int a0 = mem.HeaderStart + mem.Mods.Count;
                            int b0 = mem.Term - 2;
                            string ptype = (a0 <= b0 && b0 < toks.Count) ? m.Text.Substring(toks[a0].S, toks[b0].E - toks[a0].S) : "?";
                            int w;
                            writes.TryGetValue(name, out w);
                            props.Add(new PropRow
                            {
                                File = m.Rel, Line = LineOf(m, nameTok.S), Type = td.Name, Name = name, PropType = ptype, Vis = vis, SetVis = setVis,
                                Auto = auto, Static = mem.Mods.Contains("static"), Init = init, ExternalWrites = w
                            });
                        }
                        else if (mem.Kind == "Method" && vis == "public")
                        {
                            Tok nameTok = toks[mem.Term - 1];
                            if (nameTok.Kind != TK.Ident || nameTok.T == td.Name) continue;
                            if (mem.Term >= 2 && (toks[mem.Term - 2].T == "operator" || toks[mem.Term - 2].T == "~")) continue;
                            string name = nameTok.T;
                            int local = 0, grp = 0;
                            for (int k = td.Open + 1; k < td.Close; k++)
                            {
                                if (toks[k].Kind != TK.Ident || toks[k].T != name || m.OwnerType[k] != td || declToks.Contains(k)) continue;
                                Tok prev = toks[k - 1];
                                if (prev.T == ".")
                                {
                                    Tok q = k >= 2 ? toks[k - 2] : null;
                                    if (q == null || q.T != "this") continue;
                                }
                                if (prev.T == "(" && k >= 2 && toks[k - 2].T == "nameof") continue;
                                if (k + 1 < toks.Count && toks[k + 1].T == "(") local++; else grp++;
                            }
                            var flags = new List<string>();
                            if (mem.Mods.Contains("static")) flags.Add("static");
                            if (mem.Mods.Contains("virtual")) flags.Add("virtual");
                            if (mem.Mods.Contains("override")) flags.Add("override");
                            if (mem.Mods.Contains("abstract")) flags.Add("abstract");
                            if (ifaceNames.Contains(name)) flags.Add("iface");
                            int overloads = td.Members.Count(x => x.Kind == "Method" && x.Term > 0 && toks[x.Term - 1].T == name);
                            int ec;
                            extCalls.TryGetValue(name, out ec);
                            meths.Add(new MethodRow
                            {
                                File = m.Rel, Line = LineOf(m, nameTok.S), Type = td.Name, Name = name, Flags = string.Join(",", flags.ToArray()),
                                Overloads = overloads, LocalCalls = local, GroupRefs = grp, ExternalCalls = ec, Static = mem.Mods.Contains("static")
                            });
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("# PROPERTIES with get AND set (rule #2)");
            sb.AppendLine("# file:line\ttype\tname\tpropType\tvis\tsetVis\tauto\tstatic\tinit\texternalWrites(approx by name)");
            foreach (PropRow p in props.OrderBy(x => x.File, StringComparer.Ordinal).ThenBy(x => x.Line))
                sb.AppendLine(p.File + ":" + p.Line + "\t" + p.Type + "\t" + p.Name + "\t" + p.PropType + "\t" + p.Vis + "\t" + p.SetVis + "\t" +
                              p.Auto + "\t" + p.Static + "\t" + p.Init + "\t" + p.ExternalWrites);
            sb.AppendLine();
            sb.AppendLine("# PUBLIC METHODS (rule #3)");
            sb.AppendLine("# file:line\ttype\tname\tflags\toverloads\tlocalCalls\tgroupRefs\texternalCalls(approx by name)");
            foreach (MethodRow r in meths.OrderBy(x => x.File, StringComparer.Ordinal).ThenBy(x => x.Line))
                sb.AppendLine(r.File + ":" + r.Line + "\t" + r.Type + "\t" + r.Name + "\t" + r.Flags + "\t" + r.Overloads + "\t" + r.LocalCalls + "\t" + r.GroupRefs + "\t" + r.ExternalCalls);
            File.WriteAllText(RushCommon.FileIn("stage2_inventory.txt"), sb.ToString(), new UTF8Encoding(false));

            List<MethodRow> need = meths.Where(x => x.LocalCalls + x.GroupRefs > 0).ToList();
            var o = new StringBuilder();
            o.AppendLine("PROPS get+set: " + props.Count + " (auto=" + props.Count(x => x.Auto) + ", custom=" + props.Count(x => !x.Auto) +
                         ", private set=" + props.Count(x => x.SetVis == "private") + ", static=" + props.Count(x => x.Static) +
                         ", init=" + props.Count(x => x.Init) + ", with external writes=" + props.Count(x => x.ExternalWrites > 0) + ")");
            o.AppendLine("PUBLIC METHODS: " + meths.Count + " | called/referenced inside own class: " + need.Count +
                         " (local call sites=" + need.Sum(x => x.LocalCalls) + ", method-group refs=" + need.Sum(x => x.GroupRefs) + ")");
            o.AppendLine("  of those: plain=" + need.Count(x => x.Flags == "") + ", static=" + need.Count(x => x.Flags == "static") +
                         ", virtual/override/abstract=" + need.Count(x => x.Flags.Contains("virtual") || x.Flags.Contains("override") || x.Flags.Contains("abstract")) +
                         ", interface impl=" + need.Count(x => x.Flags.Contains("iface")) + ", overloaded=" + need.Count(x => x.Overloads > 1));
            o.AppendLine("unparsable files: " + unparsable.Count);
            return o.ToString();
        }
    }
}
