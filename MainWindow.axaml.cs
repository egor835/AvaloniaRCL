//if you're going to tell me i should use mvvm - fuck off, scum.
//fuck this shizocode
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.VersionLoader;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace RCL;

public partial class MainWindow : Window
{
    //version getter
    public class Ver
    {
        public string name { get; set; }
        public List<string> mods { get; set; }
        public List<string> shaders { get; set; }
        public List<string> resourcepacks { get; set; }
    }
    public class Packfile
    {
        public List<Ver> ver { get; set; }
    }
    //newz (and not only)
    public class Newz
    {
        public string title { get; set; }
        public List<string> news { get; set; }
        public string king { get; set; }
        public string lversion { get; set; }
        public string mcversion { get; set; }
        public string ip { get; set; }
        public string proxy { get; set; }
        public int port { get; set; }
    }
    //fetch config
    public class Json
    {
        public string updateServer { get; set; }
        public string secondServer { get; set; }
    }
    //no appdata lol
    public class Config
    {
        public string username { get; set; }
        public string version { get; set; }
        public string lastip { get; set; }
        public int lastport { get; set; }
        public int ram { get; set; }
        public bool proxy { get; set; }
        public bool faststart { get; set; }
        public bool enableShaders { get; set; }
        public bool notafirsttime { get; set; }
    }
    
    public static class GlobalPaths
    {
        public static string mcpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".reigncraft");
        public static string jsonPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        public static string datapath = Path.Combine(mcpath, "launcher_data");
        public static string configPath = Path.Combine(datapath, "config.json");
        public static string versionsPath = Path.Combine(datapath, "versions.json");
        public static string serverpath = "";
        public static string servmodfolder = Path.Combine(mcpath, "servermods");
        public static string globmodfolder = Path.Combine(mcpath, "mods");
        public static string usermodfolder = Path.Combine(mcpath, "user_mods");
        public static string resourcepacks = Path.Combine(mcpath, "resourcepacks");
        public static string serverpacks = Path.Combine(mcpath, "serverpacks");
        public static string userpacks = Path.Combine(mcpath, "user_resourcepacks");
        public static string shaderpacks = Path.Combine(mcpath, "shaderpacks");
        public static string usershaders = Path.Combine(mcpath, "user_shaderpacks");
        public static string configfolder = Path.Combine(mcpath, "config");
        public static string shaderconfig = Path.Combine(configfolder, "oculus.properties");
    }
    
    public static class Globals
    {
        //Launcher version:
        public static string launcher_version = "1.1.0";
        public static string server_version = "0.0.0";
        public static string update_type = "none";
        public static bool pack_update = false;
        //config
        public static Newz news;
        public static Config config;
        //versions
        public static Packfile packfile;
        public static List<string> dwn_mods = new List<string>();
        public static List<string> dwn_resourcepacks = new List<string>();
        public static List<string> dwn_shaders = new List<string>();
        //parameters
        public static bool isInternetHere = true;
        public static bool isLoading = false;
        public static bool notafirstrun = false;
    }

    private static void ReadConfig()
    {
        if (!File.Exists(GlobalPaths.configPath))
        {
            Globals.config =  new Config
            {
                username = "",
                version = "",
                lastip = "",
                lastport = 0,
                ram = 4096,
                proxy = false,
                enableShaders = true,
                faststart = false,
                notafirsttime = false,
            };
            File.WriteAllText(GlobalPaths.configPath, JsonSerializer.Serialize(Globals.config));
        }
        else
        {
            Globals.config = JsonSerializer.Deserialize<Config>(File.ReadAllText(GlobalPaths.configPath));
        }
    }
    
    private static void WriteConfig()
    {
        File.WriteAllText(GlobalPaths.configPath, JsonSerializer.Serialize(Globals.config));
    }
    
    //start this shit
    public MainWindow()
    {
        FileMgr.createNeededFolders();
        ReadConfig();
        if (!FileMgr.IsMinecraftHere())
        {
            Globals.config.notafirsttime = false;
        }
        
        //init minecraft
        InitializeComponent();
        plzresizeit();
    }
    
    private async void WindowStartup(object? sender, EventArgs eventArgs)
    {
        UpdateText.Text = "Версия " + Globals.launcher_version;
        Process[] pname = Process.GetProcessesByName("RCL");
        if (pname.Length > 1)
        {
            MainLauncherWindow.Hide();
            var msg = MessageBoxManager.GetMessageBoxStandard("Запуск невозможен","Лаунчер уже запущен, закройте все процессы и повторите попытку.");
            await msg.ShowAsync();
            Environment.Exit(0);
        }
        //try to download versions.json
        Json json = JsonSerializer.Deserialize<Json>(File.ReadAllText(GlobalPaths.jsonPath));
        GlobalPaths.serverpath = json.updateServer;
        try
        {
            DownloadFileSync(Path.Combine(GlobalPaths.serverpath, "versions.json"), 
                GlobalPaths.versionsPath);
            try
            {
                File.Copy(GlobalPaths.versionsPath,
                    Path.Combine(GlobalPaths.datapath, "versions.bak"), true);
            }
            catch (Exception e) { }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            GlobalPaths.serverpath = json.secondServer;
            try
            {
                DownloadFileSync(Path.Combine(GlobalPaths.serverpath, "versions.json"), 
                    GlobalPaths.versionsPath);
                File.Copy(GlobalPaths.versionsPath, 
                    Path.Combine(GlobalPaths.datapath, "versions.bak"), true);
            }
            catch (Exception exx)
            {
                if (Globals.config.notafirsttime && File.Exists(Path.Combine(GlobalPaths.datapath, "versions.bak")))
                {
                    var msg = MessageBoxManager.GetMessageBoxStandard("",
                        "Проверьте своё интернет-соединение.");
                    await msg.ShowAsync();
                    File.Copy(Path.Combine(GlobalPaths.datapath, "versions.bak"), 
                        GlobalPaths.versionsPath, true);
                    Globals.isInternetHere = false;
                    
                }
                else
                {
                    MainLauncherWindow.Hide();
                    var msg = MessageBoxManager.GetMessageBoxStandard("",
                        "Проверьте своё интернет-соединение перед первым запуском.");
                    await msg.ShowAsync();
                    Environment.Exit(0);
                }
            }
        }
        //and init this shit
        if (Globals.isInternetHere)
        {
            try
            {
                DownloadFileSync(Path.Combine(GlobalPaths.serverpath, "bg.png"), Path.Combine(GlobalPaths.datapath, "bg.png"));
                DownloadFileSync(Path.Combine(GlobalPaths.serverpath, "servers.dat"), Path.Combine(GlobalPaths.mcpath, "servers.dat"));
                DownloadFileSync(Path.Combine(GlobalPaths.serverpath, "news.json"), Path.Combine(GlobalPaths.datapath, "news.json"));
            }
            catch
            {
                MainLauncherWindow.Hide();
                var msg = MessageBoxManager.GetMessageBoxStandard("Не удалось получить доступ к данным лаунчера",
                    "Судя по всему, лаунчер уже запущен. Закройте все предыдущие процессы и повторите попытку.");
                await msg.ShowAsync();
                Environment.Exit(0);
                //MainLauncherWindow.Show();
            }   
        }
        string newsCont = File.ReadAllText(Path.Combine(GlobalPaths.datapath, "news.json"));
        Globals.news = JsonSerializer.Deserialize<Newz>(newsCont);
        NickBox.Text = Globals.config.username;
        
        //bg and get versions
        if (Globals.isInternetHere)
        {
            bgImage.Source = new Avalonia.Media.Imaging.Bitmap(Path.Combine(GlobalPaths.datapath, "bg.png"));
            //check launcher version
            var launcherversion = "";
            using (HttpClient client = new HttpClient())
            { launcherversion = await client.GetStringAsync(Path.Combine(GlobalPaths.serverpath, "launcher_version")); }
            Globals.server_version = launcherversion;
            string[] serverarray = launcherversion.Split('.');
            string[] launcherarray = Globals.launcher_version.Split('.');
            if (Convert.ToInt32(serverarray[2]) > Convert.ToInt32(launcherarray[2]))
            { Globals.update_type = "minor"; }
            if ((Convert.ToInt32(serverarray[1]) > Convert.ToInt32(launcherarray[1])) || (Convert.ToInt32(serverarray[0]) > Convert.ToInt32(launcherarray[0])))
            { Globals.update_type = "major"; }
        }
        hide("launcher_data");
        await listVersions();
        
    }

    private async Task listVersions()
    {
        // Clear list
        VersionBox.Items.Clear();

        //init config
        Globals.packfile = JsonSerializer.Deserialize<Packfile>(File.ReadAllText(GlobalPaths.versionsPath));

        IEnumerable<string> all_mods = new List<string>();
        IEnumerable<string> all_shaders = new List<string>();
        IEnumerable<string> all_resourcepacks = new List<string>();
        // List all versions and get list of all mods and shit
        foreach (var version in Globals.packfile.ver)
        {
            VersionBox.Items.Add(version.name);
            Ver? fullVersion = Globals.packfile.ver.FirstOrDefault(v => v.name == version.name);
            all_mods = all_mods.Union(fullVersion.mods);
            all_shaders = all_shaders.Union(fullVersion.shaders);
            all_resourcepacks = all_resourcepacks.Union(fullVersion.resourcepacks);
        }

        FileMgr fileMgr = new FileMgr();
        await fileMgr.removeAllBut(GlobalPaths.servmodfolder, all_mods);
        await fileMgr.removeAllBut(GlobalPaths.serverpacks, all_resourcepacks);
        await fileMgr.removeAllBut(GlobalPaths.shaderpacks, all_shaders);

        if (!(VersionBox.Items.Contains(Globals.config.version)))
        {
            VersionBox.SelectedItem = VersionBox.Items[0].ToString();
        }
        else
        {
            VersionBox.SelectedItem = Globals.config.version;
        }
        
        //news
        if (Globals.isInternetHere)
        {
            NewsLabel.Text = Globals.news.title;
            NewsRTB.Text = "";
            foreach (var neww in Globals.news.news)
            {
                NewsRTB.Text += ("• " + neww + (Environment.NewLine + Environment.NewLine));
            }
        }
        if (Globals.update_type != "none")
        {
            UpdateText.Text = "Доступно обновление лаунчера!";
            UpdateText.Foreground = Avalonia.Media.Brushes.Gold;
            updateBtn.IsVisible = true;
        }
        if (Globals.update_type == "major")
        {
            updatelauncher();
        }
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        this.IsEnabled = false;
        var usrname = NickBox.Text.Trim();
        //check username
        if (string.IsNullOrEmpty(usrname))
        {
            var msg = MessageBoxManager.GetMessageBoxStandard("","Введите никнейм");
            await msg.ShowAsync();
            this.IsEnabled = true;
        }
        else if (usrname.Replace("_", "").Any(ch => !char.IsLetterOrDigit(ch)) || Regex.IsMatch(usrname.Replace("_", ""), @"\p{IsCyrillic}"))
        {
            var msg = MessageBoxManager.GetMessageBoxStandard("",
                "Ваш никнейм не должен содержать:\n\n- пробелов\n- кириллицы\n- спецсимволов\n\nДопустимы только латинские буквы и цифры.");
            await msg.ShowAsync();
            this.IsEnabled = true;
        }
        else if (usrname.Length > 16)
        {
            var msg = MessageBoxManager.GetMessageBoxStandard("","Ваш никнейм слишком длинный!"); 
            await msg.ShowAsync();
            this.IsEnabled = true;
        }
        else if (!Globals.isInternetHere && Globals.pack_update)
        {
            var msg = MessageBoxManager.GetMessageBoxStandard("","Подключитесь к интернету, чтобы использовать эту сборку"); 
            await msg.ShowAsync();
            this.IsEnabled = true;
        }
        else
        {
            try
            {
                FileMgr fileMgr = new FileMgr();
                StartButton.Content = "Установка";
                Globals.config.username = NickBox.Text.Trim();
                PackUpdateBox.IsVisible = false;
                InstallProgressHeader.IsVisible = true;
                InstallProgress.IsVisible = true;
                Globals.config.version = VersionBox.SelectedItem.ToString();
                Ver? fullVersion = Globals.packfile.ver.FirstOrDefault(v => v.name == VersionBox.SelectedItem);

                //download minecraft
                var process = new Process();
                if (!Globals.config.notafirsttime || !FileMgr.IsMinecraftHere())
                {
                    try
                    {
                        process = await InstallAndBuildOnline(Globals.config.username);
                    }
                    catch (Exception ex)
                    {
                        var msg = MessageBoxManager.GetMessageBoxStandard("",
                            "Похоже на то, что установка Minecraft завершилась с ошибкой.\nВероятно, соединение прервано или нет доступа к серверам Microsoft.\nНажмите ОК, чтобы попытаться скачать Minecraft с сервера ReignCraft.\n\nКод ошибки: " 
                            + ex.ToString());
                        await msg.ShowAsync();
                        process = await InstallAndBuildOffline(Globals.config.username);
                    }
                }
                else
                {
                    process = await InstallAndBuildOffline(Globals.config.username);
                }

                //silly mod updater
                if (Globals.isInternetHere && Globals.pack_update)
                {
                    await fileMgr.DownloadEveryFile(Path.Combine(GlobalPaths.serverpath, "mods"), GlobalPaths.servmodfolder, Globals.dwn_mods, InstallProgress, InstallProgressHeader);
                    await fileMgr.DownloadEveryFile(Path.Combine(GlobalPaths.serverpath, "resourcepacks"), GlobalPaths.serverpacks, Globals.dwn_resourcepacks, InstallProgress, InstallProgressHeader);
                    await fileMgr.DownloadEveryFile(Path.Combine(GlobalPaths.serverpath, "shaders"), GlobalPaths.shaderpacks, Globals.dwn_shaders, InstallProgress, InstallProgressHeader);
                    await fileMgr.DownloadAndUnpack(Path.Combine(GlobalPaths.serverpath, "configs.zip"), GlobalPaths.configfolder);
                    //README
                    DownloadFileSync(Path.Combine(GlobalPaths.serverpath, "README.TXT"), Path.Combine(GlobalPaths.mcpath, "README.TXT"));
                }

                //MODPACK CHANGER (plz hewp me)
                try { Array.ForEach(Directory.GetFiles(GlobalPaths.globmodfolder), File.Delete); }
                catch { Directory.CreateDirectory(GlobalPaths.globmodfolder); }
                InstallProgressHeader.Text = "Installing mods";
                foreach (var mod in fullVersion.mods)
                {
                    File.Copy(Path.Combine(GlobalPaths.servmodfolder, mod), Path.Combine(GlobalPaths.globmodfolder, mod), true);
                }
                try
                {
                    foreach (var usermod in Directory.GetFiles(GlobalPaths.usermodfolder))
                    {
                        string modname = Path.GetFileName(usermod);
                        File.Copy(usermod, Path.Combine(GlobalPaths.globmodfolder, modname), true);
                    }
                }
                catch { Directory.CreateDirectory(GlobalPaths.usermodfolder); }

                //texturez!!!
                try { Array.ForEach(Directory.GetFiles(GlobalPaths.resourcepacks), File.Delete); }
                catch { Directory.CreateDirectory(GlobalPaths.resourcepacks); }
                InstallProgressHeader.Text = "Installing resourcepacks";
                foreach (var rps in fullVersion.resourcepacks)
                {
                    File.Copy(Path.Combine(GlobalPaths.serverpacks, rps), Path.Combine(GlobalPaths.resourcepacks, rps), true);
                }
                try
                {
                    foreach (var usrpack in Directory.GetFiles(Path.Combine(GlobalPaths.mcpath, "user_resourcepacks")))
                    {
                        string packname = Path.GetFileName(usrpack);
                        File.Copy(usrpack, Path.Combine(GlobalPaths.resourcepacks, packname), true);
                    }
                }
                catch { Directory.CreateDirectory(Path.Combine(GlobalPaths.mcpath, "user_resourcepacks")); }

                //enable shaders (yes, itz code reuse, i know that it sucks)
                if (Globals.config.enableShaders)
                {
                    var opline = "shaderPack=";
                    if (File.Exists(GlobalPaths.shaderconfig))
                    {
                        string[] arrLine = File.ReadAllLines(GlobalPaths.shaderconfig);
                        foreach (var item in arrLine.Select((value, index) => new { value, index }))
                        {
                            var lin = item.value;
                            var ind = item.index;
                            if (lin.Contains("shaderPack"))
                            {
                                foreach (var rp in fullVersion.shaders)
                                {
                                    opline = "shaderPack=" + rp;
                                }
                                //MessageBox.Show(opline);
                                arrLine[ind] = opline;
                            }
                        }
                        File.WriteAllLines(GlobalPaths.shaderconfig, arrLine);
                    }
                    else
                    {
                        foreach (var rp in fullVersion.shaders)
                        {
                            opline = "shaderPack=" + rp;
                        }
                        File.WriteAllText(GlobalPaths.shaderconfig, opline);
                    }
                }
                try
                {
                    foreach (var usersp in Directory.GetFiles(Path.Combine(GlobalPaths.mcpath, "user_shaderpacks")))
                    {
                        string spname = Path.GetFileName(usersp);
                        File.Copy(usersp, Path.Combine(GlobalPaths.shaderpacks, spname), true);
                    }
                }
                catch { Directory.CreateDirectory(Path.Combine(GlobalPaths.mcpath, "user_shaderpacks")); }

                //russian lang
                if (!File.Exists(Path.Combine(GlobalPaths.mcpath, "options.txt")))
                {
                    File.WriteAllText(Path.Combine(GlobalPaths.mcpath, "options.txt"), "lang:ru_ru");
                }


                //Hide the folders from stoopid users
                hide("assets");
                hide("libraries");
                hide("mods");
                hide("resourcepacks");
                hide("runtime");
                hide("shaderpacks");
                hide("versions");
                hide("servermods");
                hide("defaultconfigs");
                hide("serverpacks");

                //LAUNCH MINCERAFT and write vars to conf
                InstallProgress.IsVisible = false;
                InstallProgressHeader.Text = "Пожалуйста подождите...";

                //move maps and waypoints to new folder
                if (Globals.config.lastip != Globals.news.ip)
                {
                    Console.WriteLine("Searching for xaero maps and waypoints");
                    String xaerofolder =  Path.Combine(Path.Combine(GlobalPaths.mcpath, "xaero"), "world-map");
                    String waypoints =  Path.Combine(GlobalPaths.mcpath, "XaeroWaypoints");
                    if (Directory.Exists(Path.Combine(xaerofolder, "Multiplayer_" + Globals.config.lastip)))
                    {
                        Directory.Move(Path.Combine(xaerofolder, "Multiplayer_" + Globals.config.lastip), 
                            Path.Combine(xaerofolder, "Multiplayer_" + Globals.news.ip));
                    } else {Console.WriteLine("Maps not found");}
                    if (Directory.Exists(Path.Combine(waypoints, "Multiplayer_" + Globals.config.lastip)))
                    {
                        Directory.Move(Path.Combine(waypoints, "Multiplayer_" + Globals.config.lastip), 
                            Path.Combine(waypoints, "Multiplayer_" + Globals.news.ip));
                    }else {Console.WriteLine("Waypoints not found");}
                }
                
                
                
                Hide();
                Globals.config.lastip = Globals.news.ip;
                Globals.config.lastport = Globals.news.port;
                Globals.config.notafirsttime = true;
                var processUtil = new ProcessWrapper(process);
                WriteConfig();
                processUtil.OutputReceived += (s, e) => Console.WriteLine(e);
                processUtil.StartWithEvents();
                await processUtil.WaitForExitTaskAsync();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Show error
                var msg = MessageBoxManager.GetMessageBoxStandard("",
                    "Произошла ошибка.\nОтправьте репорт разрабочику.\n\n" + ex.ToString());
                await msg.ShowAsync();
                Environment.Exit(0);
            }

        }
    }

    
    private async ValueTask<Process> InstallAndBuildOnline(String nickname)
    {
        MinecraftLauncher _launcher = new MinecraftLauncher(new MinecraftPath(GlobalPaths.mcpath));
        try 
        { 
            FileMgr.cleanMcFolder(GlobalPaths.mcpath);
        }
        catch (Exception ex) 
        { 
            var msg = MessageBoxManager.GetMessageBoxStandard("", 
                "Произошла ошибка при подготовке к установке Minecraft.\nЗакройте все процессы использующие Java или перезагрузите компьютер.\n\nКод ошибки: " +
                ex.ToString());
            await msg.ShowAsync(); 
            Environment.Exit(0);
        }

        _launcher.FileProgressChanged += (_, e) =>
        {
            InstallProgressHeader.Text = $"{Path.GetFileName(e.Name)} [{e.ProgressedTasks}/{e.TotalTasks}]";
        };
        _launcher.ByteProgressChanged += (_, e) =>
        {
            InstallProgress.Value = Convert.ToInt32(e.ToRatio() * 100);
        };
        var forge = new ForgeInstaller(_launcher);
        var version_name = await forge.Install(Globals.news.mcversion);
        
        var launchOption = new MLaunchOption
        {
            MaximumRamMb = Globals.config.ram,
            Session = MSession.CreateOfflineSession(nickname),
        };
        if (Globals.config.faststart)
        {
            launchOption = new MLaunchOption
            {
                MaximumRamMb = Globals.config.ram,
                Session = MSession.CreateOfflineSession(nickname),
                ServerIp = Globals.news.ip,
                ServerPort = Globals.news.port,
            };
        }
        var process = await _launcher.InstallAndBuildProcessAsync(version_name, launchOption);
        return process;
    }
    
    private async ValueTask<Process> InstallAndBuildOffline(String nickname)
    {
        //create this fucking launcher task
        var path = new MinecraftPath(GlobalPaths.mcpath);
        var parameters = MinecraftLauncherParameters.CreateDefault(path);
        parameters.VersionLoader = new LocalJsonVersionLoader(path);
        MinecraftLauncher _launcher = new MinecraftLauncher(parameters);

        //check and wipe 
        if (!FileMgr.IsMinecraftHere())
        {
            try
            {
                FileMgr.cleanMcFolder(GlobalPaths.mcpath);
            }
            catch (Exception ex)
            {
                var msg = MessageBoxManager.GetMessageBoxStandard("",
                    "Произошла ошибка при подготовке к установке Minecraft.\nЗакройте все процессы использующие Java или перезагрузите компьютер.\n\nКод ошибки: " +
                    ex.ToString());
                await msg.ShowAsync();
                Environment.Exit(0);
            }

            //download minceraft
            FileMgr fileMgr = new FileMgr();
            var file = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                file = "minecraft-win.zip";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                file = "minecraft-osx.zip";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                file = "minecraft-linux.zip";
            }
            await fileMgr.DownloadAndUnpack(Path.Combine(GlobalPaths.serverpath, file), GlobalPaths.mcpath,
                InstallProgress, InstallProgressHeader);
        }
        
        String version_name = "";
        foreach (var element in Directory.GetDirectories(Path.Combine(GlobalPaths.mcpath, "versions")))
        {
            var namel = Path.GetFileName(element);
            if (namel.Contains("forge"))
            {
                version_name = namel;
                break;
            }
        }
        Console.WriteLine("Starting "+version_name);
        
        var launchOption = new MLaunchOption
        {
            MaximumRamMb = Globals.config.ram,
            Session = MSession.CreateOfflineSession(nickname),
        };
        if (Globals.config.faststart)
        {
            launchOption = new MLaunchOption
            {
                MaximumRamMb = Globals.config.ram,
                Session = MSession.CreateOfflineSession(nickname),
                ServerIp = Globals.news.ip,
                ServerPort = Globals.news.port,
            };
        }
        var process = await _launcher.BuildProcessAsync(version_name, launchOption);
        return process;
    }
    
    //silly updater
    private async void updatelauncher()
    {
        IsEnabled = false;
        var box = MessageBoxManager
            .GetMessageBoxStandard("Доступно обновление лаунчера!", "Нажмите ОК, чтобы загрузить обновление "  + Globals.server_version,
                ButtonEnum.OkCancel);
        var result = await box.ShowAsync();
        if (result == ButtonResult.Ok)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DownloadFileSync(Path.Combine(GlobalPaths.serverpath, "update.exe"), Path.Combine(GlobalPaths.datapath, "update.exe")); 
                Process.Start("explorer.exe", Path.Combine(GlobalPaths.datapath, "update.exe"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var launcher = GetTopLevel(this).Launcher;
                await launcher.LaunchUriAsync(new Uri(Path.Combine(GlobalPaths.serverpath, "update.dmg")));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var launcher = GetTopLevel(this).Launcher;
                await launcher.LaunchUriAsync(new Uri(Path.Combine(GlobalPaths.serverpath, "update.AppImage")));
            }
            Environment.Exit(0);
        }
        IsEnabled = true;
    }
    
    public static void DownloadFileSync(string url, string dest)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
        File.WriteAllBytes(dest, data);
        Thread.Sleep(50);
    }
    private void hide(string foldername)
    {
        var folderpath = Path.Combine(GlobalPaths.mcpath, foldername);
        if (!Directory.Exists(folderpath))
        { Directory.CreateDirectory(folderpath); }
        DirectoryInfo di = new DirectoryInfo(folderpath);
        if (!di.Attributes.HasFlag(FileAttributes.Hidden))
        { di.Attributes |= FileAttributes.Hidden; }
    }
    private void plzresizeit()
    {
        int height = Screens.Primary.Bounds.Height;
        double k = (Convert.ToDouble(height) / 1080F);
        Width = Convert.ToInt32(Width*k);
        Height = Convert.ToInt32(Height*k);
    }
    
    //make window draggable
    private bool _isWindowDragInEffect = false;
    private Point _cursorPositionAtWindowDragStart = new(0, 0);
    private void WindowDragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isWindowDragInEffect)
        {
            Point currentCursorPosition = e.GetPosition(this);
            Point cursorPositionDelta = currentCursorPosition - _cursorPositionAtWindowDragStart;
            Position = this.PointToScreen(cursorPositionDelta);
        }
    }
    private void WindowDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isWindowDragInEffect = true;
        _cursorPositionAtWindowDragStart = e.GetPosition(this);
    }
    private void WindowDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e) => _isWindowDragInEffect = false;
    
    private async void VersionBoxChange(object? sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        if ((VersionBox.SelectedIndex == (VersionBox.Items.Count - 1)) && (Globals.isLoading == false))
        {
            IsEnabled = false;
            if (Globals.notafirstrun == true)
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Значит ты выбрал Usermods...", 
                        "Вы выбрали пользовательские моды в качестве модпака.\nУчтите, что в этом режиме сборки от создателей ReignCraft не будут использоваться, вы должны добавить свой модпак в usermods\nВы хотите открыть папку с пользовательскими модами сейчас?",
                        ButtonEnum.OkCancel);
                var result = await box.ShowAsync();
                if (result == ButtonResult.Ok)
                {
                    FileMgr.OpenDirectory(GlobalPaths.usermodfolder);
                }
            }
            IsEnabled = true;
        }
        Globals.notafirstrun = true;

        FileMgr fileMgr = new FileMgr();
        Ver? fullVersion = Globals.packfile.ver.FirstOrDefault(v => v.name == VersionBox.SelectedItem);
        
        Globals.dwn_mods = fileMgr.getListOfDownloads(GlobalPaths.servmodfolder, fullVersion.mods);
        Globals.dwn_resourcepacks = fileMgr.getListOfDownloads(GlobalPaths.serverpacks, fullVersion.resourcepacks);
        Globals.dwn_shaders = fileMgr.getListOfDownloads(GlobalPaths.shaderpacks, fullVersion.shaders);
        
        if (Globals.dwn_mods.Count == 0 && Globals.dwn_resourcepacks.Count == 0 && Globals.dwn_shaders.Count == 0)
        { Globals.pack_update = false; }
        else
        { Globals.pack_update = true; }
        
        if (!Globals.pack_update)
        { PackUpdateBox.IsVisible = false; }
        else
        { PackUpdateBox.IsVisible = true; }
    }
    
    private void openFiles(object? sender, RoutedEventArgs routedEventArgs)
    {
        FileMgr.OpenDirectory(GlobalPaths.mcpath);
    }
    
    private async void openSettings(object? sender, RoutedEventArgs routedEventArgs)
    {
        WriteConfig();
        IsEnabled = false;
        var window = new SettingsWindow();
        await window.ShowDialog(this);
        IsEnabled = true;
        ReadConfig();
    }
    
    private void CloseBtnClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        Environment.Exit(0);
    }
    private void MinimizeBtnClick(object? sender, RoutedEventArgs routedEventArgs)
    {
        this.WindowState = WindowState.Minimized;
    }
    private void UpdateBtnClick(object? sender, RoutedEventArgs e)
    {
        updatelauncher();
    }

    private async void TextbookClick(object? sender, RoutedEventArgs e)
    {
        var launcher = GetTopLevel(this).Launcher;
        await launcher.LaunchUriAsync(new Uri("https://modrinth.com/organization/reign-devs"));
    }

    private async void SunflowerClick(object? sender, RoutedEventArgs e)
    {
        var launcher = GetTopLevel(this).Launcher;
        await launcher.LaunchUriAsync(new Uri("https://t.me/reignmod"));
    }
}