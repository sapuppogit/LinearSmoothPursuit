using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmoothPursuit {
    public class TargetInfo {
        public string StartingDirection { get; set; }
        public string ActualDirection { get; set; }
        public double Speed { get; set; }
        public double LineLenght { get; set; }
        public Point Position { get; set; }
        public Point CanvasPosition { get; set; }
        public Action Group { get; set; }
        public TextBlock GroupTxt { get; set; }
        public Ellipse Target { get; set; }
        public SolidColorBrush TargetColor { get; set; }

        public bool IsDirect { get; set; }

        private bool atTheEnd;
        private readonly string lineDirection;
        private double startPosition;
        private double startPositionY;
        private double endPosition;
        private double endPositionY;

        public TargetInfo(string direction, double lineLenght, double targetHeight, int charHeight) {
            StartingDirection = direction;
            ActualDirection = direction;
            LineLenght = lineLenght;

            GroupTxt = new();
            GroupTxt.FontSize = charHeight;
            GroupTxt.TextAlignment = TextAlignment.Center;

            Target = new();
            Target.Width = targetHeight;
            Target.Height = targetHeight;
            Target.Style = (Style)Application.Current.Resources["TargetStyle"];
            TargetColor = (SolidColorBrush)Target.Fill;
            IsDirect = new[] { Direction.Right, Direction.Down}.Contains(ActualDirection);

            lineDirection = new[] { Direction.Right, Direction.Left }.Contains(direction) ? Direction.Horizontal : Direction.Vertical;

            ChangeDirection();
        }

        /* Assign a command to the circle */
        public void SetGroup(Action group) {
            Group = group;
            if (Group.Type == GroupType.Characters || Group.Type == GroupType.Accents) {
                for (int i = 0; i < (group.Text.Length * 2) - 1; i++) {
                    GroupTxt.Text += i % 2 == 0 ? ("" + group.Text[i / 2]) : ' ';
                }
            }
            else {
                GroupTxt.Text = group.Text;
            }
        }

        /* Show the input element */
        public void ResetComponents() {
            Group = null;
            GroupTxt.Text = "";
            GroupTxt.Visibility = Visibility.Visible;
            Target.Visibility = Visibility.Visible;
            Speed = -1;
            if (StartingDirection != ActualDirection) {
                ChangeDirection();
            }
            if (lineDirection == Direction.Horizontal) {
                Canvas.SetLeft(Target, startPosition);
            }
            else if (lineDirection == Direction.Vertical) {
                Canvas.SetTop(Target, startPosition);
            }
            else {
                Canvas.SetLeft(Target, startPosition);
                Canvas.SetTop(Target, startPositionY);
            }
            atTheEnd = false;
        }

        /* When the circle is at the end of the line change the direction */
        public void ChangeDirection() {
            string[] directions = new[] { Direction.Up, Direction.Right,
                                                  Direction.Down, Direction.Left};
            ActualDirection = directions[(Array.IndexOf(directions, ActualDirection) + 2) % 4];
            IsDirect = !IsDirect;

            double tempPosition = startPosition;
            startPosition = endPosition;
            endPosition = tempPosition;

            tempPosition = startPositionY;
            startPositionY = endPositionY;
            endPositionY = tempPosition;
        }

        public void UpdateSize(double lineLenght, Point canvasPos) {
            LineLenght = lineLenght;
            if (ActualDirection == Direction.Right || ActualDirection == Direction.Down) {
                startPosition = -Target.Width / 2;
                endPosition = lineLenght - (Target.Width / 2);
            }
            else {
                startPosition = lineLenght - (Target.Width / 2);
                endPosition = -Target.Width / 2;
            }
            CanvasPosition = canvasPos;
        }

        public void UpdateSize(double lineLenght, double startPositionX, double startPositionY, Point canvasPos) {
            LineLenght = lineLenght;
            startPosition = startPositionX;
            this.startPositionY = startPositionY;
            endPosition = startPositionX - lineLenght;
            endPositionY = startPositionY - lineLenght;
            CanvasPosition = canvasPos;
        }

        public Point AbsolutePosition() {
            return new Point(CanvasPosition.X + Position.X, CanvasPosition.Y + Position.Y);
        }

        public void Hide() {
            Group = null;
            GroupTxt.Visibility = Visibility.Hidden;
            Target.Visibility = Visibility.Hidden;
        }

        /* Update circle position */
        public bool Move() {
            // At the end of the line change the direction
            if (atTheEnd) {
                ChangeDirection();
                if (lineDirection == Direction.Horizontal) {
                    Canvas.SetLeft(Target, startPosition);
                }
                else if (lineDirection == Direction.Vertical) {
                    Canvas.SetTop(Target, startPosition);
                }
                else {
                    Canvas.SetLeft(Target, startPosition);
                    Canvas.SetTop(Target, startPositionY);
                }
                atTheEnd = false;
            }
            else {
                double position = (lineDirection == Direction.Vertical ? Canvas.GetTop(Target) : Canvas.GetLeft(Target)) + (Speed * (IsDirect ? 1 : -1));
                if ((!IsDirect && position <= endPosition) || (IsDirect && position >= endPosition)) {
                    position = endPosition;
                    Position = new Point(Canvas.GetLeft(Target), Canvas.GetTop(Target));
                    atTheEnd = true;
                }

                if (lineDirection == Direction.Vertical) {
                    Canvas.SetTop(Target, position);
                }
                else {
                    Canvas.SetLeft(Target, position);
                }
            }
            Position = new Point(Canvas.GetLeft(Target), Canvas.GetTop(Target));
            return false;
        }
    }
}
