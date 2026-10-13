using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace RushRefactor
{
    /// <summary>
    /// Stage 0 data gate. Dumps the deserialized state of every scene, prefab and ScriptableObject under the
    /// project roots into a flat text file, so a refactor can prove no serialized value was lost or changed.
    /// Read-only: scenes are opened additively and closed without saving. Runs in slices from EditorApplication.update.
    /// Line format: KIND \t asset \t goPath \t component#index \t propertyPath \t value
    /// </summary>
    [InitializeOnLoad]
    public static class RushSnapshot
    {
        const string c_RunKey = "RushSnapshot.Running";
        const string c_BaselineFile = "snapshot_baseline.txt";
        const string c_CurrentFile = "snapshot_current.txt";
        const string c_DiffFile = "snapshot_diff.txt";
        const int c_SliceMs = 300;
        const int c_BigArray = 2000;
        const int c_MaxString = 4000;
        const int c_MaxDiffLines = 20000;

        enum RunState { Idle, Running, Done, Error }

        static readonly HashSet<string> s_SkipProps = new HashSet<string>
        {
            "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
            "m_GameObject", "m_EditorHideFlags", "m_EditorClassIdentifier"
        };

        static RunState s_State = RunState.Idle;
        static List<string> s_Queue = new List<string>();
        static int s_Index;
        static StreamWriter s_Writer;
        static string s_OutPath = "";
        static string s_Message = "";
        static string s_Current = "";
        static long s_Lines;
        static int s_Scenes, s_Prefabs, s_Assets, s_GameObjects, s_Components, s_MissingScripts;
        static readonly List<string> s_Errors = new List<string>();
        static readonly List<string> s_DirtyOnOpen = new List<string>();
        static System.Diagnostics.Stopwatch s_Clock;
        static int s_StartSceneCount;
        static bool s_CompareAfter;
        static string s_CompareResult = "";

        static RushSnapshot()
        {
            if (SessionState.GetString(c_RunKey, "") == "1")
            {
                SessionState.SetString(c_RunKey, "");
                s_State = RunState.Error;
                s_Message = "INTERRUPTED by a domain reload (compile or play mode). Run Begin again.";
            }
        }

        // ---------------------------------------------------------------- menu

        [MenuItem("Tools/Rush Refactor/Snapshot/Capture Baseline")]
        static void MenuBaseline() { Debug.Log("[Rush] " + Begin(c_BaselineFile)); }

        [MenuItem("Tools/Rush Refactor/Snapshot/Capture Current And Compare")]
        static void MenuCompare() { Debug.Log("[Rush] " + BeginCurrentAndCompare()); }

        [MenuItem("Tools/Rush Refactor/Snapshot/Status")]
        static void MenuStatus() { Debug.Log("[Rush] " + Status()); }

        [MenuItem("Tools/Rush Refactor/Snapshot/Self Test")]
        static void MenuSelfTest() { Debug.Log("[Rush] " + SelfTest()); }

        // ---------------------------------------------------------------- public API

        public static string BeginCurrentAndCompare()
        {
            string r = Begin(c_CurrentFile);
            if (s_State == RunState.Running) s_CompareAfter = true;
            return r;
        }

        public static string Begin(string outFileName)
        {
            if (s_State == RunState.Running) return "ALREADY RUNNING: " + Status();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "ABORT: editor is in play mode";
            if (EditorApplication.isCompiling) return "ABORT: editor is compiling";
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return "ABORT: a prefab stage is open, close it first";
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isDirty || string.IsNullOrEmpty(s.path))
                    return "ABORT: scene '" + s.name + "' is unsaved or modified; save or discard it first";
            }

            s_Queue = BuildQueue();
            s_Index = 0;
            s_Lines = 0;
            s_Scenes = 0; s_Prefabs = 0; s_Assets = 0; s_GameObjects = 0; s_Components = 0; s_MissingScripts = 0;
            s_Errors.Clear();
            s_DirtyOnOpen.Clear();
            s_CompareAfter = false;
            s_CompareResult = "";
            s_StartSceneCount = SceneManager.sceneCount;
            s_OutPath = RushCommon.FileIn(outFileName);
            s_Writer = new StreamWriter(s_OutPath, false, new UTF8Encoding(false));
            s_Writer.NewLine = "\n";
            s_Clock = System.Diagnostics.Stopwatch.StartNew();
            s_State = RunState.Running;
            s_Message = "";
            SessionState.SetString(c_RunKey, "1");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return "STARTED: " + s_Queue.Count + " assets queued -> " + s_OutPath;
        }

        public static string Status()
        {
            var sb = new StringBuilder();
            sb.AppendLine("state=" + s_State + (s_Message.Length > 0 ? " (" + s_Message + ")" : ""));
            if (s_State == RunState.Running || s_State == RunState.Done)
            {
                double sec = s_Clock != null ? s_Clock.Elapsed.TotalSeconds : 0;
                sb.AppendLine("progress=" + s_Index + "/" + s_Queue.Count + " elapsed=" + sec.ToString("F0") + "s lines=" + s_Lines);
                sb.AppendLine("scenes=" + s_Scenes + " prefabs=" + s_Prefabs + " assets=" + s_Assets +
                              " gameObjects=" + s_GameObjects + " components=" + s_Components + " missingScripts=" + s_MissingScripts);
                sb.AppendLine("errors=" + s_Errors.Count + " dirtyOnOpen=" + s_DirtyOnOpen.Count);
                if (s_State == RunState.Running) sb.AppendLine("current=" + s_Current);
                foreach (string e in s_Errors.Take(10)) sb.AppendLine("  ERR " + e);
            }
            if (s_CompareResult.Length > 0) sb.AppendLine(s_CompareResult);
            return sb.ToString();
        }

        public static string Cancel()
        {
            if (s_State != RunState.Running) return "nothing running";
            EditorApplication.update -= Tick;
            if (s_Writer != null) { s_Writer.Close(); s_Writer = null; }
            SessionState.SetString(c_RunKey, "");
            s_State = RunState.Error;
            s_Message = "cancelled";
            return "cancelled";
        }

        public static bool IsRunning { get { return s_State == RunState.Running; } }

        // ---------------------------------------------------------------- queue

        static List<string> BuildQueue()
        {
            string[] roots = RushCommon.AssetRoots;
            var q = new List<string>();
            q.AddRange(PathsOf("t:Scene", roots).Select(p => "S|" + p));
            q.AddRange(PathsOf("t:Prefab", roots).Select(p => "P|" + p));
            q.AddRange(PathsOf("t:ScriptableObject", roots).Select(p => "O|" + p));
            return q;
        }

        static List<string> PathsOf(string filter, string[] roots)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (string g in AssetDatabase.FindAssets(filter, roots))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (RushCommon.IsProjectPath(p)) set.Add(p);
            }
            var list = set.ToList();
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        // ---------------------------------------------------------------- run loop

        static void Tick()
        {
            if (s_State != RunState.Running) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            var slice = System.Diagnostics.Stopwatch.StartNew();
            while (s_Index < s_Queue.Count && slice.ElapsedMilliseconds < c_SliceMs)
            {
                string item = s_Queue[s_Index++];
                try { Process(item); }
                catch (Exception e) { s_Errors.Add(item + " :: " + e.GetType().Name + ": " + RushCommon.Clip(e.Message, 200)); }
            }
            if (s_Index >= s_Queue.Count) Finish();
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            if (s_Writer != null) { s_Writer.Flush(); s_Writer.Close(); s_Writer = null; }
            SessionState.SetString(c_RunKey, "");
            s_Clock.Stop();
            if (SceneManager.sceneCount != s_StartSceneCount)
                s_Errors.Add("scene count changed from " + s_StartSceneCount + " to " + SceneManager.sceneCount);
            s_State = RunState.Done;
            s_Current = "";

            var st = new StringBuilder();
            st.AppendLine("file=" + s_OutPath);
            st.AppendLine("queue=" + s_Queue.Count + " scenes=" + s_Scenes + " prefabs=" + s_Prefabs + " assets=" + s_Assets);
            st.AppendLine("gameObjects=" + s_GameObjects + " components=" + s_Components + " missingScripts=" + s_MissingScripts + " lines=" + s_Lines);
            st.AppendLine("elapsedSec=" + s_Clock.Elapsed.TotalSeconds.ToString("F1", RushCommon.Inv));
            st.AppendLine("errors=" + s_Errors.Count);
            foreach (string e in s_Errors) st.AppendLine("  ERR " + e);
            st.AppendLine("dirtyOnOpen=" + s_DirtyOnOpen.Count);
            foreach (string d in s_DirtyOnOpen) st.AppendLine("  DIRTY " + d);
            File.WriteAllText(s_OutPath + ".stats.txt", st.ToString(), new UTF8Encoding(false));

            if (s_CompareAfter)
            {
                s_CompareAfter = false;
                s_CompareResult = CompareToBaseline();
            }
        }

        static void Process(string item)
        {
            char kind = item[0];
            string path = item.Substring(2);
            s_Current = path;
            switch (kind)
            {
                case 'S': DumpScene(path); s_Scenes++; break;
                case 'P': DumpPrefab(path); s_Prefabs++; break;
                case 'O': DumpAsset(path); break;
            }
        }

        // ---------------------------------------------------------------- dumpers

        static void DumpScene(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                Emit("SCENE", path, "(scene)", "(scene)#0", "rootCount", roots.Length.ToString());
                foreach (GameObject root in roots) DumpGameObject("SCENE", path, root, path);
                if (!wasLoaded && scene.isDirty) s_DirtyOnOpen.Add(path);
            }
            finally
            {
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void DumpPrefab(string path)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) { s_Errors.Add("prefab not loadable: " + path); return; }
            DumpGameObject("PREFAB", path, root, path);
        }

        static void DumpAsset(string path)
        {
            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so == null) return;
            Type t = so.GetType();
            if (!RushCommon.IsProjectAssembly(t.Assembly.GetName().Name)) return;
            s_Assets++;
            DumpProps("ASSET", path, "(asset)", t.FullName + "#0", so, true, path);
        }

        static void DumpGameObject(string kind, string asset, GameObject go, string ctx)
        {
            string gp = HierPath(go.transform);
            s_GameObjects++;
            string tag;
            try { tag = go.tag; } catch (Exception) { tag = "?"; }
            Emit(kind, asset, gp, "(GameObject)#0", "active", go.activeSelf ? "1" : "0");
            Emit(kind, asset, gp, "(GameObject)#0", "layer", go.layer.ToString());
            Emit(kind, asset, gp, "(GameObject)#0", "tag", tag);
            Emit(kind, asset, gp, "(GameObject)#0", "static", ((int)GameObjectUtility.GetStaticEditorFlags(go)).ToString());

            var typeCount = new Dictionary<string, int>(StringComparer.Ordinal);
            int missing = 0;
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null)
                {
                    Emit(kind, asset, gp, "(MissingScript)#" + missing, "present", "1");
                    missing++;
                    s_MissingScripts++;
                    continue;
                }
                Type t = c.GetType();
                string tn = t.FullName;
                int idx;
                typeCount.TryGetValue(tn, out idx);
                typeCount[tn] = idx + 1;
                string comp = tn + "#" + idx;
                bool project = RushCommon.IsProjectAssembly(t.Assembly.GetName().Name);
                s_Components++;
                if (!project) Emit(kind, asset, gp, comp, "(exists)", "1");
                DumpProps(kind, asset, gp, comp, c, project, ctx);
            }
            foreach (Transform child in go.transform) DumpGameObject(kind, asset, child.gameObject, ctx);
        }

        static void DumpProps(string kind, string asset, string gp, string comp, UnityEngine.Object target, bool full, string ctx)
        {
            SerializedObject so;
            try { so = new SerializedObject(target); }
            catch (Exception e) { Emit(kind, asset, gp, comp, "(error)", RushCommon.Clip(e.Message, 120)); return; }
            try
            {
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.Next(enter))
                {
                    // Only containers have children worth visiting. Leaves such as object references expose
                    // m_FileID/m_PathID children that change from load to load, which would make the dump unstable.
                    enter = it.propertyType == SerializedPropertyType.Generic ||
                            it.propertyType == SerializedPropertyType.ManagedReference;
                    string path = it.propertyPath;
                    if (s_SkipProps.Contains(path)) { enter = false; continue; }
                    if (!full && path.IndexOf("m_PersistentCalls", StringComparison.Ordinal) < 0) continue;
                    if (it.propertyType == SerializedPropertyType.Generic && it.isArray && it.arraySize > c_BigArray)
                    {
                        Emit(kind, asset, gp, comp, path + ".(big)", it.arraySize.ToString());
                        enter = false;
                        continue;
                    }
                    string v = Value(it, ctx);
                    if (v == null) continue;
                    Emit(kind, asset, gp, comp, path, v);
                }
            }
            finally
            {
                so.Dispose();
            }
        }

        static string Value(SerializedProperty p, string ctx)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Generic: return null;
                case SerializedPropertyType.Integer: return p.longValue.ToString();
                case SerializedPropertyType.Boolean: return p.boolValue ? "1" : "0";
                case SerializedPropertyType.Float: return RushCommon.F(p.doubleValue);
                case SerializedPropertyType.String:
                    return RushCommon.Esc(RushCommon.Clip(p.stringValue, c_MaxString));
                case SerializedPropertyType.Color: return Fs(p.colorValue.r, p.colorValue.g, p.colorValue.b, p.colorValue.a);
                case SerializedPropertyType.ObjectReference: return DescribeRef(p.objectReferenceValue, ctx);
                case SerializedPropertyType.LayerMask: return p.intValue.ToString();
                case SerializedPropertyType.Enum: return p.intValue.ToString();
                case SerializedPropertyType.Vector2: return Fs(p.vector2Value.x, p.vector2Value.y);
                case SerializedPropertyType.Vector3: return Fs(p.vector3Value.x, p.vector3Value.y, p.vector3Value.z);
                case SerializedPropertyType.Vector4: return Fs(p.vector4Value.x, p.vector4Value.y, p.vector4Value.z, p.vector4Value.w);
                case SerializedPropertyType.Rect:
                    return Fs(p.rectValue.x, p.rectValue.y, p.rectValue.width, p.rectValue.height);
                case SerializedPropertyType.ArraySize: return p.intValue.ToString();
                case SerializedPropertyType.Character: return p.intValue.ToString();
                case SerializedPropertyType.AnimationCurve: return Curve(p.animationCurveValue);
                case SerializedPropertyType.Bounds:
                    return Fs(p.boundsValue.center.x, p.boundsValue.center.y, p.boundsValue.center.z,
                              p.boundsValue.extents.x, p.boundsValue.extents.y, p.boundsValue.extents.z);
                case SerializedPropertyType.Quaternion:
                    return Fs(p.quaternionValue.x, p.quaternionValue.y, p.quaternionValue.z, p.quaternionValue.w);
                case SerializedPropertyType.ExposedReference: return DescribeRef(p.exposedReferenceValue, ctx);
                case SerializedPropertyType.FixedBufferSize: return p.fixedBufferSize.ToString();
                case SerializedPropertyType.Vector2Int: return p.vector2IntValue.x + "," + p.vector2IntValue.y;
                case SerializedPropertyType.Vector3Int:
                    return p.vector3IntValue.x + "," + p.vector3IntValue.y + "," + p.vector3IntValue.z;
                case SerializedPropertyType.RectInt:
                    return p.rectIntValue.x + "," + p.rectIntValue.y + "," + p.rectIntValue.width + "," + p.rectIntValue.height;
                case SerializedPropertyType.BoundsInt:
                    return p.boundsIntValue.position.x + "," + p.boundsIntValue.position.y + "," + p.boundsIntValue.position.z + "," +
                           p.boundsIntValue.size.x + "," + p.boundsIntValue.size.y + "," + p.boundsIntValue.size.z;
                case SerializedPropertyType.ManagedReference: return p.managedReferenceFullTypename;
                case SerializedPropertyType.Hash128: return p.hash128Value.ToString();
                default: return "<" + p.propertyType + ">";
            }
        }

        static string Fs(params float[] v)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < v.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(RushCommon.F(v[i]));
            }
            return sb.ToString();
        }

        static string Curve(AnimationCurve c)
        {
            if (c == null) return "null";
            var sb = new StringBuilder();
            foreach (Keyframe k in c.keys)
            {
                sb.Append(Fs(k.time, k.value, k.inTangent, k.outTangent)).Append(';');
            }
            return sb.ToString();
        }

        static Transform TransformOf(UnityEngine.Object o)
        {
            GameObject go = o as GameObject;
            if (go != null) return go.transform;
            Component c = o as Component;
            if (c != null) return c.transform;
            return null;
        }

        static string DescribeRef(UnityEngine.Object o, string ctx)
        {
            if (o == null) return "null";
            string typeName = o.GetType().FullName;
            string apath = AssetDatabase.GetAssetPath(o);
            Transform tr = TransformOf(o);
            if (!string.IsNullOrEmpty(apath))
            {
                if (tr != null && apath == ctx) return "self:" + HierPath(tr) + "#" + typeName;
                string guid;
                long fid;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out guid, out fid))
                    return "asset:" + guid + ":" + fid + ":" + typeName;
                return "asset:" + apath + ":" + typeName;
            }
            if (tr != null) return "scene:" + HierPath(tr) + "#" + typeName;
            return "obj:" + RushCommon.Clip(o.name, 80) + ":" + typeName;
        }

        static string HierPath(Transform t)
        {
            var parts = new List<string>();
            for (Transform c = t; c != null; c = c.parent) parts.Add(c.name + "[" + c.GetSiblingIndex() + "]");
            parts.Reverse();
            return RushCommon.Esc(string.Join("/", parts.ToArray()));
        }

        static void Emit(string kind, string asset, string go, string comp, string prop, string value)
        {
            s_Writer.Write(kind); s_Writer.Write('\t');
            s_Writer.Write(RushCommon.Esc(asset)); s_Writer.Write('\t');
            s_Writer.Write(go); s_Writer.Write('\t');
            s_Writer.Write(RushCommon.Esc(comp)); s_Writer.Write('\t');
            s_Writer.Write(RushCommon.Esc(prop)); s_Writer.Write('\t');
            s_Writer.Write(value ?? "");
            s_Writer.Write('\n');
            s_Lines++;
        }

        // ---------------------------------------------------------------- compare

        public sealed class RenameEntry
        {
            public string Kind;   // Class | Field | Path
            public string Type;   // Field: declaring type (new full name)
            public string Old;
            public string New;
        }

        public sealed class CompareResult
        {
            public List<string> Missing = new List<string>();
            public List<string> Changed = new List<string>();
            public List<string> Added = new List<string>();
            public string Verdict
            {
                get
                {
                    if (Missing.Count > 0 || Changed.Count > 0) return "FAIL";
                    return Added.Count > 0 ? "PASS(with additions)" : "PASS";
                }
            }
        }

        public static string CompareToBaseline()
        {
            string b = RushCommon.FileIn(c_BaselineFile);
            string c = RushCommon.FileIn(c_CurrentFile);
            if (!File.Exists(b)) return "NO BASELINE: capture snapshot_baseline.txt first";
            if (!File.Exists(c)) return "NO CURRENT: capture snapshot_current.txt first";
            return CompareFiles(b, c, true);
        }

        public static string CompareFiles(string baselineFile, string currentFile, bool useRenameMap)
        {
            List<RenameEntry> map = useRenameMap ? LoadRenameMap() : new List<RenameEntry>();
            CompareResult r = CompareLines(File.ReadAllLines(baselineFile), File.ReadAllLines(currentFile), map);

            var sb = new StringBuilder();
            int written = 0;
            foreach (string s in r.Missing) { if (written++ < c_MaxDiffLines) sb.AppendLine("MISSING\t" + s); }
            foreach (string s in r.Changed) { if (written++ < c_MaxDiffLines) sb.AppendLine("CHANGED\t" + s); }
            foreach (string s in r.Added) { if (written++ < c_MaxDiffLines) sb.AppendLine("ADDED\t" + s); }
            File.WriteAllText(RushCommon.FileIn(c_DiffFile), sb.ToString(), new UTF8Encoding(false));

            var res = new StringBuilder();
            res.AppendLine("SNAPSHOT " + r.Verdict + " (missing=" + r.Missing.Count + ", changed=" + r.Changed.Count +
                           ", added=" + r.Added.Count + ", renameRules=" + map.Count + ")");
            foreach (string s in r.Missing.Take(8)) res.AppendLine("  MISSING " + RushCommon.Clip(s, 220));
            foreach (string s in r.Changed.Take(8)) res.AppendLine("  CHANGED " + RushCommon.Clip(s, 220));
            foreach (string s in r.Added.Take(4)) res.AppendLine("  ADDED   " + RushCommon.Clip(s, 220));
            return res.ToString();
        }

        public static CompareResult CompareLines(IList<string> baseline, IList<string> current, List<RenameEntry> map)
        {
            var resolved = ResolveRules(map);
            var cur = ToDict(current, null, null);
            var bas = ToDict(baseline, map, resolved);

            var r = new CompareResult();
            foreach (KeyValuePair<string, string> kv in bas)
            {
                string cv;
                if (!cur.TryGetValue(kv.Key, out cv)) r.Missing.Add(kv.Key + "\t" + kv.Value);
                else if (!string.Equals(cv, kv.Value, StringComparison.Ordinal))
                    r.Changed.Add(kv.Key + "\t" + kv.Value + "  =>  " + cv);
            }
            foreach (KeyValuePair<string, string> kv in cur)
            {
                if (!bas.ContainsKey(kv.Key)) r.Added.Add(kv.Key + "\t" + kv.Value);
            }
            return r;
        }

        sealed class ResolvedRule
        {
            public RenameEntry Entry;
            public Type DeclaringType;
            public bool DeclaringIsUnityObject;
        }

        static List<ResolvedRule> ResolveRules(List<RenameEntry> map)
        {
            var list = new List<ResolvedRule>();
            if (map == null) return list;
            foreach (RenameEntry e in map)
            {
                if (e.Kind != "Field") continue;
                Type t = RushCommon.FindType(e.Type);
                list.Add(new ResolvedRule
                {
                    Entry = e,
                    DeclaringType = t,
                    DeclaringIsUnityObject = t != null && typeof(UnityEngine.Object).IsAssignableFrom(t)
                });
            }
            return list;
        }

        static Dictionary<string, string> ToDict(IList<string> lines, List<RenameEntry> map, List<ResolvedRule> rules)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string line in lines)
            {
                if (line.Length == 0) continue;
                string[] c = line.Split(new[] { '\t' }, 6);
                if (c.Length < 6) continue;
                if (map != null && map.Count > 0) ApplyRenames(c, map, rules);
                string key = c[0] + "\t" + c[1] + "\t" + c[2] + "\t" + c[3] + "\t" + c[4];
                string unique = key;
                int n = 1;
                while (d.ContainsKey(unique)) unique = key + "~" + (n++);
                d[unique] = c[5];
            }
            return d;
        }

        static void ApplyRenames(string[] c, List<RenameEntry> map, List<ResolvedRule> rules)
        {
            // 1. asset path moves
            foreach (RenameEntry e in map)
            {
                if (e.Kind == "Path" && c[1].StartsWith(e.Old, StringComparison.Ordinal))
                    c[1] = e.New + c[1].Substring(e.Old.Length);
            }

            // 2. class renames in the component column and in the value suffix
            int hash = c[3].LastIndexOf('#');
            string compType = hash >= 0 ? c[3].Substring(0, hash) : c[3];
            string compIdx = hash >= 0 ? c[3].Substring(hash) : "";
            foreach (RenameEntry e in map)
            {
                if (e.Kind != "Class") continue;
                if (compType == e.Old) compType = e.New;
                if (c[5].EndsWith(":" + e.Old, StringComparison.Ordinal) || c[5].EndsWith("#" + e.Old, StringComparison.Ordinal))
                    c[5] = c[5].Substring(0, c[5].Length - e.Old.Length) + e.New;
            }
            c[3] = compType + compIdx;

            // 3. field renames on the property path. Every segment is resolved against the real field it belongs to,
            // so a rule for one class never rewrites a same-named field of another class or of Unity itself.
            if (rules.Count == 0) return;
            Type compClr = RushCommon.FindType(compType);
            if (compClr == null) return;
            string[] segs = c[4].Split('.');
            Type cur = compClr;
            for (int i = 0; i < segs.Length && cur != null; i++)
            {
                string seg = segs[i];
                if (seg == "Array" || seg.StartsWith("data[", StringComparison.Ordinal)) continue;
                string actual = seg;
                foreach (ResolvedRule rule in rules)
                {
                    if (rule.DeclaringType != null && rule.Entry.Old == seg && rule.DeclaringType.IsAssignableFrom(cur)) { actual = rule.Entry.New; break; }
                }
                FieldInfo fi = null;
                for (Type x = cur; x != null && x != typeof(object) && fi == null; x = x.BaseType)
                    fi = x.GetField(actual, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (actual != seg) segs[i] = actual;
                if (fi == null) break;
                Type ft = fi.FieldType;
                if (ft.IsArray) ft = ft.GetElementType();
                else if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>)) ft = ft.GetGenericArguments()[0];
                cur = ft;
            }
            c[4] = string.Join(".", segs);
        }

        public static List<RenameEntry> LoadRenameMap()
        {
            var list = new List<RenameEntry>();
            string file = RushCommon.FileIn("rename_map.txt");
            if (File.Exists(file))
            {
                foreach (string raw in File.ReadAllLines(file))
                {
                    string l = raw.Trim();
                    if (l.Length == 0 || l.StartsWith("#", StringComparison.Ordinal)) continue;
                    string[] p = l.Split('|');
                    if (p[0] == "Class" && p.Length >= 3) list.Add(new RenameEntry { Kind = "Class", Old = p[1], New = p[2] });
                    else if (p[0] == "Field" && p.Length >= 4) list.Add(new RenameEntry { Kind = "Field", Type = p[1], Old = p[2], New = p[3] });
                    else if (p[0] == "Path" && p.Length >= 3) list.Add(new RenameEntry { Kind = "Path", Old = p[1], New = p[2] });
                }
            }

            // Every [FormerlySerializedAs("old")] in code is an authorised rename, so it is added automatically.
            const BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (Type t in RushCommon.ProjectTypes())
            {
                FieldInfo[] fields;
                try { fields = t.GetFields(f); }
                catch (Exception) { continue; }
                foreach (FieldInfo fi in fields)
                {
                    foreach (object a in fi.GetCustomAttributes(typeof(FormerlySerializedAsAttribute), false))
                    {
                        var fa = (FormerlySerializedAsAttribute)a;
                        list.Add(new RenameEntry { Kind = "Field", Type = t.FullName, Old = fa.oldName, New = fi.Name });
                    }
                }
            }
            return list;
        }

        // ---------------------------------------------------------------- self test

        public class ProbeBase : ScriptableObject { }
        public class ProbeChild : ProbeBase { }
        public class ProbePlain { }

        public static string SelfTest()
        {
            var sb = new StringBuilder();
            bool ok = true;
            const string t = "\t";
            string a = "PREFAB" + t + "Assets/_dev/A.prefab" + t + "Root[0]" + t + "RushRefactor.RushSnapshot+ProbeChild#0" + t;
            var baseLines = new List<string>
            {
                a + "m_Old" + t + "5",
                a + "m_Keep" + t + "hello",
                a + "m_Ref" + t + "asset:abc:11500000:RushRefactor.RushSnapshot+OldName"
            };
            var none = new List<RenameEntry>();

            ok &= Expect(sb, "identical => PASS", CompareLines(baseLines, baseLines, none).Verdict == "PASS");

            var changed = new List<string>(baseLines);
            changed[1] = a + "m_Keep" + t + "HELLO";
            ok &= Expect(sb, "changed value => FAIL", CompareLines(baseLines, changed, none).Verdict == "FAIL");

            var removed = new List<string>(baseLines);
            removed.RemoveAt(0);
            ok &= Expect(sb, "missing line => FAIL", CompareLines(baseLines, removed, none).Verdict == "FAIL");

            var added = new List<string>(baseLines);
            added.Add(a + "m_Extra" + t + "1");
            ok &= Expect(sb, "added line => PASS(with additions)", CompareLines(baseLines, added, none).Verdict == "PASS(with additions)");

            var renamed = new List<string>
            {
                a + "m_New" + t + "5",
                a + "m_Keep" + t + "hello",
                a + "m_Ref" + t + "asset:abc:11500000:RushRefactor.RushSnapshot+ProbePlain"
            };
            ok &= Expect(sb, "rename not declared => FAIL", CompareLines(baseLines, renamed, none).Verdict == "FAIL");

            var mapField = new List<RenameEntry>
            {
                new RenameEntry { Kind = "Field", Type = "RushRefactor.RushSnapshot+ProbeBase", Old = "m_Old", New = "m_New" },
                new RenameEntry { Kind = "Class", Old = "RushRefactor.RushSnapshot+OldName", New = "RushRefactor.RushSnapshot+ProbePlain" }
            };
            ok &= Expect(sb, "declared field+class rename (base type match) => PASS",
                CompareLines(baseLines, renamed, mapField).Verdict == "PASS");

            var wrongType = new List<RenameEntry>
            {
                new RenameEntry { Kind = "Field", Type = "RushRefactor.RushSnapshot+ProbeBase", Old = "m_Old", New = "m_Wrong" }
            };
            ok &= Expect(sb, "rename to a different name => FAIL", CompareLines(baseLines, renamed, wrongType).Verdict == "FAIL");

            var moved = new List<string>();
            foreach (string l in baseLines) moved.Add(l.Replace("Assets/_dev/A.prefab", "Assets/_dev/Core/A.prefab"));
            var mapPath = new List<RenameEntry> { new RenameEntry { Kind = "Path", Old = "Assets/_dev/A.prefab", New = "Assets/_dev/Core/A.prefab" } };
            ok &= Expect(sb, "declared asset move => PASS", CompareLines(baseLines, moved, mapPath).Verdict == "PASS");
            ok &= Expect(sb, "undeclared asset move => FAIL", CompareLines(baseLines, moved, none).Verdict == "FAIL");

            sb.Insert(0, ok ? "SNAPSHOT SELFTEST PASS\n" : "SNAPSHOT SELFTEST FAIL\n");
            return sb.ToString();
        }

        static bool Expect(StringBuilder sb, string what, bool cond)
        {
            sb.AppendLine((cond ? "  ok   " : "  FAIL ") + what);
            return cond;
        }
    }
}
