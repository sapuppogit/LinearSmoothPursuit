using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SmoothPursuit {
    public class DoubleVerticalIE : VerticalIE {
        public TargetInfo TargetInfo2 { get; set; }

        public DoubleVerticalIE(WinProperties winProp) :
            base(winProp, SmoothPursuit.Direction.Down) {

            TargetInfo2 = new(SmoothPursuit.Direction.Up, winProp.CellWidth, winProp.Settings.TargetHeight, winProp.Settings.CharHeight);
            TargetInfo.TargetColor = winProp.Settings.ThemeColors.CircleBothDirections[0];
            TargetInfo.Target.Fill = winProp.Settings.ThemeColors.CircleBothDirections[0];
            TargetInfo.GroupTxt.Foreground = winProp.Settings.ThemeColors.CircleBothDirections[0];
            TargetInfo2.TargetColor = winProp.Settings.ThemeColors.CircleBothDirections[1];
            TargetInfo2.Target.Fill = winProp.Settings.ThemeColors.CircleBothDirections[1];
            TargetInfo2.GroupTxt.Foreground = winProp.Settings.ThemeColors.CircleBothDirections[1];

            Canvas.Children.Add(TargetInfo2.Target);
            Canvas.Children.Add(TargetInfo2.GroupTxt);

            TargetInfo2.GroupTxt.Width = 110;
            TargetInfo2.GroupTxt.TextAlignment = TextAlignment.Right;
        }

        public override void UpdateSize(double width, double height, Point canvasPos) {
            base.UpdateSize(width, height, canvasPos);

            if (TargetInfo2.Group != null) {
                if (TargetInfo2.Group.Type == GroupType.Lowercase) {
                    TargetInfo2.GroupTxt.FontSize = 20;
                }
                else if (TargetInfo2.Group.Type == GroupType.Uppercase || TargetInfo2.Group.Type == GroupType.Prevision) {
                    TargetInfo2.GroupTxt.FontSize = 18;
                }
            }

            TargetInfo2.UpdateSize(height, canvasPos);

            Canvas.SetTop(TargetInfo.GroupTxt, height / 3 - 10);
            Canvas.SetTop(TargetInfo2.GroupTxt, height * 2 / 3 - 10);
            Canvas.SetLeft(TargetInfo2.GroupTxt, (width * 2 / 3) - TargetInfo.GroupTxt.Width - 30);
            Canvas.SetLeft(TargetInfo2.Target, (width * 2 / 3) - (TargetInfo2.Target.Width / 2));
            Canvas.SetTop(TargetInfo2.Target, height - (TargetInfo.Target.Width / 2));
        }

        public override void SetSpeed(double val) {
            TargetInfo.Speed = val;
            TargetInfo2.Speed = val;
            SpeedBlock.Text = "" + val;
        }

        /* Hide input element */
        public override void Hide() {
            SpeedBlock.Visibility = Visibility.Hidden;
            Line.Visibility = Visibility.Hidden;
            TargetInfo.Hide();
            TargetInfo2.Hide();
            HideSecond();
        }

        /* Hide second circle */
        public override void HideSecond() {
            TargetInfo2.GroupTxt.Visibility = Visibility.Hidden;
            TargetInfo2.Target.Visibility = Visibility.Hidden;
        }

        public override TargetInfo[] GetTargetInfo() {
            return new[] { TargetInfo, TargetInfo2 };
        }

        /* Show input element */
        public override void ResetComponents() {
            SpeedBlock.Text = "";
            SpeedBlock.Visibility = speedVisibility;
            Line.Visibility = Visibility.Visible;
            TargetInfo.ResetComponents();
            TargetInfo2.ResetComponents();
        }

        /* Move circle */
        public override bool Move() {
            _ = TargetInfo.Move();
            return TargetInfo2.Move();
        }
    }
}
