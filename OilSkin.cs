using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;
using System.Web.Script.Serialization;

class OilSkin {
 static JavaScriptSerializer json=new JavaScriptSerializer();
 static string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OilSkinPatcher");
 static Encoding raw=Encoding.GetEncoding(28591);
 static string Hash(byte[] b){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(b)).Replace("-","");}
 static bool Link(string p){return (File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0;}
 static IEnumerable<string> Files(string dir){
 foreach(var f in Directory.GetFiles(dir)) if(!Link(f)&&!Path.GetFileName(f).StartsWith("DISABLED",StringComparison.OrdinalIgnoreCase))yield return f;
 foreach(var d in Directory.GetDirectories(dir))if(!Link(d)&&!Excluded(d)&&!Path.GetFileName(d).StartsWith("DISABLED",StringComparison.OrdinalIgnoreCase))foreach(var f in Files(d))yield return f;
 }
 public class Mark {public string tool{get;set;} public string ns{get;set;} public Dictionary<string,string> files{get;set;} public string coverage{get;set;} }
 class Edit {public string path;public byte[] before,after;}
 class Plan {public string dir,state;public List<Edit> edits=new List<Edit>();public void Put(string p,byte[] b){if(File.Exists(p)&&Link(p))throw new Exception("linked file");var old=File.Exists(p)?File.ReadAllBytes(p):null;if(old==null||!old.SequenceEqual(b))edits.Add(new Edit{path=p,before=old,after=b});}}
 static string Controller(string ns){using(var r=new StreamReader(typeof(OilSkin).Assembly.GetManifestResourceStream("controller")))return Regex.Replace(r.ReadToEnd(),@"(?im)^namespace = [^\r\n]+","namespace = "+ns);}
 static string GraceController(string original){
 string nl=original.Contains("\r\n")?"\r\n":"\n";
 return original.Replace("global $active = 0","global $active = 0"+nl+"global $body_seen_last_frame = 0")
 .Replace("post $active = 0","post $body_seen_last_frame = $active"+nl+"post $active = 0")
 +nl+String.Join(nl,new[]{"[TextureOverrideGraceNativeFace]","hash = 4d60568b","match_first_index = 0","match_priority = 9000","filter_index = 743219","if $enabled && $face_enabled && ($active || $body_seen_last_frame) && ib == 743219","    run = CommandListPrepareStyle","    run = CommandList\\OilSkinFaceEngine\\DrawOriginal","endif",""});
 }
 static Plan Inspect(string dir,HashSet<string> broken){
 var plan=new Plan{dir=dir};string name=Regex.Replace(Path.GetFileName(dir),"^DISABLED[_ -]*","",RegexOptions.IgnoreCase);
 string cat=Directory.GetParent(dir).Name;
 if(broken.Contains(cat+"|"+name)){plan.state="Broken in FlairX";return plan;}
 string meta=Path.Combine(dir,"mod.json");if(File.Exists(meta)){var m=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(meta));object v;if(m.TryGetValue("modBroken",out v)&&Convert.ToBoolean(v)){plan.state="Broken in FlairX";return plan;}}
 string flag=Path.Combine(dir,"oilskin.json"),ctrl=Path.Combine(dir,"OilSkinSwitch.ini"); Mark old=null;
 if(File.Exists(flag)){old=json.Deserialize<Mark>(File.ReadAllText(flag));if(old==null||old.tool!="OilSkinPatcher/1"||old.files==null||!Regex.IsMatch(old.ns??"",@"^OilSkin_[A-F0-9]{16}$"))throw new Exception("existing marker needs review");}
 if(old==null&&File.Exists(ctrl)){plan.state="Existing integration (unmanaged)";return plan;}
 string ns=old==null?"OilSkin_"+Hash(Encoding.UTF8.GetBytes(cat+"|"+name)).Substring(0,16):old.ns;
 var manifest=new Mark{tool="OilSkinPatcher/1",ns=ns,files=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)};
 var kinds=new HashSet<string>(); string controller=Controller(ns);
 string extended=GraceController(controller);bool graceMesh=false; if(File.Exists(ctrl)&&!File.ReadAllBytes(ctrl).SequenceEqual(Encoding.UTF8.GetBytes(controller))&&!File.ReadAllBytes(ctrl).SequenceEqual(Encoding.UTF8.GetBytes(extended)))throw new Exception("controller edited");
 foreach(string f in Files(dir).Where(x=>x.EndsWith(".ini",StringComparison.OrdinalIgnoreCase)&&!x.Equals(ctrl,StringComparison.OrdinalIgnoreCase))){
 byte[] bytes=File.ReadAllBytes(f);var enc=bytes.Length>=2&&bytes[0]==255&&bytes[1]==254?Encoding.Unicode:raw;
 string t=enc.GetString(bytes),rel=f.Substring(dir.Length+1),prev;
 if(Regex.IsMatch(t,@"(?im)^\[TextureOverride(?:Gracebody(?:IB|A)|0f82a13eBody|_IB_Grace Howard_[^\]\r\n]+)\][ \t]*\r?\n[ \t]*hash\s*=\s*(?:8b240678|0f82a13e)\b"))graceMesh=true;
 var graceBodyLists=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 foreach(Match block in Regex.Matches(t,@"(?im)^\[TextureOverride_IB_Grace Howard_[^\]\r\n]+\][^\r\n]*\r?\n(?:[^\[\r\n][^\r\n]*\r?\n|\r?\n)*")){
 if(!Regex.IsMatch(block.Value,@"(?im)^hash\s*=\s*8b240678\b"))continue;
 foreach(Match call in Regex.Matches(block.Value,@"(?im)^[ \t]*run\s*=\s*(CommandList_IB_Grace Howard_[^;\r\n]+)"))graceBodyLists.Add(call.Groups[1].Value.Trim());
 }
 if(old!=null&&old.files.TryGetValue(rel,out prev)&&Hash(bytes)==prev){manifest.files[rel]=prev;if(t.Contains(ns+@"\BodyStart"))kinds.Add("Body");if(t.Contains(ns+@"\FaceOverlay"))kinds.Add("Face");continue;}
 if(Regex.IsMatch(t,@"(?im)^\s*(?:run\s*=.*(?:OilSkin|KatFXMatcap)|namespace\s*=.*OilSkin)"))throw new Exception("existing or partially edited hooks");
 string section="",nl=t.Contains("\r\n")?"\r\n":"\n";
 var output=new StringBuilder();
 foreach(var line in Regex.Split(t,@"(?<=\n)")){
 var h=Regex.Match(line,@"^\s*\[([^\]]+)\]");if(h.Success)section=h.Groups[1].Value;
 var draw=Regex.Match(line,@"^([ \t]*)drawindexed\s*=\s*(auto|(\d+)\s*,\s*(\d+)\s*,\s*(-?\d+))\s*(?:;[^\r\n]*)?[\r\n]*$",RegexOptions.IgnoreCase);
 bool body=graceBodyLists.Contains(section)||Regex.IsMatch(section,@"^TextureOverride\w*Body(?:IB|[A-Z]\d*|\d+)?$",RegexOptions.IgnoreCase);
 bool face=Regex.IsMatch(section,@"^TextureOverride\w*Face(?:IB|[A-Z]\d*|\d+)?$",RegexOptions.IgnoreCase);
 if(Regex.IsMatch(section,"hair|weapon|shadow|outline|wing|mask|bangboo",RegexOptions.IgnoreCase)||!draw.Success||(!body&&!(face&&draw.Groups[3].Success&&draw.Groups[3].Value!="0"))){output.Append(line);continue;}
 string indent=draw.Groups[1].Value,run=indent+@"run = CommandList\"+ns+@"\";
 if(body){output.Append(run+"BodyStart"+nl+line.TrimEnd('\r','\n')+nl+run+"BodyEnd"+nl);kinds.Add("Body");}
 else{output.Append(line.TrimEnd('\r','\n')+nl);int i=3;foreach(var key in new[]{"count","first","base"})output.Append(indent+@"$\OilSkinFaceEngine\"+key+" = "+draw.Groups[i++].Value+nl);output.Append(run+"FaceOverlay"+nl);kinds.Add("Face");}
 }
 string next=output.ToString();if(next!=t){var b=enc.GetBytes(next);plan.Put(f,b);manifest.files[rel]=Hash(b);}
 }
 if(kinds.Count==0){plan.state="No supported draw layout";return plan;}
 if(graceMesh&&kinds.Contains("Body")&&!kinds.Contains("Face")){controller=extended;kinds.Add("Face");} manifest.coverage=String.Join(" + ",kinds.OrderBy(x=>x));plan.Put(ctrl,Encoding.UTF8.GetBytes(controller));plan.Put(flag,Encoding.UTF8.GetBytes(json.Serialize(manifest)));
 plan.state=(plan.edits.Count==0?"Current":"Ready")+" - "+manifest.coverage;return plan;
 }
 static void Save(Plan p){
 string backup=Path.Combine(data,"Backups",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(backup);
 foreach(var e in p.edits){var current=File.Exists(e.path)?File.ReadAllBytes(e.path):null;if(current==null?e.before!=null:e.before==null||!current.SequenceEqual(e.before))throw new Exception("Files changed since scan");if(current!=null){string dst=Path.Combine(backup,e.path.Substring(p.dir.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dst));File.WriteAllBytes(dst,current);}}
 File.WriteAllText(Path.Combine(backup,"backup-map.json"),json.Serialize(new {folder=p.dir,created=p.edits.Where(e=>e.before==null).Select(e=>e.path).ToArray()}));
 var done=new Stack<Edit>();try{foreach(var e in p.edits){Write(e.path,e.after);done.Push(e);}}catch{foreach(var e in done){if(e.before==null)File.Delete(e.path);else Write(e.path,e.before);}throw;}
 }
 static void Write(string path,byte[] b){string tmp=path+"."+Guid.NewGuid()+".tmp";try{File.WriteAllBytes(tmp,b);if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);}finally{if(File.Exists(tmp))File.Delete(tmp);}}
 static bool Excluded(string path){return Regex.IsMatch(Path.GetFileName(path),@"^(Other Misc|UI|Bangboo Skins)$",RegexOptions.IgnoreCase);}
 static bool SkinMarker(string d){return new[]{"mod.json","oilskin.json","OilSkinSwitch.ini"}.Any(n=>File.Exists(Path.Combine(d,n)));}
 static void Discover(string dir,List<string> roots){
 if(Link(dir)||Excluded(dir)||File.Exists(Path.Combine(dir,"Matcap_ZZZ.ini")))return;
 if(SkinMarker(dir)||Directory.GetFiles(dir,"*.ini").Any(f=>!Path.GetFileName(f).StartsWith("DISABLED",StringComparison.OrdinalIgnoreCase))){roots.Add(dir);return;}
 foreach(var child in Directory.GetDirectories(dir))Discover(child,roots);
 }
 static List<Plan> Scan(string root){
 if(!Directory.Exists(root))throw new Exception("Choose a skin or a folder containing test skins.");
 var selected=new DirectoryInfo(root);
 var ancestors=new List<DirectoryInfo>();for(var d=selected;d!=null;d=d.Parent){ancestors.Add(d);if(Link(d.FullName)||Excluded(d.FullName))throw new Exception("This folder is excluded.");}
 var library=ancestors.FirstOrDefault(d=>d.Name.Equals("Mods",StringComparison.OrdinalIgnoreCase)&&d.Parent!=null&&d.Parent.Name.Equals("ZZMI",StringComparison.OrdinalIgnoreCase));
 var broken=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 var targets=new List<string>();
 if(library!=null){
 string registry=Path.GetFullPath(Path.Combine(library.FullName,@"..\..\..\Settings\ZZ-BrokenMods.json"));
 if(File.Exists(registry)){var m=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(registry));foreach(var s in (System.Collections.IEnumerable)m["Mods"])broken.Add((string)s);}
 int depth=ancestors.IndexOf(library);
 if(depth==0){foreach(var cat in Directory.GetDirectories(root).Where(c=>!Link(c)&&!Excluded(c)))targets.AddRange(Directory.GetDirectories(cat));}
 else if(depth==1&&!SkinMarker(root)&&!Directory.GetFiles(root,"*.ini").Any())targets.AddRange(Directory.GetDirectories(root));
 else if(depth<=2)targets.Add(root);
 else throw new Exception("Select the skin's top-level folder inside its character folder.");
 }else Discover(root,targets);
 var list=new List<Plan>();foreach(var d in targets.Where(d=>!Link(d)&&!Excluded(d))){try{list.Add(Inspect(d,broken));}catch(Exception ex){list.Add(new Plan{dir=d,state="Review - "+ex.Message});}}return list;
 }
 [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Directory.CreateDirectory(data);
 string pref=Path.Combine(data,"folder.txt"),root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');if(Path.GetFileName(root)!="Mods"&&!SkinMarker(root)&&!Directory.GetFiles(root,"*.ini").Any()&&!Directory.GetDirectories(root).Any(d=>SkinMarker(d)))root=File.Exists(pref)?File.ReadAllText(pref):"";
 var form=new Form{Text="OilSkin Patcher",Width=820,Height=510,StartPosition=FormStartPosition.CenterScreen};var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=1,Padding=new Padding(12)};form.Controls.Add(layout);
 var choose=new Button{Text="Choose test or mods folder",AutoSize=true};var status=new Label{AutoSize=true};var details=new ListBox{Dock=DockStyle.Fill,HorizontalScrollbar=true};var apply=new Button{Text="Apply",AutoSize=true,Enabled=false};layout.Controls.Add(choose);layout.Controls.Add(status);layout.Controls.Add(details);layout.Controls.Add(apply);layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
 List<Plan> plans=null;Action scan=()=>{apply.Enabled=false;plans=null;details.Items.Clear();status.Text="Scanning...";form.Refresh();try{plans=Scan(root);int n=plans.Count(p=>p.state.StartsWith("Ready"));status.Text=root+Environment.NewLine+"Ready: "+n+"    Current: "+plans.Count(p=>p.state.StartsWith("Current"));foreach(var p in plans)details.Items.Add(p.state+"    "+Path.GetFileName(p.dir));apply.Text="Apply to "+n+" skins";apply.Enabled=n>0;File.WriteAllText(pref,root);}catch(Exception ex){status.Text=ex.Message;}};
 choose.Click+=(s,e)=>{using(var dlg=new FolderBrowserDialog()){if(dlg.ShowDialog()==DialogResult.OK){root=dlg.SelectedPath;scan();}}};
 apply.Click+=(s,e)=>{apply.Enabled=false;int count=0;try{var fresh=Scan(root);foreach(var p in plans.Where(x=>x.state.StartsWith("Ready"))){var q=fresh.FirstOrDefault(x=>x.dir==p.dir);if(q==null||q.state!=p.state||!q.edits.Select(x=>x.path+Hash(x.after)).SequenceEqual(p.edits.Select(x=>x.path+Hash(x.after))))throw new Exception("Library changed. Scan again.");Save(q);count++;}MessageBox.Show("Applied to "+count+" skins. Use the patched skins with the shared OilSkin engine in ZZMI.");}catch(Exception ex){MessageBox.Show("Completed "+count+". "+ex.Message);}scan();};
 form.Shown+=(s,e)=>{if(Directory.Exists(root))scan();else status.Text="Choose one skin, a test folder, or your full Mods library.";};Application.Run(form);
 }
}
