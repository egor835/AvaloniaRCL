using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace RCL;

public partial class SettingsWindow : Window
{
    public class Config
    {
        public string username { get; set; }
        public string version { get; set; }
        public int ram { get; set; }
        public bool proxy { get; set; }
        public bool faststart { get; set; }
        public bool enableShaders { get; set; }
        public bool notafirsttime { get; set; }
    }
    
    public static class Globals
    {
        public static string mcpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".reigncraft");
        public static string datapath = Path.Combine(mcpath, "launcher_data");
        public static string configPath = Path.Combine(datapath, "config.json");
        public static Config config;
    }

    public SettingsWindow()
    {
        InitializeComponent();
        plzresizeit();
    }
    
    private void SettingsOpen(object? sender, EventArgs e)
    {
        Globals.config = JsonSerializer.Deserialize<Config>(File.ReadAllText(Globals.configPath));
        
        Proxy.IsChecked = Globals.config.proxy;
        AutoLogin.IsChecked = Globals.config.faststart;
        ShadersNotManaged.IsChecked = !Globals.config.enableShaders;
        
        int maxram = Convert.ToInt32(CheckRam.GetTotalPhysicalMemory())-2047;
        if (maxram < 1024) { maxram = 16384; }
        RamBar.Maximum = (maxram-1024)/512;
        if (Globals.config.ram > maxram) { ramToBar(maxram); }
        else { ramToBar(Globals.config.ram); }
        RamBox.Text = "Выделенная память: " + barToRam() + " МБ";
    }
    
    
    //main code
    private void OK_click(object? sender, RoutedEventArgs e)
    {
        Globals.config.proxy = Proxy.IsChecked.Value;
        Globals.config.faststart = AutoLogin.IsChecked.Value;
        Globals.config.enableShaders = !ShadersNotManaged.IsChecked.Value;
        Globals.config.ram = barToRam();
        File.WriteAllText(Globals.configPath, JsonSerializer.Serialize(Globals.config));
        Close();
    }
    
    
    
    //drag
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
    //resize
    private void plzresizeit()
    {
        int height = Screens.Primary.Bounds.Height;
        double k = (Convert.ToDouble(height) / 1080F);
        Width = Convert.ToInt32(Width*k);
        Height = Convert.ToInt32(Height*k);
    }
    //close
    private void CloseBtnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    private async void ResetButton_click(object? sender, RoutedEventArgs e)
    {
        IsEnabled = false;
        var box = MessageBoxManager.GetMessageBoxStandard("Покончить с этим", 
            "Вы уверены, что хотите удалить все данные лаунчера, включая все моды, в том числе и пользовательские?", 
            ButtonEnum.OkCancel);
        var result = await box.ShowAsync();
        if (result == ButtonResult.Ok)
        {
            string mcpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".reigncraft");
            try
            {
                Directory.Delete(mcpath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Environment.Exit(0);
        }
        IsEnabled = true;
    }
    
    //some ram shit
    private void RamChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        RamBox.Text = "Выделенная память: " + barToRam() + " МБ";
    }
    private int barToRam()
    {
        return (Convert.ToInt32(RamBar.Value)*512)+1024;
    }
    private void ramToBar(int ram)
    {
            
        RamBar.Value = ((ram-1024) / 512);
    }
}