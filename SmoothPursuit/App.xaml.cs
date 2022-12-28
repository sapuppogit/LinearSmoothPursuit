using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;

namespace SmoothPursuit {

    public class SettingParse {
        public Setting Settings { get; set; }
        public string Theme { get; set; }
        public ThemeColorsParse[] ThemesColors { get; set; }
    }

    public class ThemeColorsParse {
        public string Theme { get; set; }
        public string WindowBG { get; set; }
        public string WindowFG { get; set; }
        public string Circle { get; set; }
        public string[] CircleBothDirections { get; set; }
        public string Line { get; set; }
        public string TextBlockBG { get; set; }
        public string TextBlockFG { get; set; }
    }

    public class Setting {
        public string Mode { get; set; }
        public string Direction { get; set; }
        public string Language { get; set; }
        public double[] Speeds { get; set; }
        public double SingleSpeed { get; set; }
        public int NLines { get; set; }
        public int NColumns { get; set; }
        public int TargetHeight { get; set; }
        public int LineHeight { get; set; }
        public int CharHeight { get; set; }
        public int TextBoxHeight { get; set; }
        public int TextBoxWidth { get; set; }
        public int TextBoxMargin { get; set; }
        public int VerticalBackDeleteSize { get; set; }
        public int HorizontalBackDeleteSize { get; set; }
        public int LineMargin { get; set; }
        public ThemeColorsSettings ThemeColors { get; set; }

    }

    public class ThemeColorsSettings {
        public SolidColorBrush Circle { get; set; }
        public SolidColorBrush[] CircleBothDirections { get; set; }
        public SolidColorBrush Line { get; set; }

        public ThemeColorsSettings(string circle, string[] cbd, string line) {
            Circle = new SolidColorBrush((Color)ColorConverter.ConvertFromString(circle));
            CircleBothDirections = new SolidColorBrush[] {
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString(cbd[0])),
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString(cbd[1]))};
            Line = new SolidColorBrush((Color)ColorConverter.ConvertFromString(line));
        }
    }

    public partial class App : Application {
        private SettingParse settingsParse;
        private Setting settings;

        private void Application_Startup(object sender, StartupEventArgs e) {
            InitializeComponent();
            StreamReader r = new("../../../Properties/settings.json");
            string json = r.ReadToEnd();
            settingsParse = JsonConvert.DeserializeObject<SettingParse>(json);

            settings = settingsParse.Settings;
            foreach (ThemeColorsParse th in settingsParse.ThemesColors) {
                if (settingsParse.Theme == th.Theme) {
                    settings.ThemeColors = new(th.Circle, th.CircleBothDirections, th.Line);
                    break;
                }
            }

            MainWindow mw = new(new WinProperties(settings));

            foreach (ThemeColorsParse th in settingsParse.ThemesColors) {
                if (settingsParse.Theme == th.Theme) {
                    mw.myWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(th.WindowBG));
                    mw.myWindow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(th.WindowFG));
                    mw.myText.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(th.TextBlockBG));
                    mw.myText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(th.TextBlockFG));
                    mw.myText.Margin = new Thickness(0, settingsParse.Settings.TextBoxMargin, 0, settingsParse.Settings.TextBoxMargin);
                    mw.myText.Measure(new Size(settingsParse.Settings.TextBoxHeight, settingsParse.Settings.TextBoxWidth));
                    mw.myText.Width = settingsParse.Settings.TextBoxWidth;
                    break;
                }
            }
        }
    }
}
