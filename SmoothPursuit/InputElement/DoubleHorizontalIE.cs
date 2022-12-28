using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SmoothPursuit {
    public class DoubleHorizontalIE : HorizontalIE {
        public TargetInfo TargetInfo2 { get; set; }

        public DoubleHorizontalIE(WinProperties winProp) :
            base(winProp, SmoothPursuit.Direction.Right) {

            TargetInfo2 = new(SmoothPursuit.Direction.Left, winProp.CellWidth, winProp.Settings.TargetHeight, winProp.Settings.CharHeight);
            TargetInfo.TargetColor = winProp.Settings.ThemeColors.CircleBothDirections[0];
            TargetInfo.Target.Fill = winProp.Settings.ThemeColors.CircleBothDirections[0];
            TargetInfo.GroupTxt.Foreground = winProp.Settings.ThemeColors.CircleBothDirections[0];
            TargetInfo2.TargetColor = winProp.Settings.ThemeColors.CircleBothDirections[1];
            TargetInfo2.Target.Fill = winProp.Settings.ThemeColors.CircleBothDirections[1];
            TargetInfo2.GroupTxt.Foreground = winProp.Settings.ThemeColors.CircleBothDirections[1];
            
            Canvas.Children.Add(TargetInfo2.Target);
            Canvas.Children.Add(TargetInfo2.GroupTxt);

            TargetInfo.GroupTxt.TextAlignment = TextAlignment.Center;
            TargetInfo2.GroupTxt.TextAlignment = TextAlignment.Center;
        }

        public override void UpdateSize(double width, double height, Point canvasPos) {
            base.UpdateSize(width,  height, canvasPos);
            TargetInfo2.UpdateSize(width, canvasPos);
            TargetInfo2.GroupTxt.Width = width;

            Canvas.SetBottom(TargetInfo2.GroupTxt, (height / 2) - 45);
            Canvas.SetLeft(TargetInfo2.Target, width - (TargetInfo.Target.Height / 2));
            Canvas.SetTop(TargetInfo2.Target, (height / 2) - (TargetInfo2.Target.Height / 2));
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
            TargetInfo.GroupTxt.Visibility = Visibility.Hidden;
            TargetInfo.Target.Visibility = Visibility.Hidden;
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
