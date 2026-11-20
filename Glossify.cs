using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Drawing;
using System.Web.Script.Serialization;

[assembly: TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]

class Glossify {
 static JavaScriptSerializer json = new JavaScriptSerializer();
 static string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OilSkinPatcher");
 static Encoding raw = Encoding.GetEncoding(28591);

 const string DefaultBodyKey = "no_modifiers VK_MULTIPLY";
 const string DefaultBodyLabel = "Numpad *";
 const string DefaultFaceKey = "no_ctrl no_shift alt VK_MULTIPLY";
 const string DefaultFaceLabel = "Alt + Numpad *";

 public class Mark {
  public string tool { get; set; }
  public string ns { get; set; }
  public Dictionary<string, string> files { get; set; }
  public string coverage { get; set; }
 }

 public class Settings {
  public string lastFolder { get; set; }
  public string lastLibrary { get; set; }
  public string backupDir { get; set; }
  public string bodyKey { get; set; }
  public string bodyKeyLabel { get; set; }
  public string faceKey { get; set; }
  public string faceKeyLabel { get; set; }
 }

 class Edit { public string path; public byte[] before, after; }

 class ModBackups {
  public string recorded;
  public List<string> dirs = new List<string>();
 }

 class Plan {
  public string dir, state;
  public List<Edit> edits = new List<Edit>();
  public void Put(string p, byte[] b) {
   p = CleanPath(p);
   if (File.Exists(LP(p)) && Link(p)) throw new Exception("linked file");
   var old = File.Exists(LP(p)) ? File.ReadAllBytes(LP(p)) : null;
   if (old == null || !old.SequenceEqual(b)) edits.Add(new Edit { path = p, before = old, after = b });
  }
 }

 class CleanCheckedListBox : CheckedListBox {
  public CleanCheckedListBox() {
   DoubleBuffered = true;
   DrawMode = DrawMode.OwnerDrawFixed;
   ItemHeight = 22;
   BorderStyle = BorderStyle.FixedSingle;
   HorizontalScrollbar = true;
   CheckOnClick = true;
   IntegralHeight = false;
  }
  protected override void WndProc(ref Message m) {
   if (m.Msg == 0x0201 || m.Msg == 0x0203) { // WM_LBUTTONDOWN, WM_LBUTTONDBLCLK
    int x = (short)(m.LParam.ToInt64() & 0xFFFF);
    int y = (short)((m.LParam.ToInt64() >> 16) & 0xFFFF);
    int idx = IndexFromPoint(new Point(x, y));
    if (idx < 0 || idx >= Items.Count || !GetItemRectangle(idx).Contains(x, y)) {
     if (!Focused) Focus();
     return;
    }
   }
   base.WndProc(ref m);
  }
  protected override void OnDrawItem(DrawItemEventArgs e) {
   if (e.Index < 0 || e.Index >= Items.Count) return;
   string item = (string)Items[e.Index];
   bool patchable = item.StartsWith("Patch");
   bool isChecked = patchable && GetItemChecked(e.Index);
   Color fg = Color.FromArgb(50, 50, 50);
   if (patchable) fg = Color.FromArgb(0, 135, 58);
   else if (item.StartsWith("Up to Date")) fg = Color.FromArgb(115, 115, 115);
   else if (item.StartsWith("Needs Review") || item.StartsWith("Manual")) fg = Color.FromArgb(190, 115, 0);
   else if (item.StartsWith("Broken")) fg = Color.FromArgb(185, 55, 55);
   else if (item.StartsWith("Skipped")) fg = Color.FromArgb(145, 145, 145);

   Color bg = isChecked ? Color.FromArgb(244, 251, 246) : Color.White;
   using (var bb = new SolidBrush(bg)) e.Graphics.FillRectangle(bb, e.Bounds);

   if (patchable) {
    Size glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, isChecked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);
    Point pt = new Point(e.Bounds.X + 5, e.Bounds.Y + (e.Bounds.Height - glyph.Height) / 2);
    CheckBoxRenderer.DrawCheckBox(e.Graphics, pt, isChecked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);
   }
   using (var b = new SolidBrush(fg))
    e.Graphics.DrawString(item, Font, b, new RectangleF(e.Bounds.X + 24, e.Bounds.Y + 3, Math.Max(100, e.Bounds.Width - 28), e.Bounds.Height));
  }
 }

 static string NormalizeInputPath(string p) {
  if (p == null) return "";
  p = p.Trim().Trim('"').Trim();
  if (p.Length > 3) p = p.TrimEnd('\\', '/');
  return p;
 }

 static string CleanPath(string p) {
  if (string.IsNullOrEmpty(p)) return p;
  if (p.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) return @"\\" + p.Substring(8);
  if (p.StartsWith(@"\\?\")) return p.Substring(4);
  return p;
 }

 static string LP(string p) {
  if (string.IsNullOrEmpty(p)) return p;
  if (p.StartsWith(@"\\?\")) return p;
  string full = Path.GetFullPath(p);
  return full.StartsWith(@"\\") ? @"\\?\UNC\" + full.Substring(2) : @"\\?\" + full;
 }

 static string Hash(byte[] b) { using (var s = SHA256.Create()) return BitConverter.ToString(s.ComputeHash(b)).Replace("-", ""); }
 static bool Link(string p) { return (File.GetAttributes(LP(p)) & FileAttributes.ReparsePoint) != 0; }

 static IEnumerable<string> Files(string dir) {
  foreach (var f in Directory.GetFiles(LP(dir))) {
   string cf = CleanPath(f);
   if (!Link(cf) && !Path.GetFileName(cf).StartsWith("DISABLED", StringComparison.OrdinalIgnoreCase)) yield return cf;
  }
  foreach (var d in Directory.GetDirectories(LP(dir))) {
   string cd = CleanPath(d);
   if (!Link(cd) && !Excluded(cd) && !Path.GetFileName(cd).StartsWith("DISABLED", StringComparison.OrdinalIgnoreCase))
    foreach (var f in Files(cd)) yield return f;
  }
 }

 static string Controller(string ns) {
  using (var r = new StreamReader(typeof(Glossify).Assembly.GetManifestResourceStream("controller")))
   return Regex.Replace(r.ReadToEnd(), @"(?im)^namespace = [^\r\n]+", "namespace = " + ns);
 }

 static readonly Dictionary<string, string[]> FaceDrawBuffers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
  {"52f5aa74", new[]{"016a909a","b3f6842f","9dad4eb7","3b7803dd"}}
 };

 static string NativeFaceController(string original, string label, string hash) {
  string nl = original.Contains("\r\n") ? "\r\n" : "\n";
  var hashes = new List<string> { hash };
  string[] extra;
  if (FaceDrawBuffers.TryGetValue(hash, out extra)) hashes.AddRange(extra);
  var blocks = new List<string>();
  foreach (string h in hashes.Distinct(StringComparer.OrdinalIgnoreCase)) {
   blocks.Add(String.Join(nl, new[] {
    "[TextureOverride" + label + "NativeFace" + h + "]",
    "hash = " + h,
    "match_priority = 9000",
    @"if y83 == 1 && ($active || $body_seen_last_frame)",
    "    run = CommandListPrepareStyle",
    @"    run = CommandList\GlossifyFaceEngine\DrawOriginal",
    "endif",
    ""
   }));
  }
  return original.Replace("global $active = 0", "global $active = 0" + nl + "global $body_seen_last_frame = 0")
   .Replace("post $active = 0", "post $body_seen_last_frame = $active" + nl + "post $active = 0")
   + nl + String.Join(nl, blocks);
 }

 static string TogglesIni(string bodyKey, string faceKey) {
  return String.Join("\r\n", new[] {
   "; Generated by Glossify Patcher",
   "; Global keybinds for toggling Body and Face gloss independently.",
   "namespace = GlossifySkinToggles",
   "",
   "[Constants]",
   "global persist $body_enabled = 1",
   "global persist $face_enabled = 1",
   "global $save_request = 0",
   "global $mod_saved = 0",
   "global $mod_style = 2",
   "global $mod_strength = 1",
   "global $prev_saved = 0",
   "global $prev_style = 2",
   "global $prev_strength = 1",
   "",
   "[KeyGlossifyBodyToggle]",
   "key = " + bodyKey,
   "type = cycle",
   "smart = false",
   "$body_enabled = 0,1",
   "run = CommandListPublishToggles",
   "",
   "[KeyGlossifyFaceToggle]",
   "key = " + faceKey,
   "type = cycle",
   "smart = false",
   "$face_enabled = 0,1",
   "run = CommandListPublishToggles",
   "",
   "[CommandListPublishToggles]",
   "x83 = $body_enabled",
   "y83 = $face_enabled",
   "",
   "[Present]",
   "run = CommandListPublishToggles",
   ""
  });
 }

 static string To3DMigotoKey(Keys code, out string label) {
  label = null;
  if (code >= Keys.A && code <= Keys.Z) { label = code.ToString(); return label; }
  if (code >= Keys.D0 && code <= Keys.D9) { label = ((int)(code - Keys.D0)).ToString(); return label; }
  if (code >= Keys.NumPad0 && code <= Keys.NumPad9) { int n = (int)(code - Keys.NumPad0); label = "Numpad " + n; return "VK_NUMPAD" + n; }
  if (code >= Keys.F1 && code <= Keys.F24) { label = code.ToString(); return "VK_" + label; }
  switch (code) {
   case Keys.Multiply: label = "Numpad *"; return "VK_MULTIPLY";
   case Keys.Add: label = "Numpad +"; return "VK_ADD";
   case Keys.Subtract: label = "Numpad -"; return "VK_SUBTRACT";
   case Keys.Divide: label = "Numpad /"; return "VK_DIVIDE";
   case Keys.Decimal: label = "Numpad ."; return "VK_DECIMAL";
   case Keys.Up: label = "Up"; return "VK_UP";
   case Keys.Down: label = "Down"; return "VK_DOWN";
   case Keys.Left: label = "Left"; return "VK_LEFT";
   case Keys.Right: label = "Right"; return "VK_RIGHT";
   case Keys.Insert: label = "Insert"; return "VK_INSERT";
   case Keys.Delete: label = "Delete"; return "VK_DELETE";
   case Keys.Home: label = "Home"; return "VK_HOME";
   case Keys.End: label = "End"; return "VK_END";
   case Keys.PageUp: label = "PageUp"; return "VK_PRIOR";
   case Keys.PageDown: label = "PageDown"; return "VK_NEXT";
   case Keys.Back: label = "Backspace"; return "VK_BACK";
   case Keys.Tab: label = "Tab"; return "VK_TAB";
   case Keys.Return: label = "Enter"; return "VK_RETURN";
   case Keys.Space: label = "Space"; return "VK_SPACE";
   case Keys.Oemtilde: label = "`"; return "VK_OEM_3";
   case Keys.OemMinus: label = "-"; return "VK_OEM_MINUS";
   case Keys.Oemplus: label = "="; return "VK_OEM_PLUS";
   case Keys.OemOpenBrackets: label = "["; return "VK_OEM_4";
   case Keys.OemCloseBrackets: label = "]"; return "VK_OEM_6";
   case Keys.OemPipe: label = "\\"; return "VK_OEM_5";
   case Keys.OemSemicolon: label = ";"; return "VK_OEM_1";
   case Keys.OemQuotes: label = "'"; return "VK_OEM_7";
   case Keys.Oemcomma: label = ","; return "VK_OEM_COMMA";
   case Keys.OemPeriod: label = "."; return "VK_OEM_PERIOD";
   case Keys.OemQuestion: label = "/"; return "VK_OEM_2";
   default: return null;
  }
 }

 static Settings LoadSettings() {
  string cfgPath = Path.Combine(data, "settings.json");
  string oldPref = Path.Combine(data, "folder.txt");
  Settings s = null;
  if (File.Exists(LP(cfgPath))) {
   try { s = json.Deserialize<Settings>(File.ReadAllText(LP(cfgPath))); } catch { s = null; }
  }
  if (s == null) s = new Settings();
  if (string.IsNullOrEmpty(s.lastFolder) && File.Exists(LP(oldPref))) {
   try { s.lastFolder = NormalizeInputPath(File.ReadAllText(LP(oldPref))); } catch { }
  }
  if (string.IsNullOrEmpty(s.backupDir)) s.backupDir = Path.Combine(data, "Backups");
  if (string.IsNullOrEmpty(s.bodyKey)) { s.bodyKey = DefaultBodyKey; s.bodyKeyLabel = DefaultBodyLabel; }
  if (string.IsNullOrEmpty(s.bodyKeyLabel)) s.bodyKeyLabel = s.bodyKey;
  if (string.IsNullOrEmpty(s.faceKey)) { s.faceKey = DefaultFaceKey; s.faceKeyLabel = DefaultFaceLabel; }
  if (string.IsNullOrEmpty(s.faceKeyLabel)) s.faceKeyLabel = s.faceKey;
  return s;
 }

 static void SaveSettings(Settings s) {
  Directory.CreateDirectory(LP(data));
  Write(Path.Combine(data, "settings.json"), Encoding.UTF8.GetBytes(json.Serialize(s)));
  if (!string.IsNullOrEmpty(s.lastFolder)) Write(Path.Combine(data, "folder.txt"), Encoding.UTF8.GetBytes(s.lastFolder));
 }

 static string FindLibrary(string path) {
  path = NormalizeInputPath(path);
  if (string.IsNullOrEmpty(path) || !Directory.Exists(LP(path))) return null;
  DirectoryInfo fallbackMods = null;
  for (var d = new DirectoryInfo(CleanPath(path)); d != null; d = d.Parent) {
   if (d.Name.Equals("Mods", StringComparison.OrdinalIgnoreCase)) {
    if (d.Parent != null && d.Parent.Name.Equals("ZZMI", StringComparison.OrdinalIgnoreCase)) return CleanPath(d.FullName);
    if (fallbackMods == null && Directory.Exists(LP(Path.Combine(d.FullName, "Bangboo Skins")))) fallbackMods = d;
   }
  }
  return fallbackMods != null ? CleanPath(fallbackMods.FullName) : null;
 }

 static string ResolveTogglesPath(string currentRoot, Settings cfg) {
  string lib = FindLibrary(currentRoot);
  if (lib != null && cfg.lastLibrary != lib) { cfg.lastLibrary = lib; SaveSettings(cfg); }
  if (lib == null && !string.IsNullOrEmpty(cfg.lastLibrary) && Directory.Exists(LP(cfg.lastLibrary)))
   lib = CleanPath(cfg.lastLibrary);
  if (lib == null) return null;

  string direct = Path.Combine(lib, "Glossify", "GlossifySkinToggles.ini");
  if (File.Exists(LP(direct))) return direct;
  string legacy = Path.Combine(lib, "GlossifySkinToggles", "GlossifySkinToggles.ini");
  if (File.Exists(LP(legacy))) return legacy;

  string engineCat = null;
  foreach (var d1 in Directory.GetDirectories(LP(lib)).Select(CleanPath)) {
   if (Link(d1) || Path.GetFileName(d1).StartsWith("DISABLED", StringComparison.OrdinalIgnoreCase)) continue;
   string cand1 = Path.Combine(d1, "GlossifySkinToggles.ini");
   if (File.Exists(LP(cand1))) return cand1;
   foreach (var d2 in Directory.GetDirectories(LP(d1)).Select(CleanPath)) {
    if (Link(d2) || Path.GetFileName(d2).StartsWith("DISABLED", StringComparison.OrdinalIgnoreCase)) continue;
    string cand2 = Path.Combine(d2, "GlossifySkinToggles.ini");
    if (File.Exists(LP(cand2))) return cand2;
    if (engineCat == null && File.Exists(LP(Path.Combine(d2, "Matcap_ZZZ.ini")))) engineCat = d1;
   }
  }

  string bangboo = Path.Combine(lib, "Bangboo Skins");
  if (Directory.Exists(LP(bangboo))) return Path.Combine(bangboo, "Glossify", "GlossifySkinToggles.ini");
  if (engineCat != null) return Path.Combine(engineCat, "Glossify", "GlossifySkinToggles.ini");
  return Path.Combine(lib, "Glossify", "GlossifySkinToggles.ini");
 }

 static bool TogglesSynced(string togglesPath, Settings cfg) {
  if (string.IsNullOrEmpty(togglesPath) || !File.Exists(LP(togglesPath))) return false;
  byte[] expected = Encoding.UTF8.GetBytes(TogglesIni(cfg.bodyKey, cfg.faceKey));
  return File.ReadAllBytes(LP(togglesPath)).SequenceEqual(expected);
 }

 static string CleanModBackupName(string modFolder) {
  string rawName = Path.GetFileName(CleanPath(modFolder).TrimEnd('\\', '/'));
  string clean = Regex.Replace(rawName ?? "", "^DISABLED[_ -]*", "", RegexOptions.IgnoreCase).Trim().TrimEnd('.');
  if (string.IsNullOrEmpty(clean)) clean = (rawName ?? "").Trim().TrimEnd('.');
  return string.IsNullOrEmpty(clean) ? "ModBackup" : clean;
 }

 static string AllocateBackupDir(string backupRoot, string modFolder) {
  backupRoot = CleanPath(backupRoot);
  Directory.CreateDirectory(LP(backupRoot));
  string baseName = CleanModBackupName(modFolder);
  string parent = Path.GetDirectoryName(CleanPath(modFolder));
  string cat = string.IsNullOrEmpty(parent) ? "" : Path.GetFileName(parent);

  string candidate = Path.Combine(backupRoot, baseName);
  if (!Directory.Exists(LP(candidate)) && !File.Exists(LP(candidate))) return candidate;

  if (!string.IsNullOrEmpty(cat)) {
   string mapPath = Path.Combine(candidate, "backup-map.json");
   if (File.Exists(LP(mapPath))) {
    try {
     var m = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(LP(mapPath)));
     object fObj;
     if (m != null && m.TryGetValue("folder", out fObj) && fObj is string) {
      string existingParent = Path.GetDirectoryName(CleanPath((string)fObj));
      string existingCat = string.IsNullOrEmpty(existingParent) ? "" : Path.GetFileName(existingParent);
      if (!string.IsNullOrEmpty(existingCat) && !existingCat.Equals(cat, StringComparison.OrdinalIgnoreCase)) {
       baseName = baseName + " (" + cat + ")";
       candidate = Path.Combine(backupRoot, baseName);
       if (!Directory.Exists(LP(candidate)) && !File.Exists(LP(candidate))) return candidate;
      }
     }
    } catch { }
   }
  }

  for (int n = 2; ; n++) {
   candidate = Path.Combine(backupRoot, baseName + " (" + n + ")");
   if (!Directory.Exists(LP(candidate)) && !File.Exists(LP(candidate))) return candidate;
  }
 }

 static void MigrateLegacyBackups(string backupRoot) {
  backupRoot = CleanPath(backupRoot);
  if (string.IsNullOrEmpty(backupRoot) || !Directory.Exists(LP(backupRoot))) return;
  List<string> legacyDirs;
  try {
   legacyDirs = Directory.GetDirectories(LP(backupRoot))
    .Select(CleanPath)
    .Where(d => !Link(d) && Regex.IsMatch(Path.GetFileName(d), @"^(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{32})$") && File.Exists(LP(Path.Combine(d, "backup-map.json"))))
    .OrderBy(d => { try { return Directory.GetCreationTimeUtc(LP(d)); } catch { return DateTime.MaxValue; } })
    .ToList();
  } catch { return; }

  foreach (var oldDir in legacyDirs) {
   try {
    var m = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(LP(Path.Combine(oldDir, "backup-map.json"))));
    object fObj;
    if (m == null || !m.TryGetValue("folder", out fObj) || !(fObj is string) || string.IsNullOrEmpty((string)fObj)) continue;
    string targetDir = AllocateBackupDir(backupRoot, (string)fObj);
    Directory.Move(LP(oldDir), LP(targetDir));
   } catch { }
  }
  PruneAllExtraBackups(backupRoot);
 }

 static string ReadBackupFolder(string backupDir) {
  string map = Path.Combine(backupDir, "backup-map.json");
  if (!File.Exists(LP(map)) || Link(backupDir) || Link(map)) return null;
  try {
   var m = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(LP(map)));
   object fObj;
   if (m != null && m.TryGetValue("folder", out fObj) && fObj is string && !string.IsNullOrEmpty(((string)fObj).Trim()))
    return CleanPath(((string)fObj).Trim());
  } catch { }
  return null;
 }

 static string ModIdentity(string recorded) {
  recorded = CleanPath((recorded ?? "").Trim()).TrimEnd('\\', '/');
  if (string.IsNullOrEmpty(recorded)) return "";
  string name = ModFolderKey(Path.GetFileName(recorded));
  string parent = Path.GetDirectoryName(recorded);
  string cat = string.IsNullOrEmpty(parent) ? "" : ModFolderKey(Path.GetFileName(parent.TrimEnd('\\', '/')));
  return cat.ToLowerInvariant() + "\n" + name.ToLowerInvariant();
 }

 static bool IsNumberedCopy(string backupDir) {
  return Regex.IsMatch(Path.GetFileName(CleanPath(backupDir).TrimEnd('\\', '/')) ?? "", @" \(\d+\)$");
 }

 static List<string> BackupDirs(string backupRoot) {
  backupRoot = CleanPath(backupRoot);
  if (string.IsNullOrEmpty(backupRoot) || !Directory.Exists(LP(backupRoot))) return new List<string>();
  return Directory.GetDirectories(LP(backupRoot)).Select(CleanPath)
   .Where(d => !Link(d) && File.Exists(LP(Path.Combine(d, "backup-map.json"))) && !Link(Path.Combine(d, "backup-map.json")))
   .ToList();
 }

 static List<ModBackups> GroupBackups(string backupRoot) {
  var groups = new Dictionary<string, ModBackups>(StringComparer.Ordinal);
  var order = new List<ModBackups>();
  foreach (string dir in BackupDirs(backupRoot).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)) {
   string recorded = ReadBackupFolder(dir);
   string id = ModIdentity(recorded);
   if (string.IsNullOrEmpty(id)) continue;
   ModBackups g;
   if (!groups.TryGetValue(id, out g)) {
    g = new ModBackups { recorded = recorded };
    groups[id] = g;
    order.Add(g);
   }
   g.dirs.Add(dir);
   if (!IsNumberedCopy(dir)) g.recorded = recorded;
  }
  return order;
 }

 static List<string> BackupsForMod(string backupRoot, string modFolder) {
  string id = ModIdentity(modFolder);
  if (string.IsNullOrEmpty(id)) return new List<string>();
  return BackupDirs(backupRoot).Where(d => ModIdentity(ReadBackupFolder(d)) == id).ToList();
 }

 static string PickOriginalBackup(List<string> dirs) {
  var plain = dirs.Where(d => !IsNumberedCopy(d)).ToList();
  var pool = plain.Count > 0 ? plain : dirs;
  return pool.OrderBy(d => { try { return Directory.GetCreationTimeUtc(LP(d)); } catch { return DateTime.MaxValue; } })
   .ThenBy(d => d, StringComparer.OrdinalIgnoreCase).First();
 }

 static void KeepOneBackup(string backupRoot, List<string> dirs) {
  if (dirs == null || dirs.Count <= 1) return;
  string keep = PickOriginalBackup(dirs);
  string root = CleanPath(Path.GetFullPath(backupRoot)).TrimEnd('\\');
  foreach (string dir in dirs) {
   if (dir.Equals(keep, StringComparison.OrdinalIgnoreCase)) continue;
   try {
    string clean = CleanPath(dir).TrimEnd('\\');
    if (clean.Equals(root, StringComparison.OrdinalIgnoreCase) || Link(dir)) continue;
    if (Directory.Exists(LP(dir))) Directory.Delete(LP(dir), true);
   } catch { }
  }
 }

 static void PruneAllExtraBackups(string backupRoot) {
  foreach (var g in GroupBackups(backupRoot)) KeepOneBackup(backupRoot, g.dirs);
 }

 static bool SyncTogglesIni(string togglesPath, Settings cfg, string backupRoot) {
  byte[] expected = Encoding.UTF8.GetBytes(TogglesIni(cfg.bodyKey, cfg.faceKey));
  if (File.Exists(LP(togglesPath))) {
   byte[] cur = File.ReadAllBytes(LP(togglesPath));
   if (cur.SequenceEqual(expected)) return false;
   string toggleFolder = Path.GetDirectoryName(CleanPath(togglesPath));
   if (BackupsForMod(backupRoot, toggleFolder).Count == 0) {
    string bdir = AllocateBackupDir(backupRoot, toggleFolder);
    Directory.CreateDirectory(LP(bdir));
    File.WriteAllBytes(LP(Path.Combine(bdir, "GlossifySkinToggles.ini")), cur);
    File.WriteAllText(LP(Path.Combine(bdir, "backup-map.json")), json.Serialize(new { folder = toggleFolder, created = new string[0] }));
   }
  }
  Write(togglesPath, expected);
  string hkCache = Path.Combine(Path.GetDirectoryName(CleanPath(togglesPath)), "hotkeys.json");
  if (File.Exists(LP(hkCache))) File.Delete(LP(hkCache));
  return true;
 }

 // Native body-to-face IB pairs, ZZ-Model-Importer-Assets revision e1673e8993.
 static readonly Dictionary<string, string> BodyToFace = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
  {"00172ec3","52f5aa74"},
  {"029c1f5a","8b067f99"},
  {"02d8a2cb","51831437"},
  {"046400d3","ffa703e8"},
  {"0479bb8f","2414f4b9"},
  {"04b53ecd","ddf4efa6"},
  {"12251f42","a2406060"},
  {"1300e048","2414f4b9"},
  {"175a0c0a","27966f80"},
  {"195857d8","f6e96452"},
  {"19a3f02e","1fdaf388"},
  {"1eca2097","1fdaf388"},
  {"22fe3236","1fdaf388"},
  {"26a487ff","37119851"},
  {"2825da1e","9f90cfaa"},
  {"28e05a59","7fbbcf0d"},
  {"2cd6516a","7fbbcf0d"},
  {"3060793b","39944f20"},
  {"30abbad1","df3e3965"},
  {"3371580a","dc7978f3"},
  {"38daef11","48625d6d"},
  {"3afb3865","0e74656e"},
  {"4586e530","d643e19a"},
  {"4816de84","19df8e84"},
  {"4a938c0a","f6ef8f3a"},
  {"5144c409","507384ea"},
  {"52842f31","93b02078"},
  {"53d3f4e5","e30ca87f"},
  {"585da98b","e11baad9"},
  {"5f2741ff","d643e19a"},
  {"619c5c94","9a9780a7"},
  {"634ac589","09d9dca7"},
  {"653fb27c","a0201907"},
  {"6619364f","f1c241b7"},
  {"6749b6e7","6ffdfccb"},
  {"6a57d06b","43c3c5a0"},
  {"773f390c","4fafb136"},
  {"79679a10","b0688334"},
  {"7efbdabd","1a2c8573"},
  {"7f32eeae","40cd4182"},
  {"8899e0fd","dcc7bb78"},
  {"8a512b21","b078ff22"},
  {"8b240678","4d60568b"},
  {"8d6acf4e","1fdaf388"},
  {"8e7f72d5","c28e6303"},
  {"92061e5e","6caaeb53"},
  {"92cb56c9","7fbbcf0d"},
  {"94288cca","9c4f1a9a"},
  {"95de0d39","8b067f99"},
  {"981c1a1e","dbd59d30"},
  {"a5eac582","ed85f33b"},
  {"ac900322","ef86fc9f"},
  {"af39a873","facb2461"},
  {"af63e974","b3f6fcb3"},
  {"b298482d","507384ea"},
  {"b3c6ea5a","1a2c8573"},
  {"b4765894","66b936fc"},
  {"b518e540","ebb6a59b"},
  {"ba4255a5","ef86fc9f"},
  {"bd385763","62de5837"},
  {"be5f4c7d","df3e3965"},
  {"c209c22b","c28e6303"},
  {"c2b4ce3a","9a9780a7"},
  {"cb1a6db9","ebb6a59b"},
  {"cd609d98","39944f20"},
  {"ced49ff8","020f9ac6"},
  {"cf8612e6","b078ff22"},
  {"d39c304d","6986f28e"},
  {"d509bdd4","9a9780a7"},
  {"d942b3a7","6467d6c6"},
  {"d9e49957","48625d6d"},
  {"de303163","321768df"},
  {"e30fae03","f6ef8f3a"},
  {"e3ee72d9","bb315c43"},
  {"e53364dd","93b02078"},
  {"e74620b5","a0c80593"},
  {"ee17c9a2","1a2c8573"},
  {"f478ee4c","0817204c"},
  {"fb76d934","d643e19a"},
  {"feb1c4cd","9a9780a7"},
  {"ff2ec4d6","d1a31f0b"},
  {"9040e2c4","d58b6350"},
  {"ad90e710","d58b6350"},
  {"5cb044e9","dbd59d30"},
  {"0492f64a","dbd59d30"},
  {"168b1df9","dbd59d30"},
  {"1a3644e7","dbd59d30"},
  {"24494b59","c28e6303"},
  {"274269c2","6caaeb53"},
  {"279d7a51","6caaeb53"},
  {"f4b78da0","6caaeb53"},
  {"0a3ba8ac","b3f6fcb3"},
  {"0b4ee1b8","b3f6fcb3"},
  {"f0e67001","b3f6fcb3"},
  {"22fa0cd6","f6ef8f3a"},
  {"fa650e6c","39944f20"},
  {"fb385169","f1c241b7"},
  {"f60602ec","0817204c"},
  {"0fa86052","27966f80"},
  {"e6ff7471","27966f80"},
  {"fe9fc31a","7fbbcf0d"},
  {"1817f3ca","9a9780a7"},
  {"e6afd8d1","9a9780a7"},
  {"02e6cb95","66b936fc"},
  {"0914d3d3","66b936fc"},
  {"0219df6e","8b067f99"},
  {"064cd7d3","8b067f99"},
  {"190a2ff1","d643e19a"},
  {"1fb9079c","48625d6d"},
  {"09d0a7d1","51831437"},
  {"698046e6","f3b6e869"},
  {"5f855404","f3b6e869"},
  {"635709b5","f3b6e869"},
  {"10c77d62","ef3c4506"}
 };
 static readonly HashSet<string> FaceHashes = new HashSet<string>(BodyToFace.Values, StringComparer.OrdinalIgnoreCase);

 static bool FaceSection(string section, string hash) {
  if (Regex.IsMatch(section, "hair|weapon|shadow|outline|wing|mask|bangboo|custom|Blend|Texcoord|Position|VertexLimit|Vertex", RegexOptions.IgnoreCase)) return false;
  if (Regex.IsMatch(section, @"(?:Face|lian|脸)", RegexOptions.IgnoreCase)) return true;
  return Regex.IsMatch(section, @"Head", RegexOptions.IgnoreCase) && !string.IsNullOrEmpty(hash) && FaceHashes.Contains(hash);
 }

 static Plan Inspect(string dir, HashSet<string> broken) {
  dir = CleanPath(dir);
  var plan = new Plan { dir = dir };
  if (IsEngineFolder(dir)) { plan.state = "Skipped - Engine / Add-on"; return plan; }
  string name = Regex.Replace(Path.GetFileName(dir), "^DISABLED[_ -]*", "", RegexOptions.IgnoreCase);
  string parentDir = Path.GetDirectoryName(dir);
  string cat = string.IsNullOrEmpty(parentDir) ? "" : Path.GetFileName(parentDir);
  if (broken.Contains(cat + "|" + name)) { plan.state = "Broken (FlairX)"; return plan; }
  string meta = Path.Combine(dir, "mod.json");
  if (File.Exists(LP(meta))) {
   var m = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(LP(meta)));
   object v;
   if (m != null && m.TryGetValue("modBroken", out v) && Convert.ToBoolean(v)) { plan.state = "Broken (FlairX)"; return plan; }
  }
  string flag = Path.Combine(dir, "oilskin.json"), ctrl = Path.Combine(dir, "OilSkinSwitch.ini");
  Mark old = null;
  if (File.Exists(LP(flag))) {
   old = json.Deserialize<Mark>(File.ReadAllText(LP(flag)));
   if (old == null || old.tool != "OilSkinPatcher/1" || old.files == null || !Regex.IsMatch(old.ns ?? "", @"^OilSkin\w+$"))
    throw new Exception("existing marker needs review");
  }
  string ns = old != null ? old.ns : null;
  if (old == null && File.Exists(LP(ctrl))) {
   string existing = File.ReadAllText(LP(ctrl));
   var nsMatch = Regex.Match(existing, @"(?im)^namespace\s*=\s*(\S+)");
   if (nsMatch.Success && Regex.IsMatch(existing, @"(?im)^(?:\[CommandListBodyStart\]|\[CommandListBodyEnd\]|\[CommandListFaceOverlay\]|\[CommandListFaceDraw\])")) {
    ns = nsMatch.Groups[1].Value;
    old = new Mark { tool = "OilSkinPatcher/1", ns = ns, files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) };
   } else {
    plan.state = "Manual Integration";
    return plan;
   }
  }
  if (ns == null) ns = "OilSkin_" + Hash(Encoding.UTF8.GetBytes(cat + "|" + name)).Substring(0, 16);
  var manifest = new Mark { tool = "OilSkinPatcher/1", ns = ns, files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) };
  bool adopted = old != null && old.files.Count == 0;
  var kinds = new HashSet<string>();
  string controller = Controller(ns);
  var nativeFaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  var blockedFaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  bool graceMesh = false;
  var catalogueFaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  var replacedMeshes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

  foreach (string f in Files(dir).Where(x => x.EndsWith(".ini", StringComparison.OrdinalIgnoreCase) && !x.Equals(ctrl, StringComparison.OrdinalIgnoreCase))) {
   byte[] bytes = File.ReadAllBytes(LP(f));
   var enc = bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254 ? Encoding.Unicode : raw;
   string t = enc.GetString(bytes), rel = f.Substring(dir.Length + 1), prev;
   if (Regex.IsMatch(t, @"(?im)^\[TextureOverride(?:Gracebody(?:IB|A)|0f82a13eBody|_IB_Grace Howard_[^\]\r\n]+)\][ \t]*\r?\n[ \t]*hash\s*=\s*(?:8b240678|0f82a13e)\b")) graceMesh = true;
   var bodySections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   var sectionHash = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
   foreach (Match sectionBlock in Regex.Matches(t, @"(?ims)^\[TextureOverride(?<name>[^\]\r\n]+)\][^\r\n]*\r?\n(?<cmd>.*?)(?=^\[|\z)")) {
    string cmd = sectionBlock.Groups["cmd"].Value;
    var mesh = Regex.Match(cmd, @"(?im)^[ \t]*hash\s*=\s*([a-f0-9]{8})\b");
    if (!mesh.Success) continue;
    string id = mesh.Groups[1].Value, faceId;
    sectionHash["TextureOverride" + sectionBlock.Groups["name"].Value] = id;
    if (BodyToFace.TryGetValue(id, out faceId) && !Regex.IsMatch(cmd, @"(?im)^[ \t]*this\s*=")) {
     catalogueFaces.Add(faceId);
     if (Regex.IsMatch(cmd, @"(?im)^[ \t]*(?:ib|match_first_index|handling)\s*=")) bodySections.Add("TextureOverride" + sectionBlock.Groups["name"].Value);
    }
    if (Regex.IsMatch(cmd, @"(?im)^[ \t]*(?:handling|ib|drawindexed\w*)\s*=")) replacedMeshes.Add(id);
   }
   foreach (Match block in Regex.Matches(t, @"(?ims)^\[TextureOverride[^\]\r\n]*(?:Face|Head|lian)_?IB\][^\r\n]*\r?\n(?<body>.*?)(?=^\[|\z)")) {
    string commands = block.Groups["body"].Value;
    var id = Regex.Match(commands, @"(?im)^[ \t]*hash\s*=\s*([0-9a-f]{8})\b");
    if (!id.Success) continue;
    string hash = id.Groups[1].Value.ToLowerInvariant();
    if (Regex.IsMatch(commands, @"(?im)^[ \t]*(?:ib|handling|draw\w*|this)\s*=|^[ \t]*run\s*=\s*(?!CommandListSkinTexture[ \t]*(?:;[^\r\n]*)?\r?$|CommandList\\ZZMI\\SetTextures[ \t]*\r?$)\S")) blockedFaces.Add(hash);
    else nativeFaces.Add(hash);
   }
   if (Regex.IsMatch(t, @"(?im)^\[TextureOverrideNicoleBody(?:IB|A)\]\s*hash\s*=\s*e53364dd\b")) nativeFaces.Add("93b02078");
   var bodyCalls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   var otherCalls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach (Match sec in Regex.Matches(t, @"(?ims)^\[(?<name>[^\]\r\n]+)\][^\r\n]*\r?\n(?<cmd>.*?)(?=^\[|\z)")) {
    var dest = bodySections.Contains(sec.Groups["name"].Value) ? bodyCalls : otherCalls;
    foreach (Match call in Regex.Matches(sec.Groups["cmd"].Value, @"(?im)^[ \t]*(?:pre\s+|post\s+)?run\s*=\s*(CommandList[^;\\\r\n]+?)[ \t]*(?:;[^\r\n]*)?\r?$")) dest.Add(call.Groups[1].Value.Trim());
   }
   bodyCalls.ExceptWith(otherCalls);
   var graceBodyLists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach (Match block in Regex.Matches(t, @"(?im)^\[TextureOverride_IB_Grace Howard_[^\]\r\n]+\][^\r\n]*\r?\n(?:[^\[\r\n][^\r\n]*\r?\n|\r?\n)*")) {
    if (!Regex.IsMatch(block.Value, @"(?im)^hash\s*=\s*8b240678\b")) continue;
    foreach (Match call in Regex.Matches(block.Value, @"(?im)^[ \t]*run\s*=\s*(CommandList_IB_Grace Howard_[^;\r\n]+)")) graceBodyLists.Add(call.Groups[1].Value.Trim());
   }
   if (old != null && old.files.TryGetValue(rel, out prev) && Hash(bytes) == prev) {
    manifest.files[rel] = prev;
    if (t.Contains(ns + @"\BodyStart")) kinds.Add("Body");
    if (t.Contains(ns + @"\FaceOverlay") || t.Contains(ns + @"\FaceDraw")) kinds.Add("Face");
    continue;
   }
   {
    string check = t;
    bool hasControllerHooks = old != null || Regex.IsMatch(check, @"(?im)^\s*run\s*=\s*CommandList\\OilSkin\w+\\(?:BodyStart|BodyEnd|FaceOverlay|FaceDraw)\b");
    if (hasControllerHooks) {
     string nsPat = old != null ? Regex.Escape(ns) : @"OilSkin\w+";
     check = Regex.Replace(check, @"(?im)^[ \t]*if\b[^\r\n]*\r?\n(?:[ \t]*;[^\r\n]*\r?\n)*[ \t]*run\s*=\s*CommandList\\" + nsPat + @"\\FaceDraw[ \t]*\r?\n[ \t]*endif[ \t]*[\r\n]*", "");
     check = Regex.Replace(check, @"(?im)^\s*(?:run\s*=\s*CommandList\\" + nsPat + @"\\(?:BodyStart|BodyEnd|FaceOverlay|FaceDraw)\b[^\r\n]*|\$\\GlossifyFaceEngine\\(?:count|first|base|auto)\s*=\s*[^\r\n]*|;\s*OilSkin:[^\r\n]*)[\r\n]*", "");
     t = check;
    }
    if (!hasControllerHooks && Regex.IsMatch(check, @"(?im)^\s*run\s*=\s*CommandList\\(?:KatFXMatcap|KatMatcapFX)\\")) { plan.state = "Skipped - Built-in Matcap"; return plan; }
    if (Regex.IsMatch(check, @"(?im)^\s*(?:run\s*=.*(?:OilSkin|KatFXMatcap)|namespace\s*=.*OilSkin)")) throw new Exception("existing or partially edited hooks");
   }
   var textureOnlyFaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach (Match sb in Regex.Matches(t, @"(?ims)^\[(?<name>[^\]\r\n]+)\][^\r\n]*\r?\n(?<cmd>.*?)(?=^\[|\z)")) {
    string sn = sb.Groups["name"].Value;
    string cmd = sb.Groups["cmd"].Value;
    var hid = Regex.Match(cmd, @"(?im)^[ \t]*hash\s*=\s*([0-9a-f]{8})\b");
    if (!FaceSection(sn, hid.Success ? hid.Groups[1].Value : null)) continue;
    if (Regex.IsMatch(cmd, @"(?im)^[ \t]*(?:drawindexed|draw)\s*=")) continue;
    if (Regex.IsMatch(cmd, @"(?im)^[ \t]*run\s*=\s*CommandList(?:\\ZZMI\\SetTextures|SkinTexture)\b")) textureOnlyFaces.Add(sn);
   }
   string section = "", note = "", nl = t.Contains("\r\n") ? "\r\n" : "\n";
   var output = new StringBuilder();
   foreach (var line in Regex.Split(t, @"(?<=\n)")) {
    var h = Regex.Match(line, @"^\s*\[([^\]]+)\]"); if (h.Success) { section = h.Groups[1].Value; note = ""; }
    if (Regex.IsMatch(line, @"^[ \t]*;")) {
     var cmt = Regex.Match(line, @"^[ \t]*;([^\r\n]*)");
     if (cmt.Success) note = cmt.Groups[1].Value;
     output.Append(line);
     continue;
    }
    string part = note;
    if (!string.IsNullOrWhiteSpace(line)) note = "";
    var draw = Regex.Match(line, @"^([ \t]*)drawindexed\s*=\s*(auto|(\d+)\s*,\s*(\d+)\s*,\s*(-?\d+))\s*(?:;[^\r\n]*)?[\r\n]*$", RegexOptions.IgnoreCase);
    var plain = Regex.Match(line, @"^([ \t]*)draw\s*=\s*(\d+)\s*,\s*(-?\d+)\s*(?:;[^\r\n]*)?[\r\n]*$", RegexOptions.IgnoreCase);
    var textures = Regex.Match(line, @"^([ \t]*)run\s*=\s*CommandList(?:\\ZZMI\\SetTextures|SkinTexture)\b", RegexOptions.IgnoreCase);
    bool excluded = Regex.IsMatch(section, "hair|weapon|shadow|outline|wing|mask|bangboo|custom", RegexOptions.IgnoreCase);
    string secHash = null;
    sectionHash.TryGetValue(section, out secHash);
    bool face = FaceSection(section, secHash);
    bool skipPart = Regex.IsMatch(part, "hair|earring|bang|ahoge|horn|accessory", RegexOptions.IgnoreCase);
    bool body = !excluded && (bodySections.Contains(section) || bodyCalls.Contains(section) || graceBodyLists.Contains(section) || Regex.IsMatch(section, @"^(?:TextureOverride|CommandList)(?:\w*(?:Body(?:Upper|Lower)?|NoCoatTorso|Torso|LowerBody|UpperBody|Legs|LeftArm|RightArm|BothArms|shenti)(?:IB|[A-Z]\d*|\d+)?|[^]]*(?:_IB_[a-f0-9]{8}_shenti_Component\d+|[a-f0-9]{8}Part\d+))$", RegexOptions.IgnoreCase));
    if (face && !body && textureOnlyFaces.Contains(section) && textures.Success) {
     output.Append(line.TrimEnd('\r', '\n') + nl);
     output.Append(textures.Groups[1].Value + @"run = CommandList\" + ns + @"\FaceDraw" + nl);
     kinds.Add("Face");
     continue;
    }
    bool faceDraw = face && !body && !skipPart && ((draw.Success && (draw.Groups[2].Value.Equals("auto", StringComparison.OrdinalIgnoreCase) || (draw.Groups[3].Success && draw.Groups[3].Value != "0"))) || (plain.Success && plain.Groups[2].Value != "0"));
    bool bodyDraw = body && (draw.Success || plain.Success);
    if (!faceDraw && !bodyDraw) { output.Append(line); continue; }
    string indent = (draw.Success ? draw.Groups[1].Value : plain.Groups[1].Value), run = indent + @"run = CommandList\" + ns + @"\";
    if (bodyDraw) { output.Append(run + "BodyStart" + nl + line.TrimEnd('\r', '\n') + nl + run + "BodyEnd" + nl); kinds.Add("Body"); }
    else if (draw.Success && draw.Groups[3].Success) {
     output.Append(line.TrimEnd('\r', '\n') + nl);
     output.Append(indent + @"$\GlossifyFaceEngine\auto = 0" + nl);
     int i = 3; foreach (var key in new[] { "count", "first", "base" }) output.Append(indent + @"$\GlossifyFaceEngine\" + key + " = " + draw.Groups[i++].Value + nl);
     output.Append(run + "FaceOverlay" + nl); kinds.Add("Face");
    } else if (draw.Success) {
     output.Append(line.TrimEnd('\r', '\n') + nl);
     output.Append(indent + @"$\GlossifyFaceEngine\count = 0" + nl);
     output.Append(indent + @"$\GlossifyFaceEngine\auto = 1" + nl);
     output.Append(run + "FaceOverlay" + nl); kinds.Add("Face");
    } else {
     output.Append(line.TrimEnd('\r', '\n') + nl);
     output.Append(indent + @"$\GlossifyFaceEngine\count = 0" + nl);
     output.Append(indent + @"$\GlossifyFaceEngine\auto = 0" + nl);
     output.Append(run + "FaceOverlay" + nl); kinds.Add("Face");
    }
   }
   string next = output.ToString();
   if (next != t) { var b = enc.GetBytes(next); plan.Put(f, b); manifest.files[rel] = Hash(b); }
  }

  if (kinds.Count == 0) { plan.state = "Skipped - No Draws"; return plan; }
  if (nativeFaces.Count == 0 && catalogueFaces.Count == 0 && kinds.Contains("Body")) {
   foreach (string f in Files(dir).Where(x => x.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))) {
    byte[] fb = File.ReadAllBytes(LP(f));
    var fe = fb.Length >= 2 && fb[0] == 255 && fb[1] == 254 ? Encoding.Unicode : raw;
    string ft = fe.GetString(fb);
    foreach (Match fm in Regex.Matches(ft, @"(?ims)^\[TextureOverride[^\]\r\n]*(?:Face|Head|lian)_?(?:IB|A)\][^\r\n]*\r?\n(?<cmd>.*?)(?=^\[|\z)")) {
     var fid = Regex.Match(fm.Groups["cmd"].Value, @"(?im)^[ \t]*hash\s*=\s*([0-9a-f]{8})\b");
     if (!fid.Success) continue;
     string fh = fid.Groups[1].Value.ToLowerInvariant();
     if (!replacedMeshes.Contains(fh) && !blockedFaces.Contains(fh)) nativeFaces.Add(fh);
    }
    if (nativeFaces.Count > 0) break;
   }
  }
  if (graceMesh) nativeFaces.Add("4d60568b");
  string extended = null;
  if (catalogueFaces.SetEquals(new[] { "2414f4b9" }) && nativeFaces.SetEquals(new[] { "6cfb2498" }) && !replacedMeshes.Contains("6cfb2498") && !blockedFaces.Contains("6cfb2498")) nativeFaces.Clear();
  if (nativeFaces.Count == 0 && catalogueFaces.Count == 1) nativeFaces.UnionWith(catalogueFaces);
  if (nativeFaces.Count == 1 && !replacedMeshes.Contains(nativeFaces.First()) && !blockedFaces.Contains(nativeFaces.First())) {
   string hash = nativeFaces.First();
   extended = NativeFaceController(controller, hash == "4d60568b" ? "Grace" : "Mesh", hash);
  }
  if (File.Exists(LP(ctrl))) {
   string current = File.ReadAllText(LP(ctrl));
   bool patcherCtrl = Regex.IsMatch(current, @"(?im)^namespace\s*=\s*" + Regex.Escape(ns) + @"\s*$")
    && Regex.IsMatch(current, @"(?im)^\[CommandListBodyStart\]")
    && Regex.IsMatch(current, @"(?im)^\[CommandListBodyEnd\]")
    && Regex.IsMatch(current, @"(?im)^\[Constants\]");
   if (current != controller && current != extended && !patcherCtrl && !adopted)
    throw new Exception("controller changed or face layout needs review");
  }
  if (extended != null && kinds.Contains("Body") && !kinds.Contains("Face")) { controller = extended; kinds.Add("Face"); }
  string faceNl = controller.Contains("\r\n") ? "\r\n" : "\n";
  var faceSets = new StringBuilder();
  foreach (string faceHash in catalogueFaces.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) {
   if (!FaceHashes.Contains(faceHash)) continue;
   faceSets.Append("$\\GlossifyFaceEngine\\f").Append(faceHash.ToLowerInvariant()).Append(" = 1").Append(faceNl);
  }
  if (faceSets.Length > 0)
   controller = Regex.Replace(controller, "z83 = 1(\\r?\\n)run = CommandListCaptureSave", m => "z83 = 1" + m.Groups[1].Value + faceSets + "run = CommandListCaptureSave");
  manifest.coverage = String.Join(" + ", kinds.OrderBy(x => x));
  plan.Put(ctrl, Encoding.UTF8.GetBytes(controller));
  plan.Put(flag, Encoding.UTF8.GetBytes(json.Serialize(manifest)));
  plan.state = (plan.edits.Count == 0 ? "Up to Date" : "Patch") + " - " + manifest.coverage;
  return plan;
 }

 static string ModFolderKey(string name) {
  return Regex.Replace(name ?? "", "^DISABLED[_ -]*", "", RegexOptions.IgnoreCase).Trim();
 }

 static List<string> MatchingModFolders(string parent, string key) {
  var matches = new List<string>();
  if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(key) || !Directory.Exists(LP(parent))) return matches;
  foreach (string child in Directory.GetDirectories(LP(parent)).Select(CleanPath)) {
   if (Link(child)) continue;
   if (ModFolderKey(Path.GetFileName(child)).Equals(key, StringComparison.OrdinalIgnoreCase)) matches.Add(child);
  }
  return matches;
 }

 static string ChooseModFolder(List<string> matches, string recorded) {
  if (matches.Count == 0) throw new Exception("The mod folder from this backup is not there anymore:\n" + recorded);
  if (matches.Count == 1) return matches[0];
  var patched = matches.Where(d => File.Exists(LP(Path.Combine(d, "oilskin.json"))) || File.Exists(LP(Path.Combine(d, "OilSkinSwitch.ini")))).ToList();
  if (patched.Count == 1) return patched[0];
  throw new Exception("More than one folder matches this backup. Leave only one enabled or disabled, then try again:\n" + String.Join("\n", matches));
 }

 static string ResolveLiveModFolder(string recorded) {
  recorded = CleanPath((recorded ?? "").Trim());
  if (string.IsNullOrEmpty(recorded)) throw new Exception("This backup does not record which mod it came from.");
  if (Directory.Exists(LP(recorded))) {
   if (Link(recorded)) throw new Exception("Mod folder cannot be a symbolic link.");
   if (File.Exists(LP(Path.Combine(recorded, "oilskin.json"))) || File.Exists(LP(Path.Combine(recorded, "OilSkinSwitch.ini")))) return recorded;
  }
  string parent = Path.GetDirectoryName(recorded);
  string name = Path.GetFileName(recorded.TrimEnd('\\', '/'));
  string key = ModFolderKey(name);
  var direct = MatchingModFolders(parent, key);
  if (direct.Count > 0) return ChooseModFolder(direct, recorded);
  string grand = string.IsNullOrEmpty(parent) ? "" : Path.GetDirectoryName(parent);
  string parentKey = string.IsNullOrEmpty(parent) ? "" : ModFolderKey(Path.GetFileName(parent.TrimEnd('\\', '/')));
  var nested = new List<string>();
  foreach (string category in MatchingModFolders(grand, parentKey))
   nested.AddRange(MatchingModFolders(category, key));
  return ChooseModFolder(nested, recorded);
 }

 static int RestoreBackupFiles(string mapFile, string backupRoot) {
  mapFile = CleanPath(mapFile);
  if (!File.Exists(LP(mapFile)) || !Path.GetFileName(mapFile).Equals("backup-map.json", StringComparison.OrdinalIgnoreCase))
   throw new Exception("That backup is missing backup-map.json.");
  if (Link(mapFile)) throw new Exception("Backup file cannot be a symbolic link.");
  string backupDir = CleanPath(Path.GetDirectoryName(mapFile));
  if (string.IsNullOrEmpty(backupDir) || Link(backupDir)) throw new Exception("Backup folder cannot be a symbolic link.");
  string backupRootClean = CleanPath(Path.GetFullPath(backupRoot)).TrimEnd('\\');
  if (backupDir.TrimEnd('\\').Equals(backupRootClean, StringComparison.OrdinalIgnoreCase))
   throw new Exception("Choose a mod backup inside the backup path, not the backup path itself.");
  var m = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(LP(mapFile)));
  object fObj;
  if (m == null || !m.TryGetValue("folder", out fObj) || !(fObj is string) || string.IsNullOrEmpty(((string)fObj).Trim()))
   throw new Exception("This backup does not record which mod it came from.");
  string mod = ResolveLiveModFolder((string)fObj);
  string prefix = backupDir.TrimEnd('\\') + "\\";
  int restored = 0;
  foreach (string raw in Directory.GetFiles(LP(backupDir), "*", SearchOption.AllDirectories)) {
   string file = CleanPath(raw);
   if (!file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Link(file)) continue;
   string name = Path.GetFileName(file);
   if (name.Equals("backup-map.json", StringComparison.OrdinalIgnoreCase)
    || name.Equals("oilskin.json", StringComparison.OrdinalIgnoreCase)
    || name.Equals("OilSkinSwitch.ini", StringComparison.OrdinalIgnoreCase)) continue;
   string rel = file.Substring(prefix.Length);
   if (rel.StartsWith("..", StringComparison.Ordinal)) continue;
   Write(Path.Combine(mod, rel), File.ReadAllBytes(LP(file)));
   restored++;
  }
  foreach (string asset in new[] { "oilskin.json", "OilSkinSwitch.ini" }) {
   string assetPath = Path.Combine(mod, asset);
   if (File.Exists(LP(assetPath))) File.Delete(LP(assetPath));
  }
  return restored;
 }

 static int RestoreModBackups(ModBackups g, string backupRoot) {
  var dirs = BackupsForMod(backupRoot, g.recorded);
  if (dirs.Count == 0) dirs = g.dirs.ToList();
  if (dirs.Count == 0) throw new Exception("No backup folder is left for this mod.");
  int restored = RestoreBackupFiles(Path.Combine(PickOriginalBackup(dirs), "backup-map.json"), backupRoot);
  string root = CleanPath(Path.GetFullPath(backupRoot)).TrimEnd('\\');
  var failed = new List<string>();
  foreach (string dir in dirs) {
   try {
    string clean = CleanPath(dir).TrimEnd('\\');
    if (clean.Equals(root, StringComparison.OrdinalIgnoreCase)) throw new Exception("refusing to delete the backup path itself");
    if (Link(dir)) throw new Exception("symbolic link");
    if (Directory.Exists(LP(dir))) Directory.Delete(LP(dir), true);
   } catch (Exception ex) { failed.Add(Path.GetFileName(dir) + ": " + ex.Message); }
  }
  if (failed.Count > 0) throw new Exception("The mod was restored, but some backup folders could not be deleted:\n" + String.Join("\n", failed));
  return restored;
 }

 static string BackupLabel(ModBackups g) {
  string recorded = CleanPath(g.recorded).TrimEnd('\\', '/');
  string name = ModFolderKey(Path.GetFileName(recorded));
  string parent = Path.GetDirectoryName(recorded);
  string cat = string.IsNullOrEmpty(parent) ? "" : ModFolderKey(Path.GetFileName(parent.TrimEnd('\\', '/')));
  return string.IsNullOrEmpty(cat) ? name : cat + " \\ " + name;
 }

 class BackupRow {
  public ModBackups mod;
  public CheckBox box;
  public Label title, path;
 }

 static Panel BackupList(IList<ModBackups> groups, bool checkable, out List<BackupRow> rows) {
  rows = new List<BackupRow>();
  var scroll = new Panel {
   Dock = DockStyle.Fill,
   AutoScroll = true,
   BorderStyle = BorderStyle.FixedSingle,
   BackColor = Color.White
  };
  var sheet = new Panel { Location = new Point(0, 0), BackColor = Color.White };
  scroll.Controls.Add(sheet);
  foreach (var g in groups) {
   var row = new BackupRow { mod = g };
   if (checkable) {
    row.box = new CheckBox {
     Text = BackupLabel(g),
     Checked = true,
     AutoSize = true,
     Font = new Font("Segoe UI", 9.2f),
     Cursor = Cursors.Hand,
     Margin = new Padding(0)
    };
    sheet.Controls.Add(row.box);
   } else {
    row.title = new Label {
     Text = BackupLabel(g),
     AutoSize = true,
     Font = new Font("Segoe UI", 9.2f),
     Margin = new Padding(0)
    };
    sheet.Controls.Add(row.title);
   }
   row.path = new Label {
    Text = CleanPath(g.recorded),
    AutoSize = true,
    ForeColor = Color.FromArgb(90, 90, 90),
    Font = new Font("Segoe UI", 8.5f),
    Margin = new Padding(0)
   };
   sheet.Controls.Add(row.path);
   rows.Add(row);
  }
  var laidOut = rows;
  bool layingOut = false;
  Action reflow = () => {
   if (layingOut) return;
   int width = scroll.ClientSize.Width - 8;
   if (width < 80) return;
   layingOut = true;
   int y = 10;
   foreach (var row in laidOut) {
    var head = (Control)row.box ?? row.title;
    head.MaximumSize = new Size(Math.Max(40, width - 20), 0);
    head.Location = new Point(8, y);
    y += head.Height + 2;
    row.path.MaximumSize = new Size(Math.Max(40, width - 36), 0);
    row.path.Location = new Point(26, y);
    y += row.path.Height + 12;
   }
   sheet.Size = new Size(width, y + 6);
   layingOut = false;
  };
  scroll.Resize += (s, e) => reflow();
  return scroll;
 }

 static Form RestoreDialog(string title) {
  var area = Screen.PrimaryScreen.WorkingArea;
  return new Form {
   Text = title,
   Width = Math.Min(760, Math.Max(520, area.Width - 160)),
   Height = Math.Min(640, Math.Max(420, area.Height - 160)),
   MinimumSize = new Size(460, 320),
   StartPosition = FormStartPosition.CenterParent,
   FormBorderStyle = FormBorderStyle.Sizable,
   MinimizeBox = false,
   MaximizeBox = true,
   Font = new Font("Segoe UI", 9.2f),
   BackColor = Color.White
  };
 }

 static List<ModBackups> AskRestore(IWin32Window owner, List<ModBackups> groups) {
  var chosen = new List<ModBackups>();
  using (var dlg = RestoreDialog("Restore Backups")) {
   var top = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(12, 10, 12, 4) };
   var actions = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
   var selAll = MakeButton("Select All", Color.FromArgb(95, 100, 108), 80);
   selAll.Height = 24;
   selAll.Margin = new Padding(0, 0, 6, 0);
   var selNone = MakeButton("Clear", Color.FromArgb(125, 130, 138), 62);
   selNone.Height = 24;
   actions.Controls.Add(selAll);
   actions.Controls.Add(selNone);
   var hint = new Label { Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(55, 55, 55), Text = "Check the mods to put back. Each mod has one original backup." };
   top.Controls.Add(actions);
   top.Controls.Add(hint);

   var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(12, 8, 12, 12) };
   var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
   var restoreBtn = MakeButton("Restore " + groups.Count + " skin" + (groups.Count == 1 ? "" : "s"), Color.FromArgb(120, 72, 40), 0);
   restoreBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
   restoreBtn.Margin = new Padding(0, 0, 8, 0);
   var cancelBtn = MakeButton("Cancel", Color.FromArgb(105, 110, 118), 80);
   buttons.Controls.Add(restoreBtn);
   buttons.Controls.Add(cancelBtn);
   bottom.Controls.Add(buttons);

   var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 4) };
   List<BackupRow> rows;
   host.Controls.Add(BackupList(groups, true, out rows));

   Action refresh = () => {
    int n = rows.Count(r => r.box.Checked);
    restoreBtn.Text = "Restore " + n + " skin" + (n == 1 ? "" : "s");
    restoreBtn.Enabled = n > 0;
   };
   foreach (var row in rows) row.box.CheckedChanged += (s, e) => refresh();
   selAll.Click += (s, e) => { foreach (var row in rows) row.box.Checked = true; refresh(); };
   selNone.Click += (s, e) => { foreach (var row in rows) row.box.Checked = false; refresh(); };
   restoreBtn.Click += (s, e) => {
    chosen.Clear();
    foreach (var row in rows) if (row.box.Checked) chosen.Add(row.mod);
    if (chosen.Count == 0) return;
    dlg.DialogResult = DialogResult.OK;
   };
   cancelBtn.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; };
   dlg.Controls.Add(host);
   dlg.Controls.Add(bottom);
   dlg.Controls.Add(top);
   dlg.CancelButton = cancelBtn;
   dlg.AcceptButton = restoreBtn;
   if (dlg.ShowDialog(owner) != DialogResult.OK) return null;
  }
  return chosen;
 }

 static bool ConfirmRestore(IWin32Window owner, List<ModBackups> chosen) {
  using (var dlg = RestoreDialog("Restore Backups")) {
   int n = chosen.Count;
   var top = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(16, 12, 16, 4) };
   top.Controls.Add(new Label {
    Dock = DockStyle.Fill,
    ForeColor = Color.FromArgb(40, 40, 40),
    Text = "Restore " + n + " skin" + (n == 1 ? "" : "s") + " to the files from before they were patched?"
   });

   var note = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(16, 4, 16, 0) };
   note.Controls.Add(new Label {
    Dock = DockStyle.Fill,
    ForeColor = Color.FromArgb(90, 90, 90),
    Text = "oilskin.json and OilSkinSwitch.ini are removed from each mod. Every backup for those mods is deleted, including copies named (2) and (3)."
   });

   var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(12, 8, 12, 12) };
   var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
   var okBtn = MakeButton("Restore", Color.FromArgb(120, 72, 40), 96);
   okBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
   okBtn.Margin = new Padding(0, 0, 8, 0);
   var cancelBtn = MakeButton("Cancel", Color.FromArgb(105, 110, 118), 80);
   okBtn.DialogResult = DialogResult.OK;
   cancelBtn.DialogResult = DialogResult.Cancel;
   buttons.Controls.Add(okBtn);
   buttons.Controls.Add(cancelBtn);
   bottom.Controls.Add(buttons);

   var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 4, 16, 4) };
   List<BackupRow> rows;
   host.Controls.Add(BackupList(chosen, false, out rows));

   dlg.Controls.Add(host);
   dlg.Controls.Add(note);
   dlg.Controls.Add(bottom);
   dlg.Controls.Add(top);
   dlg.AcceptButton = okBtn;
   dlg.CancelButton = cancelBtn;
   return dlg.ShowDialog(owner) == DialogResult.OK;
  }
 }

 static void ShowNotice(IWin32Window owner, string title, string text) {
  using (var dlg = RestoreDialog(title)) {
   var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(12, 8, 12, 12) };
   var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
   var okBtn = MakeButton("OK", Color.FromArgb(45, 45, 48), 80);
   okBtn.DialogResult = DialogResult.OK;
   buttons.Controls.Add(okBtn);
   bottom.Controls.Add(buttons);
   var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8) };
   host.Controls.Add(new TextBox {
    Dock = DockStyle.Fill,
    Multiline = true,
    ReadOnly = true,
    ScrollBars = ScrollBars.Both,
    WordWrap = true,
    BorderStyle = BorderStyle.None,
    BackColor = Color.White,
    Font = new Font("Segoe UI", 9.2f),
    Text = text
   });
   dlg.Controls.Add(host);
   dlg.Controls.Add(bottom);
   dlg.AcceptButton = okBtn;
   dlg.CancelButton = okBtn;
   dlg.ShowDialog(owner);
  }
 }

 static void Save(Plan p, string backupRoot) {
  foreach (var e in p.edits) {
   var current = File.Exists(LP(e.path)) ? File.ReadAllBytes(LP(e.path)) : null;
   if (current == null ? e.before != null : e.before == null || !current.SequenceEqual(e.before)) throw new Exception("Files changed since scan");
  }
  if (BackupsForMod(backupRoot, p.dir).Count == 0) {
   string backup = AllocateBackupDir(backupRoot, p.dir);
   Directory.CreateDirectory(LP(backup));
   foreach (var e in p.edits) {
    var current = File.Exists(LP(e.path)) ? File.ReadAllBytes(LP(e.path)) : null;
    if (current != null) {
     string dst = Path.Combine(backup, e.path.Substring(p.dir.Length + 1));
     Directory.CreateDirectory(LP(Path.GetDirectoryName(dst)));
     File.WriteAllBytes(LP(dst), current);
    }
   }
   File.WriteAllText(LP(Path.Combine(backup, "backup-map.json")), json.Serialize(new { folder = p.dir, created = p.edits.Where(e => e.before == null).Select(e => e.path).ToArray() }));
  }
  var done = new Stack<Edit>();
  try {
   foreach (var e in p.edits) { Write(e.path, e.after); done.Push(e); }
  } catch {
   foreach (var e in done) { if (e.before == null) File.Delete(LP(e.path)); else Write(e.path, e.before); }
   throw;
  }
 }

 static void Write(string path, byte[] b) {
  string clean = CleanPath(path);
  string parent = Path.GetDirectoryName(clean);
  if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(LP(parent));
  string lp = LP(clean), tmp = lp + ".~tmp";
  try {
   File.WriteAllBytes(tmp, b);
   if (File.Exists(lp)) File.Replace(tmp, lp, null);
   else File.Move(tmp, lp);
  } finally {
   if (File.Exists(tmp)) File.Delete(tmp);
  }
 }

 static bool Excluded(string path) { return Regex.IsMatch(Path.GetFileName(CleanPath(path)), @"^(UI|Bangboo Skins)$", RegexOptions.IgnoreCase); }
 static bool IsEngineFolder(string d) { return new[] { "Matcap_ZZZ.ini", "GlossifyFaceEngine.ini", "OilSkinFaceEngine.ini", "BaseFaces.ini", "GlossifyBase.ini", "GlossySkinBase.ini", "GlossifySkinToggles.ini", "GlossySkinToggles.ini", "GlossifyToggles.ini", "GlossyToggles.ini" }.Any(n => File.Exists(LP(Path.Combine(CleanPath(d), n)))); }
 static bool SkinMarker(string d) { return new[] { "mod.json", "oilskin.json", "OilSkinSwitch.ini" }.Any(n => File.Exists(LP(Path.Combine(CleanPath(d), n)))); }

 static void Discover(string dir, List<string> roots) {
  dir = CleanPath(dir);
  if (Link(dir) || Excluded(dir) || IsEngineFolder(dir)) return;
  if (SkinMarker(dir) || Directory.GetFiles(LP(dir), "*.ini").Select(CleanPath).Any(f => !Path.GetFileName(f).StartsWith("DISABLED", StringComparison.OrdinalIgnoreCase))) { roots.Add(dir); return; }
  foreach (var child in Directory.GetDirectories(LP(dir)).Select(CleanPath)) Discover(child, roots);
 }

 static List<Plan> Scan(string root) {
  root = NormalizeInputPath(root);
  if (string.IsNullOrEmpty(root) || !Directory.Exists(LP(root))) throw new Exception("Choose a skin or a folder containing skins.");
  var selected = new DirectoryInfo(CleanPath(root));
  var ancestors = new List<DirectoryInfo>();
  for (var d = selected; d != null; d = d.Parent) { ancestors.Add(d); if (Link(d.FullName)) throw new Exception("Cannot scan a symbolic link."); }
  var library = ancestors.FirstOrDefault(d => d.Name.Equals("Mods", StringComparison.OrdinalIgnoreCase) && d.Parent != null && d.Parent.Name.Equals("ZZMI", StringComparison.OrdinalIgnoreCase));
  var broken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  var targets = new List<string>();
  if (library != null) {
   string registry = Path.GetFullPath(Path.Combine(library.FullName, @"..\..\..\Settings\ZZ-BrokenMods.json"));
   if (File.Exists(LP(registry))) {
    var m = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(LP(registry)));
    object modsObj;
    if (m != null && m.TryGetValue("Mods", out modsObj) && modsObj is System.Collections.IEnumerable)
     foreach (var s in (System.Collections.IEnumerable)modsObj) broken.Add((string)s);
   }
   int depth = ancestors.IndexOf(library);
   if (depth == 0) {
    foreach (var cat in Directory.GetDirectories(LP(root)).Select(CleanPath).Where(c => !Link(c) && !Excluded(c)))
     targets.AddRange(Directory.GetDirectories(LP(cat)).Select(CleanPath));
   } else if (depth == 1 && !SkinMarker(root) && !Directory.GetFiles(LP(root), "*.ini").Any()) {
    targets.AddRange(Directory.GetDirectories(LP(root)).Select(CleanPath));
   } else if (depth <= 2) {
    targets.Add(CleanPath(root));
   } else {
    throw new Exception("Select the skin's top-level folder inside its character folder.");
   }
  } else Discover(root, targets);

  var list = new List<Plan>();
  foreach (var d in targets.Where(d => !Link(d) && !Excluded(d) && !IsEngineFolder(d))) {
   try { list.Add(Inspect(d, broken)); }
   catch (PathTooLongException) { list.Add(new Plan { dir = d, state = "Skipped - Path Too Long" }); }
   catch (Exception ex) { list.Add(new Plan { dir = d, state = "Needs Review - " + ex.Message }); }
  }
  return list;
 }

 static Button MakeButton(string text, Color bg, int width = 0) {
  var b = new Button {
   Text = text,
   FlatStyle = FlatStyle.Flat,
   BackColor = bg,
   ForeColor = Color.White,
   Cursor = Cursors.Hand,
   Font = new Font("Segoe UI", 9f),
   Margin = new Padding(0)
  };
  b.FlatAppearance.BorderSize = 0;
  if (width > 0) { b.Width = width; b.Height = 27; }
  else { b.AutoSize = true; b.Padding = new Padding(12, 4, 12, 4); }
  return b;
 }

 [STAThread]
 static void Main(string[] args) {
  Application.EnableVisualStyles();
  Application.SetCompatibleTextRenderingDefault(false);
  Directory.CreateDirectory(LP(data));
  var cfg = LoadSettings();

  string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
  if (Path.GetFileName(root) != "Mods" && !SkinMarker(root) && !Directory.GetFiles(LP(root), "*.ini").Any() && !Directory.GetDirectories(LP(root)).Select(CleanPath).Any(d => SkinMarker(d)))
   root = cfg.lastFolder ?? "";

  var form = new Form {
   Text = "Glossify Patcher v1.0",
   Width = 1080,
   Height = 680,
   MinimumSize = new Size(880, 520),
   StartPosition = FormStartPosition.CenterScreen,
   Font = new Font("Segoe UI", 9.2f),
   BackColor = Color.White
  };

  var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, Padding = new Padding(16, 12, 16, 12) };
  for (int r = 0; r < 6; r++) layout.RowStyles.Add(r == 4 ? new RowStyle(SizeType.Percent, 100f) : new RowStyle(SizeType.AutoSize));
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
  form.Controls.Add(layout);

  // Row 0: Mods Folder bar
  var modRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 0, 0, 6) };
  modRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96f));
  modRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
  modRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  var modLbl = new Label { Text = "Mods Folder:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(45, 45, 48) };
  var pathBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 8, 2) };
  var chooseBtn = MakeButton("Choose Folder", Color.FromArgb(45, 45, 48), 114);
  modRow.Controls.Add(modLbl, 0, 0);
  modRow.Controls.Add(pathBox, 1, 0);
  modRow.Controls.Add(chooseBtn, 2, 0);
  layout.Controls.Add(modRow, 0, 0);

  // Row 1: Backup Path bar
  var bakRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 5, RowCount = 1, Margin = new Padding(0, 0, 0, 8) };
  bakRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96f));
  bakRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
  bakRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  bakRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  bakRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  var bakLbl = new Label { Text = "Backup Path:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(45, 45, 48) };
  var backupBox = new TextBox { Dock = DockStyle.Fill, Text = cfg.backupDir, Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 8, 2) };
  var changeBakBtn = MakeButton("Change...", Color.FromArgb(65, 68, 74), 88);
  changeBakBtn.Margin = new Padding(0, 0, 6, 0);
  var openBakBtn = MakeButton("Open Folder", Color.FromArgb(95, 100, 108), 98);
  var restoreBakBtn = MakeButton("Restore Backups", Color.FromArgb(120, 72, 40), 138);
  restoreBakBtn.Margin = new Padding(6, 0, 0, 0);
  bakRow.Controls.Add(bakLbl, 0, 0);
  bakRow.Controls.Add(backupBox, 1, 0);
  bakRow.Controls.Add(changeBakBtn, 2, 0);
  bakRow.Controls.Add(openBakBtn, 3, 0);
  bakRow.Controls.Add(restoreBakBtn, 4, 0);
  layout.Controls.Add(bakRow, 0, 1);

  // Row 2: Separate Body & Face Toggle Keybinds bar
  var keyCard = new Panel { Dock = DockStyle.Fill, AutoSize = true, BackColor = Color.FromArgb(245, 247, 250), BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(10, 6, 10, 6), Margin = new Padding(0, 0, 0, 8) };
  var keyRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 9, RowCount = 1, BackColor = Color.Transparent };
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155f));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155f));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

  var keyTitle = new Label { Text = "Gloss Toggles:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(35, 38, 42) };
  var bodyKeyLbl = new Label { Text = "Body Gloss:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(12, 0, 5, 0), ForeColor = Color.FromArgb(55, 55, 55) };
  var bodyKeyBox = new TextBox { Text = cfg.bodyKeyLabel, ReadOnly = true, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill, BackColor = Color.White, ForeColor = Color.FromArgb(25, 25, 25), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand, Margin = new Padding(0, 2, 0, 2) };
  var faceKeyLbl = new Label { Text = "Face Gloss:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(14, 0, 5, 0), ForeColor = Color.FromArgb(55, 55, 55) };
  var faceKeyBox = new TextBox { Text = cfg.faceKeyLabel, ReadOnly = true, TextAlign = HorizontalAlignment.Center, Dock = DockStyle.Fill, BackColor = Color.White, ForeColor = Color.FromArgb(25, 25, 25), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand, Margin = new Padding(0, 2, 0, 2) };
  var keyHint = new Label { Text = "(click a box & press shortcut; Body & Face toggle separately)", UseMnemonic = false, Font = new Font("Segoe UI", 8.3f), AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(115, 118, 124), Margin = new Padding(10, 0, 6, 0) };
  var saveKeysBtn = MakeButton("Apply Keybinds", Color.FromArgb(0, 112, 168), 116);
  saveKeysBtn.Margin = new Padding(0, 0, 6, 0);
  var resetKeysBtn = MakeButton("Reset", Color.FromArgb(105, 110, 118), 64);

  keyRow.Controls.Add(keyTitle, 0, 0);
  keyRow.Controls.Add(bodyKeyLbl, 1, 0);
  keyRow.Controls.Add(bodyKeyBox, 2, 0);
  keyRow.Controls.Add(faceKeyLbl, 3, 0);
  keyRow.Controls.Add(faceKeyBox, 4, 0);
  keyRow.Controls.Add(keyHint, 5, 0);
  keyRow.Controls.Add(new Label { AutoSize = true }, 6, 0);
  keyRow.Controls.Add(saveKeysBtn, 7, 0);
  keyRow.Controls.Add(resetKeysBtn, 8, 0);
  keyCard.Controls.Add(keyRow);
  layout.Controls.Add(keyCard, 0, 2);

  // Row 3: Status summary + Select All / Clear
  var statusRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 0, 0, 4) };
  statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
  statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  var status = new Label { AutoSize = true, UseMnemonic = false, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(55, 55, 55), Padding = new Padding(0, 2, 0, 2) };
  var selAllBtn = MakeButton("Select All", Color.FromArgb(95, 100, 108), 80);
  selAllBtn.Height = 24;
  selAllBtn.Margin = new Padding(6, 0, 6, 0);
  selAllBtn.Enabled = false;
  var selNoneBtn = MakeButton("Clear", Color.FromArgb(125, 130, 138), 62);
  selNoneBtn.Height = 24;
  selNoneBtn.Enabled = false;
  statusRow.Controls.Add(status, 0, 0);
  statusRow.Controls.Add(selAllBtn, 1, 0);
  statusRow.Controls.Add(selNoneBtn, 2, 0);
  layout.Controls.Add(statusRow, 0, 3);

  // Row 4: Skin list
  var details = new CleanCheckedListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 9.3f), Margin = new Padding(0, 0, 0, 8) };
  layout.Controls.Add(details, 0, 4);

  // Row 5: Footer with Apply button + Toggles status
  var footerRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
  footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
  footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
  var apply = new Button {
   Text = "Patch 0 skins",
   AutoSize = true,
   Enabled = false,
   Padding = new Padding(18, 6, 18, 6),
   FlatStyle = FlatStyle.Flat,
   BackColor = Color.FromArgb(0, 128, 78),
   ForeColor = Color.White,
   Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
   Cursor = Cursors.Hand,
   Margin = new Padding(0, 0, 14, 0)
  };
  apply.FlatAppearance.BorderSize = 0;
  var toggleInfo = new Label { AutoSize = true, UseMnemonic = false, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(95, 100, 108), Font = new Font("Segoe UI", 8.8f) };
  footerRow.Controls.Add(apply, 0, 0);
  footerRow.Controls.Add(toggleInfo, 1, 0);
  layout.Controls.Add(footerRow, 0, 5);

  List<Plan> plans = null;
  bool updatingChecks = false;

  Func<string> commitBackupPath = () => {
   string bp = NormalizeInputPath(backupBox.Text);
   if (string.IsNullOrEmpty(bp)) bp = Path.Combine(data, "Backups");
   backupBox.Text = bp;
   if (cfg.backupDir != bp) { cfg.backupDir = bp; SaveSettings(cfg); }
   return bp;
  };

  Action updateToggleInfo = () => {
   string tp = ResolveTogglesPath(root, cfg);
   if (tp == null) {
    toggleInfo.Text = "Global Toggles: Select a ZZMI Mods folder to install GlossifySkinToggles.ini";
    toggleInfo.ForeColor = Color.FromArgb(140, 105, 30);
   } else if (TogglesSynced(tp, cfg)) {
    toggleInfo.Text = "Global Toggles: Synced (" + tp + ")";
    toggleInfo.ForeColor = Color.FromArgb(30, 125, 70);
   } else {
    toggleInfo.Text = "Global Toggles: Pending update — click 'Apply Keybinds' or 'Patch' (" + tp + ")";
    toggleInfo.ForeColor = Color.FromArgb(185, 110, 0);
   }
  };

  Action updateApplyButton = () => {
   int cc = 0;
   if (plans != null) {
    for (int i = 0; i < details.Items.Count && i < plans.Count; i++)
     if (details.GetItemChecked(i) && plans[i].state.StartsWith("Patch")) cc++;
   }
   apply.Text = "Patch " + cc + " skin" + (cc == 1 ? "" : "s");
   apply.Enabled = cc > 0;
  };

  Action scan = () => {
   apply.Enabled = false;
   selAllBtn.Enabled = false;
   selNoneBtn.Enabled = false;
   plans = null;
   details.Items.Clear();
   status.Text = "Scanning...";
   form.Refresh();
   try {
    root = NormalizeInputPath(root);
    plans = Scan(root);
    int n = plans.Count(p => p.state.StartsWith("Patch"));
    status.Text = root + "   —   Patch: " + n
     + "  |  Up to Date: " + plans.Count(p => p.state.StartsWith("Up to Date"))
     + "  |  Review: " + plans.Count(p => p.state.StartsWith("Needs Review") || p.state.StartsWith("Manual"))
     + "  |  Broken: " + plans.Count(p => p.state.StartsWith("Broken"))
     + "  |  Skipped: " + plans.Count(p => p.state.StartsWith("Skipped"));
    updatingChecks = true;
    try {
     for (int i = 0; i < plans.Count; i++) {
      details.Items.Add(plans[i].state + "    " + Path.GetFileName(plans[i].dir));
      if (plans[i].state.StartsWith("Patch")) details.SetItemChecked(i, true);
     }
    } finally { updatingChecks = false; }
    selAllBtn.Enabled = n > 0;
    selNoneBtn.Enabled = n > 0;
    updateApplyButton();
    pathBox.Text = root;
    cfg.lastFolder = root;
    SaveSettings(cfg);
   } catch (Exception ex) {
    status.Text = ex.Message;
   }
   updateToggleInfo();
  };

  details.ItemCheck += (ss, ee) => {
   if (plans == null || ee.Index < 0 || ee.Index >= plans.Count) return;
   if (!plans[ee.Index].state.StartsWith("Patch")) {
    if (ee.NewValue != CheckState.Unchecked) ee.NewValue = CheckState.Unchecked;
    return;
   }
   if (updatingChecks) { details.Invalidate(details.GetItemRectangle(ee.Index)); return; }
   int cc = 0;
   for (int i = 0; i < details.Items.Count && i < plans.Count; i++) {
    bool chk = (i == ee.Index) ? ee.NewValue == CheckState.Checked : details.GetItemChecked(i);
    if (chk && plans[i].state.StartsWith("Patch")) cc++;
   }
   apply.Text = "Patch " + cc + " skin" + (cc == 1 ? "" : "s");
   apply.Enabled = cc > 0;
   details.BeginInvoke((Action)(() => details.Invalidate(details.GetItemRectangle(ee.Index))));
  };

  selAllBtn.Click += (s, e) => {
   if (plans == null) return;
   updatingChecks = true;
   try {
    for (int i = 0; i < plans.Count; i++)
     if (plans[i].state.StartsWith("Patch")) details.SetItemChecked(i, true);
   } finally { updatingChecks = false; }
   details.Invalidate();
   updateApplyButton();
  };

  selNoneBtn.Click += (s, e) => {
   if (plans == null) return;
   updatingChecks = true;
   try {
    for (int i = 0; i < plans.Count; i++) details.SetItemChecked(i, false);
   } finally { updatingChecks = false; }
   details.Invalidate();
   updateApplyButton();
  };

  // Keybind interactive capture helper
  Action<TextBox, bool> bindKeyBox = (box, isBody) => {
   bool capturing = false;
   Action endCapture = () => {
    capturing = false;
    box.BackColor = Color.White;
    box.ForeColor = Color.FromArgb(25, 25, 25);
    box.Text = isBody ? cfg.bodyKeyLabel : cfg.faceKeyLabel;
   };
   box.Click += (s, e) => {
    capturing = true;
    box.BackColor = Color.FromArgb(228, 242, 255);
    box.ForeColor = Color.FromArgb(0, 90, 180);
    box.Text = "Press key... (Esc)";
    box.SelectionLength = 0;
   };
   box.Leave += (s, e) => { if (capturing) endCapture(); };
   box.PreviewKeyDown += (s, e) => { if (capturing) e.IsInputKey = true; };
   box.KeyDown += (s, e) => {
    if (!capturing) return;
    e.SuppressKeyPress = true;
    e.Handled = true;
    if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin) return;
    if (e.KeyCode == Keys.Escape && !e.Control && !e.Shift && !e.Alt) {
     endCapture();
     form.ActiveControl = null;
     return;
    }
    string keyLabel;
    string vk = To3DMigotoKey(e.KeyCode, out keyLabel);
    if (vk == null) { box.Text = "Unsupported key (Esc)"; return; }
    string migoto = (!e.Control && !e.Shift && !e.Alt)
     ? ("no_modifiers " + vk)
     : ((e.Control ? "ctrl " : "no_ctrl ") + (e.Shift ? "shift " : "no_shift ") + (e.Alt ? "alt " : "no_alt ") + vk);
    string disp = (e.Control ? "Ctrl + " : "") + (e.Shift ? "Shift + " : "") + (e.Alt ? "Alt + " : "") + keyLabel;
    if (isBody) { cfg.bodyKey = migoto; cfg.bodyKeyLabel = disp; }
    else { cfg.faceKey = migoto; cfg.faceKeyLabel = disp; }
    SaveSettings(cfg);
    endCapture();
    form.ActiveControl = null;
    updateToggleInfo();
   };
  };
  bindKeyBox(bodyKeyBox, true);
  bindKeyBox(faceKeyBox, false);

  resetKeysBtn.Click += (s, e) => {
   cfg.bodyKey = DefaultBodyKey;
   cfg.bodyKeyLabel = DefaultBodyLabel;
   cfg.faceKey = DefaultFaceKey;
   cfg.faceKeyLabel = DefaultFaceLabel;
   bodyKeyBox.Text = cfg.bodyKeyLabel;
   faceKeyBox.Text = cfg.faceKeyLabel;
   SaveSettings(cfg);
   updateToggleInfo();
  };

  saveKeysBtn.Click += (s, e) => {
   if (string.Equals(cfg.bodyKey, cfg.faceKey, StringComparison.OrdinalIgnoreCase)) {
    MessageBox.Show("Body Gloss and Face Gloss are bound to the same shortcut (" + cfg.bodyKeyLabel + ").\nPlease choose two different shortcuts so you can toggle Body and Face separately.", "Duplicate Keybind", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
   }
   try {
    string bak = commitBackupPath();
    if (Directory.Exists(LP(bak)) && Link(bak)) throw new Exception("Backup path cannot be a symbolic link.");
    Directory.CreateDirectory(LP(bak));
    MigrateLegacyBackups(bak);
    string tp = ResolveTogglesPath(root, cfg);
    if (tp == null) {
     MessageBox.Show("Keybind preferences saved.\n\nSelect your ZZMI Mods folder (or any character/skin folder inside it) and click 'Apply Keybinds' again to write GlossifySkinToggles.ini.", "Keybinds Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
     return;
    }
    bool changed = SyncTogglesIni(tp, cfg, bak);
    updateToggleInfo();
    MessageBox.Show((changed ? "Updated global glossy toggle keybinds!" : "Keybinds are already up to date!")
     + "\n\nBody Gloss Toggle:  " + cfg.bodyKeyLabel
     + "\nFace Gloss Toggle:  " + cfg.faceKeyLabel
     + "\n\nFile: " + tp
     + "\n\nPress F10 in Zenless Zone Zero to reload mods.", "Glossify Keybinds", MessageBoxButtons.OK, MessageBoxIcon.Information);
   } catch (Exception ex) {
    MessageBox.Show("Failed to update keybinds:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
   }
  };

  // Backup Path controls
  backupBox.Leave += (s, e) => { commitBackupPath(); };
  backupBox.KeyDown += (s, e) => {
   if (e.KeyCode == Keys.Enter) {
    e.SuppressKeyPress = true;
    string bp = commitBackupPath();
    try {
     Directory.CreateDirectory(LP(bp));
     status.Text = "Backup folder set to: " + bp;
    } catch (Exception ex) {
     status.Text = "Invalid backup path: " + ex.Message;
    }
   }
  };
  changeBakBtn.Click += (s, e) => {
   using (var dlg = new FolderBrowserDialog()) {
    dlg.Description = "Choose where Glossify Patcher stores file backups:";
    string cur = commitBackupPath();
    if (Directory.Exists(LP(cur))) dlg.SelectedPath = cur;
    if (dlg.ShowDialog() == DialogResult.OK) {
     backupBox.Text = dlg.SelectedPath;
     commitBackupPath();
    }
   }
  };
  openBakBtn.Click += (s, e) => {
   try {
    string bp = commitBackupPath();
    Directory.CreateDirectory(LP(bp));
    MigrateLegacyBackups(bp);
    Process.Start("explorer.exe", "\"" + CleanPath(bp) + "\"");
   } catch (Exception ex) {
    MessageBox.Show("Could not open backup folder:\n" + ex.Message, "Backup Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
   }
  };
  restoreBakBtn.Click += (s, e) => {
   try {
    string bp = commitBackupPath();
    Directory.CreateDirectory(LP(bp));
    MigrateLegacyBackups(bp);
    var groups = GroupBackups(bp);
    if (groups.Count == 0) {
     MessageBox.Show("No backups in:\n" + bp, "Restore Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
     return;
    }
    var chosen = AskRestore(form, groups);
    if (chosen == null || chosen.Count == 0) return;
    if (!ConfirmRestore(form, chosen)) return;
    int ok = 0, files = 0;
    var errors = new List<string>();
    foreach (var g in chosen) {
     try { files += RestoreModBackups(g, bp); ok++; }
     catch (Exception ex) { errors.Add(BackupLabel(g) + ": " + ex.Message); }
    }
    string msg = "Restored " + ok + " skin" + (ok == 1 ? "" : "s") + ".\r\nIni files put back: " + files + ".\r\noilskin.json and OilSkinSwitch.ini were removed, and every backup for those mods was deleted."
     + (errors.Count > 0 ? "\r\n\r\n" + String.Join("\r\n", errors) : "")
     + "\r\n\r\nPress F10 in Zenless Zone Zero to reload mods.";
    ShowNotice(form, "Restore Backups", msg);
    if (!string.IsNullOrEmpty(root) && Directory.Exists(LP(root))) scan();
   } catch (Exception ex) {
    MessageBox.Show("Could not restore those backups:\n" + ex.Message, "Restore Backups", MessageBoxButtons.OK, MessageBoxIcon.Error);
   }
  };

  // Mods Folder controls
  chooseBtn.Click += (s, e) => {
   using (var dlg = new FolderBrowserDialog()) {
    if (Directory.Exists(LP(root))) dlg.SelectedPath = CleanPath(root);
    if (dlg.ShowDialog() == DialogResult.OK) { root = dlg.SelectedPath; scan(); }
   }
  };
  pathBox.KeyDown += (s, e) => {
   if (e.KeyCode == Keys.Enter) {
    e.SuppressKeyPress = true;
    string p = NormalizeInputPath(pathBox.Text);
    if (!string.IsNullOrEmpty(p) && Directory.Exists(LP(p))) { root = p; scan(); }
    else status.Text = "Folder not found: " + p;
   }
  };

  apply.Click += (s, e) => {
   if (string.Equals(cfg.bodyKey, cfg.faceKey, StringComparison.OrdinalIgnoreCase)) {
    MessageBox.Show("Body Gloss and Face Gloss are bound to the same shortcut (" + cfg.bodyKeyLabel + ").\nPlease choose two different shortcuts before patching.", "Duplicate Keybind", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
   }
   apply.Enabled = false;
   int count = 0;
   bool togglesUpdated = false;
   string tp = null;
   var errors = new List<string>();
   string bak = commitBackupPath();
   try {
    if (Directory.Exists(LP(bak)) && Link(bak)) throw new Exception("Backup path cannot be a symbolic link.");
    Directory.CreateDirectory(LP(bak));
    MigrateLegacyBackups(bak);
    tp = ResolveTogglesPath(root, cfg);
    if (tp != null) togglesUpdated = SyncTogglesIni(tp, cfg, bak);

    var fresh = Scan(root);
    for (int pi = 0; pi < plans.Count; pi++) {
     if (!details.GetItemChecked(pi) || !plans[pi].state.StartsWith("Patch")) continue;
     var p = plans[pi];
     var q = fresh.FirstOrDefault(x => x.dir == p.dir);
     if (q == null || q.state != p.state || !q.edits.Select(x => x.path + Hash(x.after)).SequenceEqual(p.edits.Select(x => x.path + Hash(x.after)))) {
      errors.Add(Path.GetFileName(p.dir) + ": library changed since scan");
      continue;
     }
     try { Save(q, bak); count++; }
     catch (Exception ex) { errors.Add(Path.GetFileName(p.dir) + ": " + ex.Message); }
    }
   } catch (Exception ex) {
    errors.Add(ex.Message);
   }
   string msg = "Patched " + count + " skin" + (count == 1 ? "" : "s") + "."
    + "\nBody Toggle: " + cfg.bodyKeyLabel + "   |   Face Toggle: " + cfg.faceKeyLabel
    + (tp != null ? "\nGlobal Toggles: " + tp + (togglesUpdated ? " (updated)" : "") : "\nNote: Select your ZZMI Mods folder and click 'Apply Keybinds' to install GlossifySkinToggles.ini.")
    + "\nBackups stored in: " + bak
    + "\n\nTo keep gloss off NPCs, turn stock face (Ctrl+Alt+F8) and stock body (Ctrl+Alt+F9) off, and patch each skin mod on its own.";
   if (errors.Count > 0) msg += "\n\nSkipped:\n" + String.Join("\n", errors);
   MessageBox.Show(msg, "Glossify Patcher", MessageBoxButtons.OK, errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
   scan();
  };

  form.Shown += (s, e) => {
   pathBox.Text = root;
   backupBox.Text = cfg.backupDir;
   MigrateLegacyBackups(cfg.backupDir);
   if (!string.IsNullOrEmpty(root) && Directory.Exists(LP(root))) scan();
   else {
    status.Text = "Choose or paste a skin folder, character folder, or your ZZMI Mods library to scan.";
    updateToggleInfo();
   }
  };
  Application.Run(form);
 }
}
