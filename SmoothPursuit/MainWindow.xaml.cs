using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace SmoothPursuit {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private TcpClient gp3_client;
        private NetworkStream data_feed;
        private StreamWriter data_write;
        private readonly WinProperties winProp;
        private readonly int maxNLines;

        private bool fullScreen;
        private double winHeight;
        private double winWidth;

        private readonly DispatcherTimer targetTimer;

        private DateTime chronos;
        private int times;
        private bool activeGaze;

        private string choice;
        private bool activeWrite;

        public MainWindow(WinProperties winProp) {
            InitializeComponent();
            this.winProp = winProp;
            maxNLines = Direction.BothDirections(winProp.Settings.Direction) ? (winProp.Settings.NLines + 1) / 2 : winProp.Settings.NLines;
            winProp.Settings.NColumns = Math.Min(maxNLines, winProp.Settings.NColumns);
            bool isHorizontalOrVertical = Direction.IsHorizontal(winProp.Settings.Direction) || Direction.IsVertical(winProp.Settings.Direction);
            choice = "";

            /* Initialize screen */
            InitializeGrid();
            SetScreen(new InstructionsScreen(winProp.Settings.NLines, isHorizontalOrVertical, winProp.Prediction.IsActive));

            /* Configure threads settings */
            // TargetTimerEvent update targets position and after 20 samples make a PoG (Point of Gaze)
            // GetGazeSamples thread read the input stream of the samples, get the data and collect the samples coordinates
            targetTimer = new();
            targetTimer.Tick += TargetTimerEvent;
            targetTimer.Interval = TimeSpan.FromMilliseconds(13.333);
            Show();
            targetTimer.Start();

            InitializeConnectionToEyeTracker();

            Thread t = new(new ThreadStart(GetGazeSamples));
            t.Start();
        }

        private void InitializeConnectionToEyeTracker() {
            const int ServerPort = 4242;
            const string ServerAddr = "127.0.0.1";

            /* Open a connection with the eye tracker */
            try {
                gp3_client = new TcpClient(ServerAddr, ServerPort);
                // Load the read and write streams
                data_feed = gp3_client.GetStream();
                data_write = new StreamWriter(data_feed);

                // Setup the data records
                data_write.Write("<SET ID=\"ENABLE_SEND_POG_FIX\" STATE=\"1\" />\r\n");
                data_write.Write("<SET ID=\"ENABLE_SEND_CURSOR\" STATE=\"1\" />\r\n");
                data_write.Write("<SET ID=\"ENABLE_SEND_DATA\" STATE=\"1\" />\r\n");
                data_write.Write("<SET ID=\"ENABLE_SEND_POG_BEST\" STATE=\"1\" />\r\n");

                // Flush the buffer out the socket
                data_write.Flush();
            }
            catch (Exception e) {
                Console.WriteLine("Failed to connect with error: {0}", e);
                return;
            }
        }

        /* Locate input elements in cells equally spaced */
        private void InitializeGrid() {
            bool isHorizontal = Direction.IsHorizontal(winProp.Settings.Direction);
            bool isVertical = Direction.IsVertical(winProp.Settings.Direction);

            // Create instance of columns and rows
            for (int i = 0; i < winProp.Settings.NColumns + (isHorizontal ? 1 : 0); i++) {
                ColumnDefinition colDef = new();
                myGrid.ColumnDefinitions.Add(colDef);
            }
            for (int i = 0; i <= ((maxNLines + winProp.Settings.NColumns - 1) / winProp.Settings.NColumns) + (isVertical ? 1 : 0); i++) {
                RowDefinition rowDef = new();
                myGrid.RowDefinitions.Add(rowDef);
            }

            // Initialize the textbox
            Grid.SetRow(myText, 0);
            Grid.SetColumn(myText, 0);
            Grid.SetColumnSpan(myText, winProp.Settings.NColumns + (isHorizontal ? 1 : 0));

            List<string> directions = Direction.GetDirections(winProp.Settings.Direction).ToList();

            // Create Cell content based on direction
            int t = 0;
            for (int i = 0; i < maxNLines + (isHorizontal || isVertical ? 1 : 0); i++) {
                string direction;
                if (i == maxNLines) {
                    direction = isHorizontal ? Direction.Vertical : Direction.Horizontal;
                }
                else {
                    if (winProp.Settings.NColumns % 2 == 0) {
                        if (i != 0 && i % winProp.Settings.NColumns == 0) {
                            t = 0;
                            directions.Reverse();
                        }
                        direction = directions[t % directions.Count];
                        t++;
                    }
                    else {
                        direction = directions[i % directions.Count];
                    }
                }
                InputElement cell =
                    direction == Direction.Horizontal ? new DoubleHorizontalIE(winProp) :
                    direction == Direction.Vertical ? new DoubleVerticalIE(winProp) :
                    new[] { Direction.Right, Direction.Left }.Contains(direction) ? new HorizontalIE(winProp, direction) :
                    new VerticalIE(winProp, direction);

                winProp.Cells.Add(cell);
                myGrid.Children.Add(cell.Canvas);
                winProp.TargetsInfo.AddRange(cell.GetTargetInfo());

                // Set Cells to Grid
                if (i < maxNLines) {
                    Grid.SetRow(cell.Canvas, (winProp.Settings.NColumns + i) / winProp.Settings.NColumns);
                    Grid.SetColumn(cell.Canvas, i % winProp.Settings.NColumns);
                }
                else {
                    // Set Back and Delete Cells to Grid
                    if (isHorizontal) {
                        Grid.SetRow(cell.Canvas, 1);
                        Grid.SetColumn(cell.Canvas, winProp.Settings.NColumns);
                        Grid.SetRowSpan(cell.Canvas, myGrid.RowDefinitions.Count - 1);
                    }
                    else {
                        Grid.SetRow(cell.Canvas, myGrid.RowDefinitions.Count);
                        Grid.SetColumn(cell.Canvas, 0);
                        Grid.SetColumnSpan(cell.Canvas, winProp.Settings.NColumns);
                    }
                }
            }
            winProp.POGsEval.SetTargetsInfo(winProp.TargetsInfo);
        }


        /* Update Grid with screen specific input elements */
        private void SetScreen(Screen p) {
            bool isHorizontal = Direction.IsHorizontal(winProp.Settings.Direction);
            bool isVertical = Direction.IsVertical(winProp.Settings.Direction);
            winProp.Screen = p;
            myText.Visibility = p.Type == ScreenType.Instructions ? Visibility.Hidden : Visibility.Visible;
            ResetComponents();

            // Assign character clusters and functions to baseGroups
            List<Action> baseGroups = new();
            baseGroups.AddRange(p.CharactersGroups);
            baseGroups.AddRange(p.DefaultGroups);
            if (!isHorizontal && !isVertical) {
                baseGroups.AddRange(p.BackDeleteGroups);
            }

            bool bothDirections = Direction.BothDirections(winProp.Settings.Direction);

            string groupTxt = "";
            for (int i = 0; i < winProp.Settings.NLines; i++) {
                groupTxt = "";
                if (i < baseGroups.Count) {
                    if (winProp.Screen.Type == ScreenType.Letters && winProp.Prediction.IsActive && baseGroups[i].Type == GroupType.Prevision) {
                        // If Word Prediction is active, get 4 words
                        if (myText.Text.Length > 1) {
                            foreach (var el in winProp.Prediction.GetPrevision(myText.Text, Math.Min(4, winProp.Settings.NLines))) {
                                groupTxt += el.ToUpper() + " ";
                            }
                            if (groupTxt.Length > 0) {
                                groupTxt = groupTxt[..^1];
                                baseGroups[i].Text = groupTxt;
                            }
                            else {
                                baseGroups[i].Text = GroupType.NoResults;
                            }
                        }
                        else {
                            baseGroups[i].Text = GroupType.Words;
                        }
                    }

                    // Set Group to TargetInfos
                    winProp.TargetsInfo[i].SetGroup(baseGroups[i]);
                    // If both directions setted and actual groups are odds, hide last cell's second target
                    if (bothDirections && i == baseGroups.Count - 1 && baseGroups.Count % 2 == 1) {
                        winProp.Cells[(i + 1) / 2].HideSecond();
                        i++;
                    }
                }
                else {
                    // Hide cell if actual lines are less than cells
                    if (bothDirections) {
                        winProp.Cells[i / 2].Hide();
                        i++;
                    }
                    else {
                        winProp.Cells[i].Hide();
                    }
                }
            }

            if (isHorizontal || isVertical) {
                // Hide or assign Back and Delete groups if they are or not in the screen
                if (p.BackDeleteGroups.Count == 0) {
                    winProp.Cells[^1].Hide();
                }
                else if (p.BackDeleteGroups.Count == 1) {
                    winProp.TargetsInfo[^2].SetGroup(p.BackDeleteGroups[0]);
                    winProp.Cells[^1].HideSecond();
                }
                else {
                    winProp.TargetsInfo[^2].SetGroup(p.BackDeleteGroups[0]);
                    winProp.TargetsInfo[^1].SetGroup(p.BackDeleteGroups[1]);
                }
                if (p.BackDeleteGroups.Count >= 1) {
                    string speedDirection = isHorizontal ? Direction.Down : Direction.Right;
                    winProp.Cells[^1].SetSpeed(winProp.Settings.Speeds[1]);
                }
            }

            // Set targets' speed
            int directionsCount = Direction.GetDirections(winProp.Settings.Direction).Length;

            for (int i = 0; i < maxNLines; i++) {
                string direction = winProp.Cells[i].Direction;

                if ((!bothDirections && i < baseGroups.Count) || (bothDirections && i < (baseGroups.Count + 1) / 2)) {
                    string speedDirection = direction;
                    speedDirection =
                        direction == Direction.Horizontal ? Direction.Right :
                        direction == Direction.Vertical ? Direction.Down :
                        direction;
                    if (baseGroups.Count <= directionsCount * (bothDirections ? 2 : 1)) {
                        winProp.Cells[i].SetSpeed(winProp.Settings.SingleSpeed);
                    }
                    else if (baseGroups.Count <= 2 * directionsCount * (bothDirections ? 2 : 1)) {
                        winProp.Cells[i].SetSpeed(i / directionsCount == 0 ? winProp.Settings.Speeds[0] : winProp.Settings.Speeds[^1]);
                    }
                    else {
                        winProp.Cells[i].SetSpeed(winProp.Settings.Speeds[(i / directionsCount) % winProp.Settings.Speeds.Length]);
                    }
                }
            }
            ReformatSize();
        }

        /* Show the input elements */
        private void ResetComponents() {
            foreach (InputElement cell in winProp.Cells) {
                cell.ResetComponents();
            }
        }

        /* Main window size updated event interception */ 
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
            Width = e.NewSize.Width;
            Height = e.NewSize.Height;
            fpogs.Width = e.NewSize.Width;
            fpogs.Height = e.NewSize.Height;
            // Resize input elements
            Resize(e.NewSize.Width - 23, e.NewSize.Height - 39);
        }

        /* Resize input elements when a new screen is loaded */
        private void ReformatSize() {
            if (fullScreen) { Resize(Width - 23, Height - 39);}
            else { fullScreen = true; }
        }

        /* Change components size */
        private void Resize(double width, double height) {
            bool isHorizontal = Direction.IsHorizontal(winProp.Settings.Direction);
            bool isVertical = Direction.IsVertical(winProp.Settings.Direction);

            int baseGroupsSize = winProp.Screen.CharactersGroups.Count + winProp.Screen.DefaultGroups.Count + (!isHorizontal && !isVertical ? winProp.Screen.BackDeleteGroups.Count : 0);
            baseGroupsSize = Direction.BothDirections(winProp.Settings.Direction) ? (baseGroupsSize + 1) / 2 : baseGroupsSize;

            int actualBaseRows = (baseGroupsSize + winProp.Settings.NColumns - 1) / winProp.Settings.NColumns;
            int actualBaseCols = baseGroupsSize <= winProp.Settings.NColumns ? baseGroupsSize : winProp.Settings.NColumns;

            myGrid.RowDefinitions[0].Height = new GridLength(winProp.Settings.TextBoxHeight + (2 * winProp.Settings.TextBoxMargin));

            // Groups Cell Width and Height
            double baseCellWidth = (width - (isHorizontal && winProp.Screen.BackDeleteGroups.Count > 0 ? winProp.Settings.HorizontalBackDeleteSize : 0)) / actualBaseCols;
            double baseCellHeight = (height - myGrid.RowDefinitions[0].Height.Value - winProp.Settings.TextBoxMargin - 
                (isVertical && winProp.Screen.BackDeleteGroups.Count > 0 ? winProp.Settings.VerticalBackDeleteSize : 0)) / ((baseGroupsSize + actualBaseCols - 1) / actualBaseCols);
            
            // Back and Delete Cell Width and Height
            double backDeleteCellWidth =
                winProp.Screen.BackDeleteGroups.Count == 0 ? 0 :
                isHorizontal ? winProp.Settings.HorizontalBackDeleteSize :
                isVertical ? actualBaseCols * baseCellWidth : 0;
            double backDeleteCellHeight =
                winProp.Screen.BackDeleteGroups.Count == 0 ? 0 :
                isHorizontal ? actualBaseRows * baseCellHeight :
                isVertical ? winProp.Settings.VerticalBackDeleteSize : 0;

            // Assign size and absolute position to Cells

            for (int i = 0; i < myGrid.ColumnDefinitions.Count; i++) {
                myGrid.ColumnDefinitions[i].Width = new GridLength(
                    i < actualBaseCols 
                    ? baseCellWidth : i == myGrid.ColumnDefinitions.Count - 1 && isHorizontal && winProp.Screen.BackDeleteGroups.Count > 0 
                    ? backDeleteCellWidth : 0);
            }
            for (int i = 1; i < myGrid.RowDefinitions.Count; i++) {
                myGrid.RowDefinitions[i].Height = new GridLength(
                    i <= actualBaseRows 
                    ? baseCellHeight : i == myGrid.RowDefinitions.Count - 1 && isVertical && winProp.Screen.BackDeleteGroups.Count > 0 
                    ? backDeleteCellHeight : 0);
            }
            for (int i = 0; i < winProp.Cells.Count - (isHorizontal || isVertical ? 1 : 0); i++) {
                double canvasPosX = 
                    winProp.Settings.LineMargin +
                    (i % winProp.Settings.NColumns * baseCellWidth);
                double canvasPosY = 
                    myGrid.RowDefinitions[0].Height.Value +
                    winProp.Settings.LineMargin +
                    (i / winProp.Settings.NColumns * baseCellHeight);
                Point canvasPos = new(canvasPosX, canvasPosY);

                winProp.Cells[i].UpdateSize(Math.Max(0, baseCellWidth - (2 * winProp.Settings.LineMargin)), 
                                            Math.Max(0, baseCellHeight - (2 * winProp.Settings.LineMargin)), 
                                            canvasPos);
            }

            // Assign size and absolute position to Back and Delete cell
            if (isHorizontal || isVertical) {
                double canvasPosX = isHorizontal ? width - backDeleteCellWidth + winProp.Settings.LineMargin : winProp.Settings.LineMargin;
                double canvasPosY = isHorizontal ? myGrid.RowDefinitions[0].Height.Value + winProp.Settings.LineMargin : height - backDeleteCellHeight + winProp.Settings.LineMargin;
                Point canvasPos = new(canvasPosX, canvasPosY);
                winProp.Cells[^1].UpdateSize(Math.Max(0, backDeleteCellWidth - (2 * winProp.Settings.LineMargin)), Math.Max(0, backDeleteCellHeight), canvasPos);
            }

            double midSize =
                winProp.Screen.BackDeleteGroups.Count == 0 ? 0 :
                isHorizontal ? (width - backDeleteCellWidth) / 2 :
                isVertical ? (myGrid.RowDefinitions[0].Height.Value + (height - myGrid.RowDefinitions[0].Height.Value - backDeleteCellHeight) / 2) : 0;

            winProp.POGsEval.MidScreen = midSize;
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e) {
            // Select an input element by keyboard
            if (e.Key is >= Key.D0 and <= Key.D9) {
                choice += e.Key - Key.D0;
            }
            if (e.Key == Key.Return) {
                ChoiceSelected(int.Parse(choice));
                choice = "";
            }
            // Quit
            if (e.Key == Key.Q) {
                winProp.Close = true;
                return;
            }
            // Clear the Text Box
            if (e.Key == Key.R) {
                myText.Text = "_";
            }
        }

        /* Input element selection */
        private void ChoiceSelected(int choice) {
            string groupType = winProp.TargetsInfo[choice].Group.Type;
            string groupText = winProp.TargetsInfo[choice].Group.Text;
            if (groupText != GroupType.NoResults) {
                if (groupType == GroupType.Space) {
                    if (myText.Text.Length > 0) {
                        myText.Text = myText.Text[..^1] + " _";
                    }
                }
                else if (groupType == GroupType.Delete) {
                    if (myText.Text.Length > 1) {
                        myText.Text = myText.Text[..^2] + '_';
                    }
                }
                else if (winProp.Screen.Type == ScreenType.Prevision) {
                    myText.Text = groupText + '_';
                }
                // Going back from a character screen, the calibrations are cleared
                else if (winProp.Screen.Type == ScreenType.Characters && groupType == GroupType.Back) {
                    winProp.POGsEval.ClearCalibration();
                }
                // Single character appended to Text Box
                else if (groupText.Length == 1) {
                    myText.Text = myText.Text[..^1] + groupText + '_';
                }
                SetScreen(winProp.Screen.NextScreen(winProp.TargetsInfo[choice].Group));
            }
        }

        private void TargetTimerEvent(object sender, EventArgs e) {
            times++;
            // Move circles
            foreach (InputElement x in winProp.Cells) {
                _ = x.Move();
            }

            // With 10 position updates, 20 gaze samples are collected and activate the PoG thread
            if (times == 10) {
                activeGaze = true;
                times = 0;
            }
        }

        // Collect gaze samples
        private void GetGazeSamples() {
            // Get the first match.
            string incoming_data;

            List<Point> samples = new();

            while (!winProp.Close) {
                byte[] bytes = new byte[gp3_client.Available];
                data_feed.Read(bytes, 0, bytes.Length);
                incoming_data = Encoding.UTF8.GetString(bytes);

                // find string terminator ("\r\n") 
                if (incoming_data.IndexOf("\r\n") != -1) {
                    // only process DATA RECORDS, ie <REC .... />
                    if (incoming_data.IndexOf("<REC") != -1) {
                        // Process incoming_data string to extract FPOGX, FPOGY, etc...

                        int fpogv = int.Parse(Regex.Match(incoming_data, "FPOGV=\"(.*?)\"").Groups[1].Value, CultureInfo.InvariantCulture.NumberFormat);

                        //float fpogx = float.Parse(Regex.Match(incoming_data, "FPOGX=\"(.*?)\"").Groups[1].Value, CultureInfo.InvariantCulture.NumberFormat);
                        //float fpogy = float.Parse(Regex.Match(incoming_data, "FPOGY=\"(.*?)\"").Groups[1].Value, CultureInfo.InvariantCulture.NumberFormat);
                        //int fogx = (int)(fpogx * winWidth);
                        //int fogy = (int)(fpogy * winHeight);

                        // Code for local server use
                        int fogx = int.Parse(Regex.Match(incoming_data, "FPOGXP=\"(.*?)\"").Groups[1].Value, CultureInfo.InvariantCulture.NumberFormat);
                        int fogy = int.Parse(Regex.Match(incoming_data, "FPOGYP=\"(.*?)\"").Groups[1].Value, CultureInfo.InvariantCulture.NumberFormat);

                        Application.Current.Dispatcher.Invoke((System.Action)delegate {
                            fogx = (int)Mouse.GetPosition(myWindow).X - (winProp.Settings.TargetHeight + 1) / 2;
                            fogy = (int)Mouse.GetPosition(myWindow).Y - (winProp.Settings.TargetHeight + 1) / 2;
                        });

                        // Collect gaze samples until 20 samples are collected
                        if (fpogv != 0) {
                            samples.Add(new Point(fogx, fogy));
                        }
                        if (activeGaze) {
                            if (samples.Count is > 0 and < 45) {
                                Point pog = new(samples.Select(item => item.X).Average(), samples.Select(item => item.Y).Average());

                                //Application.Current.Dispatcher.Invoke((Action)delegate {
                                //    fpog.Fill = new SolidColorBrush(Colors.Red);
                                //    Canvas.SetLeft(fpog, p.X);
                                //    Canvas.SetTop(fpog, p.Y);
                                //});

                                int nSamples = samples.Count;
                                samples.Clear();
                                PogsReturnData choice = winProp.POGsEval.AddPOG(pog, nSamples);

                                // If the input element algorithm find a match, the circle turns green
                                if (choice.State == POGSReturnState.Result) {
                                    Application.Current.Dispatcher.Invoke((System.Action)delegate {
                                        winProp.TargetsInfo[choice.TargetSelected].Target.Fill = new SolidColorBrush(Colors.Green);
                                    });
                                    Thread.Sleep(500);
                                    Application.Current.Dispatcher.Invoke((System.Action)delegate {
                                        winProp.TargetsInfo[choice.TargetSelected].Target.Fill = winProp.TargetsInfo[choice.TargetSelected].TargetColor;
                                        ChoiceSelected(choice.TargetSelected);
                                    });
                                }
                                // Reset the PoGs and target color
                                else if (choice.State == POGSReturnState.ClearList) {
                                    Application.Current.Dispatcher.Invoke((System.Action)delegate {
                                        foreach (TargetInfo ti in winProp.TargetsInfo) {
                                            ti.Target.Fill = ti.TargetColor;
                                        }
                                    });
                                }
                                // Assign to most probable target the color red
                                else if (choice.State == POGSReturnState.ActualResult) {
                                    Application.Current.Dispatcher.Invoke((System.Action)delegate {
                                        foreach (TargetInfo ti in winProp.TargetsInfo) {
                                            ti.Target.Fill = ti.TargetColor;
                                        }
                                        winProp.TargetsInfo[choice.TargetSelected].Target.Fill = new SolidColorBrush(Colors.Red);
                                    });
                                }
                            }
                            else {
                                Application.Current.Dispatcher.Invoke((System.Action)delegate {
                                    foreach (TargetInfo ti in winProp.TargetsInfo) {
                                        ti.Target.Fill = ti.TargetColor;
                                    }
                                });
                            }

                            samples.Clear();
                            activeGaze = false;
                        }
                    }
                }
            }
        }
    }
}