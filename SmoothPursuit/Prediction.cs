using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SmoothPursuit {

    public class Prediction {
        private readonly string langFile;
        public readonly bool IsActive;
        public Prediction(string lang) {
            langFile = "./../../../Vocabulary/" + lang + ".txt";
            IsActive = File.Exists(langFile);
        }
        public List<string> GetPrevision(string text, int nWords) {
            text = text[..^1];
            return IsActive ? File.ReadLines(langFile).Where(r => r.StartsWith(text.ToLower()) && r != text.ToLower()).Take(5).ToList() : new();
        }
    }
}
