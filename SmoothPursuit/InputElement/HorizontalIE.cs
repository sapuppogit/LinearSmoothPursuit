using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SmoothPursuit {
    public class HorizontalIE : InputElement {

        public HorizontalIE(WinProperties winProp, string direction) :
            base(winProp, direction) {

            TargetInfo = new(direction, winProp.CellWidth, winProp.Settings.TargetHeight, winProp.Settings.CharHeight);
            TargetInfo.TargetColor = winProp.Settings.ThemeColors.Circle;
            TargetInfo.Target.Fill = winProp.Settings.ThemeColors.Circle;
            TargetInfo.GroupTxt.Foreground = winProp.Settings.ThemeColors.Circle;
            TargetInfo.GroupTxt.TextAlignment = TextAlignment.Center;

            Canvas.Children.Add(TargetInfo.Target);
            Canvas.Children.Add(TargetInfo.GroupTxt);

            Line.Height = winProp.Settings.LineHeight;
            Canvas.SetTop(SpeedBlock, 20);
            Canvas.SetLeft(SpeedBlock, 10);
        }

        public override void UpdateSize(double width, double height, Point canvasPos) {
            Canvas.HorizontalAlignment = HorizontalAlignment.Center;
            Canvas.VerticalAlignment = VerticalAlignment.Center;
            Canvas.Width = width;
            Canvas.Height = height;
            Line.Width = width;
            TargetInfo.UpdateSize(width, canvasPos);
            TargetInfo.GroupTxt.Width = width;

            Canvas.SetBottom(Line, (height / 2) - (Line.Height / 2));
            Canvas.SetBottom(TargetInfo.GroupTxt, (height / 2) + 10);
            Canvas.SetLeft(TargetInfo.Target, (-TargetInfo.Target.Height / 2) + (Direction == SmoothPursuit.Direction.Right ? 0 : width));
            Canvas.SetTop(TargetInfo.Target, (height / 2) - (TargetInfo.Target.Height - 1) / 2);
        }
    }
}
