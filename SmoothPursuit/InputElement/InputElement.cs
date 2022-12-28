using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SmoothPursuit {
    public abstract class InputElement {
        public Canvas Canvas { get; set; }
        public TextBlock SpeedBlock { get; set; }
        public Rectangle Line { get; set; }
        public string Direction { get; set; }

        protected double margin;

        protected double targetSpeed;

        protected bool atTheEnd;

        protected Visibility speedVisibility;

        public TargetInfo TargetInfo { get; set; }

        public InputElement(WinProperties winProp, string direction) {
            Canvas = new();
            SpeedBlock = new();
            Line = new();
            Direction = direction;

            _ = Canvas.Children.Add(Line);
            _ = Canvas.Children.Add(SpeedBlock);

            Line.Fill = winProp.Settings.ThemeColors.Line;
            margin = winProp.Settings.LineMargin;

            speedVisibility = Visibility.Hidden;
        }

        public virtual void SetSpeed(double val) {
            TargetInfo.Speed = val;
            SpeedBlock.Text = "" + val;
        }

        public double GetSpeed() {
            return TargetInfo.Speed;
        }

        public virtual TargetInfo[] GetTargetInfo() {
            return new[] { TargetInfo };
        }

        /* Update components sizes */
        public abstract void UpdateSize(double width, double height, Point canvasPos);

        /* Show input element */
        public virtual void ResetComponents() {
            SpeedBlock.Text = "";
            SpeedBlock.Visibility = speedVisibility;
            Line.Visibility = Visibility.Visible;
            TargetInfo.ResetComponents();
        }

        /* Hide input element */
        public virtual void Hide() {
            SpeedBlock.Visibility = Visibility.Hidden;
            Line.Visibility = Visibility.Hidden;
            TargetInfo.Hide();
        }

        /* Hide second circle */
        public virtual void HideSecond() { }

        /* Move circle */
        public virtual bool Move() {
            return TargetInfo.Move();
        }
    }
}
