using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace SmoothPursuit {
    public class InputElementSelectionAlgorithm {
        private readonly string targetsMovement;
        private double[] speeds;
        private string lastDirectionDetected;

        private readonly List<Point> pogsList;
        private readonly List<double> nGazeSamplesList;
        private List<TargetInfo> targetsInfo;

        private const int smoothPursuitCandidates = 4;
        private int smoothPursuitCandidatesCounter;
        private List<Point> startTargetsPosition;
        private List<bool> startTargetsDirection;
        private double pogsMaxDistance;

        private List<int> bestResultCounter;

        private const int verticalThreshold = 40;

        private const double horizontalLineRange = 0.8;
        private const double verticalLineRange = 1.2;

        private const double maxOrtogonalMovement = 240;
        private const double distanceInPixel = 110;
        private const int changeInDirectionMaxCounterValue = 1;

        private const int numMatchesForResult = 3;
        private const int numMatchesForIfCalibrNotPerformed = 5;

        private List<double> gazeSpeeds;
        private List<double> speedProportion;
        private List<double> gazeYProportion;
        private Point alignment;
        private double[] centers;
        private int changeInDirectionCounter;

        public double MidScreen { get; set; }


        public InputElementSelectionAlgorithm(string targetsMovement, double[] speeds) {

            this.targetsMovement = targetsMovement;
            this.speeds = speeds;

            pogsList = new();
            nGazeSamplesList = new();

            smoothPursuitCandidatesCounter = 0;
            startTargetsPosition = new();
            startTargetsDirection = new();
            pogsMaxDistance = 0;

            bestResultCounter = new();
            lastDirectionDetected = "";

            gazeSpeeds = new();
            speedProportion = new();
            gazeYProportion = new();

            alignment = new(0, 0);
            MidScreen = 0;

            centers = new double[2] { -1, -1 };

            changeInDirectionCounter = 0;
        }

        public PogsReturnData AddPOG(Point p, int npoints) {
            if (smoothPursuitCandidatesCounter < smoothPursuitCandidates) {
                if (pogsList.Count >= 2) {
                    // Speed between 2 PoGs
                    double pogsDistance = Math.Sqrt(Math.Pow(p.Y - pogsList[^1].Y, 2) + Math.Pow(p.X - pogsList[^1].X, 2));
                    // Differences between consecutive distances in the axis
                    double distXInRange = Math.Abs(Math.Abs(p.X - pogsList[^1].X) - Math.Abs(pogsList[^1].X - pogsList[^2].X));
                    double distYInRange = Math.Abs(Math.Abs(p.Y - pogsList[^1].Y) - Math.Abs(pogsList[^1].Y - pogsList[^2].Y));

                    bool distXThreshold = distXInRange < Math.Abs(((pogsDistance * 75) - 135.75) * 0.093) + 27;
                    bool distYThreshold = distYInRange < verticalThreshold;
                    // If conditions are satisfied increase good candidates counter else reset PoGs list
                    if (distXThreshold && distYThreshold) {
                        smoothPursuitCandidatesCounter++;
                        // If the number of good candidates is reached take trace of targets' position and direction
                        if (smoothPursuitCandidatesCounter == smoothPursuitCandidates) {
                            pogsList.Clear();
                            foreach (TargetInfo ti in targetsInfo) {
                                startTargetsPosition.Add(ti.AbsolutePosition());
                                startTargetsDirection.Add(ti.IsDirect);
                            }
                            pogsList.Add(p);
                            nGazeSamplesList.Add(npoints);
                            return new(POGSReturnState.MatchDetectionPhase);
                        }
                    }
                    else {
                        Reset();
                        pogsList.Add(p);
                        return new(POGSReturnState.ClearList);
                    }
                }
                pogsList.Add(p);
                return new(POGSReturnState.AddPogToList);
            }
            else {
                // Calculate the speed
                double endEyePositionX = p.X - pogsList[0].X;
                double endEyePositionY = p.Y - pogsList[0].Y;

                double totNPoints = 0;
                for (int i = 1; i < nGazeSamplesList.Count; i++) {
                    totNPoints += (nGazeSamplesList[i] + nGazeSamplesList[i - 1]) / 2;
                }
                totNPoints += (npoints + nGazeSamplesList[^1]) / 2;

                double gazeSpeedX = endEyePositionX * 2 / totNPoints;
                double gazeSpeedY = endEyePositionY * 2 / totNPoints;

                // Calculate the slope of the Least-Squares Regression Line that crosses the actual point and the first one
                double slope = LeastSquareSlope(p);
                // Find the movement direction
                string direction = "";
                if (slope <= horizontalLineRange) {
                    direction = endEyePositionX > 0 ? Direction.Right : Direction.Left;
                }
                else if (slope > verticalLineRange) {
                    direction = endEyePositionY < 0 ? Direction.Up : Direction.Down;
                }

                // If direction is detected continue, else collect another PoG
                if (direction != "") {
                    if (lastDirectionDetected == "") {
                        lastDirectionDetected = direction;
                    }
                    if (lastDirectionDetected == direction) {
                        double gazeSpeed = direction == Direction.Right || direction == Direction.Left ? gazeSpeedX : gazeSpeedY;
                        double actualTotDistance = Math.Max(Math.Abs(endEyePositionX), Math.Abs(endEyePositionY));

                        // Identify orthogonal movements
                        if (Math.Min(Math.Abs(endEyePositionX), Math.Abs(endEyePositionY)) > maxOrtogonalMovement) {
                            Reset();
                            pogsList.Add(p);
                            return new(POGSReturnState.ClearList);
                        }

                        // Take trace of max distance between the first PoG and the actual one
                        // If the current distance does not exceed the pogsMaxDistance, no operation is performed
                        // If does not exceed the pogsMaxDistance two times identify a change in direction
                        if (actualTotDistance > pogsMaxDistance) {
                            changeInDirectionCounter = 0;
                            pogsMaxDistance = actualTotDistance;

                            List<double> diffResults = new();
                            List<double> sortDiff = new();
                            int bestAligned = -1;
                            double estimatedGazePosition = -1;

                            // Calculate the actual avarage gaze position
                            // If IsHorizontal is true, it is based on the center of the screen and vertical proportion

                            if (Direction.IsHorizontal(targetsMovement)) {
                                estimatedGazePosition = speedProportion.Count > 0 ?
                                    (gazeYProportion.Count > 0 ? gazeYProportion.Average() : 1) * 
                                    (pogsList.Select(t => t.Y).Average() - centers[1]) + centers[0] :
                                    pogsList.Select(t => t.Y).Average();
                            }
                            else {
                                estimatedGazePosition = pogsList.Select(t => t.X).Average();
                            }

                            // For each target with same direction, calculate the difference in positions
                            for (int i = 0; i < targetsInfo.Count; i++) {
                                if (targetsInfo[i].Group == null || targetsInfo[i].IsDirect != startTargetsDirection[i]) {
                                    diffResults.Add(3000);
                                }
                                else if (direction == targetsInfo[i].ActualDirection) {
                                    if (Direction.IsHorizontal(targetsMovement) && (direction == Direction.Right || direction == Direction.Left)) {
                                        diffResults.Add(Math.Abs(estimatedGazePosition - targetsInfo[i].AbsolutePosition().Y));
                                    }
                                    else if (!Direction.IsHorizontal(targetsMovement)) {
                                        diffResults.Add(Math.Abs(estimatedGazePosition - targetsInfo[i].AbsolutePosition().X));
                                    }
                                    else {
                                        diffResults.Add(3000);
                                    }
                                }
                                else {
                                    diffResults.Add(3000);
                                }

                                // Get the closest target
                                sortDiff.AddRange(diffResults);
                                sortDiff.Sort();
                                _ = sortDiff.RemoveAll(item => item == 3000);
                                if (sortDiff.Count > 0) {
                                    bestAligned = diffResults.IndexOf(sortDiff[0]);
                                }
                            }

                            // Speed selection
                            diffResults.Clear();
                            for (int i = 0; i < targetsInfo.Count; i++) {
                                // Hidden targets not considered
                                if (targetsInfo[i].Group == null) {
                                    diffResults.Add(3000);
                                }

                                // If target change direction remove it from possible results
                                else if (targetsInfo[i].IsDirect != startTargetsDirection[i]) {
                                    _ = bestResultCounter.RemoveAll(item => item == i);
                                    diffResults.Add(3000);
                                    startTargetsDirection[i] = targetsInfo[i].IsDirect;
                                }

                                // If the direction of the target matches the direction of the gaze, calculate the distance between their speeds and get the best one
                                else if (direction == targetsInfo[i].ActualDirection) {
                                    double gazeSpeedCorrected = gazeSpeed * (speedProportion.Count > 0 ? speedProportion.Average() : 1);
                                    double tempDiff = Math.Abs((targetsInfo[i].Speed * (targetsInfo[i].IsDirect ? 1 : -1)) - gazeSpeedCorrected);
                                    // Check if the user looks at the labels or if he wants to select Delete/Back action
                                    if (Direction.IsHorizontal(targetsMovement) && (direction == Direction.Up || direction == Direction.Down)) {
                                        diffResults.Add((Math.Abs(pogsList[0].X + alignment.X - startTargetsPosition[i].X) < Math.Abs(pogsList[0].X + alignment.X - MidScreen)) ? 0 : 3000);
                                    }
                                    else if (Direction.IsVertical(targetsMovement) && (direction == Direction.Right || direction == Direction.Left)) {
                                        diffResults.Add((Math.Abs(pogsList[0].Y + alignment.Y - startTargetsPosition[i].Y) < Math.Abs(pogsList[0].Y + alignment.Y - MidScreen)) ? 0 : 3000);
                                    }
                                    else {
                                        // With two speeds check the speed of the four lines closest to the estimated gaze position
                                        if ((speeds.Length == 1 && i == bestAligned) || (speeds.Length == 2 && i - bestAligned is >= -1 and <= 2) || (speeds.Length >= 3 && Math.Abs(i - bestAligned) <= 2)) {
                                            diffResults.Add(tempDiff);
                                        }
                                        else {
                                            diffResults.Add(3000);
                                        }
                                    }
                                }
                                else {
                                    diffResults.Add(3000);
                                }
                            }

                            // Get the best result
                            sortDiff.Clear();
                            sortDiff.AddRange(diffResults);
                            sortDiff.Sort();
                            _ = sortDiff.RemoveAll(item => item == 3000);

                            if (sortDiff.Count == 0) {
                                Reset();
                                pogsList.Add(p);
                                return new(POGSReturnState.ClearList);
                            }

                            // If starting calibration is not performed store the result
                            // Back/Delete selection are easy to perform
                            if (sortDiff.Count > 0 && 
                                (speedProportion.Count == 0 || 
                                (Direction.IsHorizontal(targetsMovement) && (direction == Direction.Up || direction == Direction.Down)) ||
                                (Direction.IsVertical(targetsMovement) && (direction == Direction.Right || direction == Direction.Left)) ||
                                (speedProportion.Count > 0))) {
                                int result = diffResults.IndexOf(sortDiff[0]);
                                PogsReturnData returnData = new(
                                    state: POGSReturnState.Result,
                                    targetSelected: result
                                );
                                gazeSpeeds.Add(gazeSpeed);

                                bestResultCounter.Add(result);
                                List<int> maxBestList = bestResultCounter
                                    .Where(x => x != result)
                                    .GroupBy(p => p)
                                    .ToDictionary(p => p.Key, q => q.Count())
                                    .Select(x => x.Value)
                                    .ToList();
                                int maxBest = maxBestList.Count > 0 ? maxBestList.Max() : 0;

                                // Select the target best result counter exceeds the second best counter of a number equal to numMatchesForResult
                                if (bestResultCounter.Count(i => i == result) - maxBest >= (speedProportion.Count == 0 ? numMatchesForIfCalibrNotPerformed : numMatchesForResult)) {
                                    
                                    // Add new speed proportion
                                    if (speedProportion.Count == 0) {
                                        if (Direction.IsHorizontal(targetsMovement) && (direction == Direction.Right || direction == Direction.Left)) {
                                            centers = new double[2] { startTargetsPosition[result].Y, pogsList.Select(t => t.Y).Average() };
                                            alignment = new(startTargetsPosition[result].X - pogsList[0].X, startTargetsPosition[result].Y - pogsList[0].Y);
                                        }
                                    }
                                    else if (Direction.IsHorizontal(targetsMovement) && (direction == Direction.Right || direction == Direction.Left)) {
                                        double centerTargetDistance = Math.Abs(pogsList.Select(t => t.Y).Average() - centers[1]);
                                        if (centerTargetDistance != 0) {
                                            gazeYProportion.Add(Math.Abs(startTargetsPosition[result].Y - centers[0]) / centerTargetDistance);
                                        }
                                    }
                                    speedProportion.Add(Math.Abs(targetsInfo[result].Speed / gazeSpeeds.Average()));

                                    Reset();
                                    return returnData;
                                }

                                pogsList.Add(p);
                                nGazeSamplesList.Add(npoints);
                                returnData.State = POGSReturnState.ActualResult;
                                return returnData;
                            }
                            pogsList.Add(p);
                            nGazeSamplesList.Add(npoints);
                        }
                        else {
                            changeInDirectionCounter++;
                            if (changeInDirectionCounter == changeInDirectionMaxCounterValue) {
                                Reset();
                                pogsList.Add(p);
                                return new(POGSReturnState.ClearList);
                            }
                        }
                    }
                    else {
                        // Direction changed, reset the points
                        Reset();
                        pogsList.Add(p);
                        return new(POGSReturnState.ClearList);
                    }
                }
                else {
                    // Direction not detected
                    return new(POGSReturnState.Continue);
                }
            }
            return new(POGSReturnState.Continue);
        }

        public void SetTargetsInfo(List<TargetInfo> ti) {
            targetsInfo = ti;
        }
        private void Reset() {
            pogsList.Clear();
            nGazeSamplesList.Clear();
            smoothPursuitCandidatesCounter = 0;
            startTargetsPosition.Clear();
            startTargetsDirection.Clear();
            pogsMaxDistance = 0;
            bestResultCounter.Clear();
            lastDirectionDetected = "";
            gazeSpeeds.Clear();
            changeInDirectionCounter = 0;
        }

        public void ClearCalibration() {
            gazeSpeeds.Clear();
            speedProportion = speedProportion.Take(2).ToList();
            gazeYProportion = gazeYProportion.Take(2).ToList();
        }

        private double LeastSquareSlope(Point newPoint) {
            List<Point> tempPogsList = new();
            tempPogsList.AddRange(pogsList);
            tempPogsList.Add(newPoint);
            double xmean = tempPogsList.Select(item => item.X).Average();
            double ymean = tempPogsList.Select(item => item.Y).Average();

            double num = 0;
            double denom = 0;
            foreach (Point p in tempPogsList) {
                double xdist = p.X - xmean;
                double ydist = p.Y - ymean;
                num += (xdist * ydist);
                denom += (xdist * xdist);
            }
            if (denom == 0) denom = 0.01;
            return Math.Abs(num / denom);
        }
    }
}
