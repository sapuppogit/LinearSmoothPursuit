using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace SmoothPursuit {
    public class WinProperties {

        public Setting Settings { get; set; }
        public bool Close { get; set; }
        public Screen Screen { get; set; }
        public double CellHeight { get; set; }
        public double CellWidth { get; set; }
        public List<InputElement> Cells { get; set; }
        public List<TargetInfo> TargetsInfo { get; set; }
        public InputElementSelectionAlgorithm POGsEval { get; set; }
        public Prediction Prediction { get; }


        public WinProperties(Setting settings) {
            Settings = settings;
            Prediction = new(settings.Language);

            Close = false;

            Cells = new();
            TargetsInfo = new();
            POGsEval = new(settings.Direction, Settings.Speeds);
        }
    }
}
