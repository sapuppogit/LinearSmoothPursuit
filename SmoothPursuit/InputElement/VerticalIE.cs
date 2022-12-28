using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SmoothPursuit {
    public class VerticalIE : InputElement {

        public VerticalIE(WinProperties winProp, string direction) :
            base(winProp, direction) {

            TargetInfo = new(direction, winProp.CellHeight, winProp.Settings.TargetHeight, winProp.Settings.CharHeight);
            TargetInfo.TargetColor = winProp.Settings.ThemeColors.Circle;
            TargetInfo.Target.Fill = winProp.Settings.ThemeColors.Circle;
            TargetInfo.GroupTxt.Foreground = winProp.Settings.ThemeColors.Circle;

            Canvas.Children.Add(TargetInfo.Target);
            Canvas.Children.Add(TargetInfo.GroupTxt);

            Line.Width = winProp.Settings.LineHeight;
            TargetInfo.GroupTxt.Width = 110;
            TargetInfo.GroupTxt.TextAlignment = TextAlignment.Right;

            Canvas.SetTop(SpeedBlock, 20);
            Canvas.SetLeft(SpeedBlock, 10);
        }

        public override void UpdateSize(double width, double height, Point canvasPos) {
            Canvas.HorizontalAlignment = HorizontalAlignment.Center;
            Canvas.VerticalAlignment = VerticalAlignment.Center;
            Canvas.Width = width;
            Canvas.Height = height;
            Line.Height = height;

            if (TargetInfo.Group != null) {
                if (TargetInfo.Group.Type == GroupType.Lowercase) {
                    TargetInfo.GroupTxt.FontSize = 20;
                }
                else if (TargetInfo.Group.Type == GroupType.Uppercase || TargetInfo.Group.Type == GroupType.Prevision) {
                    TargetInfo.GroupTxt.FontSize = 18;
                }
            }
            TargetInfo.UpdateSize(height, canvasPos);

            Canvas.SetLeft(Line, (width * 2 / 3) - (Line.Width / 2));
            Canvas.SetTop(TargetInfo.Target, (-TargetInfo.Target.Height / 2) + (Direction == SmoothPursuit.Direction.Down ? 0 : height));
            Canvas.SetLeft(TargetInfo.Target, (width * 2 / 3) - (TargetInfo.Target.Width / 2));
            Canvas.SetTop(TargetInfo.GroupTxt, (height / 2) - 20);
            Canvas.SetLeft(TargetInfo.GroupTxt, (width * 2 / 3) - TargetInfo.GroupTxt.Width - 30);
        }
    }
}
