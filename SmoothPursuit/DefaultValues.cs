using System;
using System.Linq;

namespace SmoothPursuit {

    public sealed class Mode {
        public static string Debug => "debug";
        public static string DebugWrite => "analysis";
        public static string Build => "build";
        public static string WritePOGs => "write";
        public static string Analysis => "analysis";
        public static string WriteTest => "writeTest";
    }

    public sealed class GroupType {
        public const string Start = "START";
        public const string Letters = "A B C";
        public const string Numbers = "1 2 3";
        public const string Symbols = "? @ (";
        public const string Characters = "CHAR";
        public const string Lowercase = "lowercase";
        public const string Uppercase = "UPPERCASE";
        public const string Prevision = "PREVISION";
        public const string Space = "SPACE";
        public const string Delete = "DELETE";
        public const string Accents = "ACCENTED";
        public const string Back = "BACK";
        public const string NoResults = "NO WORDS FOUND";
        public const string Words = "WORDS";
    }

    public sealed class ScreenType {
        public static string Instructions => "instructionsScreen";
        public static string Home => "homeScreen";
        public static string Letters => "lettersScreen";
        public static string Numbers => "numScreen";
        public static string NumSyms => "numSymScreen";
        public static string Symbols => "symScreen";
        public static string Characters => "charScreen";
        public static string Prevision => "previsionScreen";
    }

    public static class Direction {
        public static string Left => "left";
        public static string Right => "right";
        public static string Down => "down";
        public static string Up => "up";
        public static string Horizontal => "horizontal";
        public static string Vertical => "vertical";
        public static string CAxis => "caxis";
        public static string DAxis => "daxis";
        public static string Axis => "axis";
        public static string HBoth => "hboth";
        public static string VBoth => "vboth";
        public static string CAxisBoth => "caxisboth";

        public static string[] GetDirections(string direction) {
           if (direction == CAxis) {
                return new string[] { Right, Down, Left, Up };
            }
            else if (direction == HBoth) {
                return new string[] { Horizontal };
            }
            else if (direction == VBoth) {
                return new string[] { Vertical };
            }
            else if (direction == CAxisBoth) {
                return new string[] { Horizontal, Vertical };
            }
            else if (direction == Horizontal) {
                return new string[] { Right, Left };
            }
            else if (direction == Vertical) {
                return new string[] { Down, Up };
            }
            else {
                return new string[] { direction };
            }
        }

        public static bool BothDirections(string direction) {
            return new[] { HBoth, VBoth, CAxisBoth }.Contains(direction);
        }
        public static bool IsHorizontal(string direction) {
            return new[] { Left, Right, Horizontal, HBoth}.Contains(direction);
        }
        public static bool IsVertical(string direction) {
            return new[] { Up, Down, Vertical, VBoth }.Contains(direction);
        }
    }

    public static class POGSReturnState {
        public static string AddPogToList => "addPogToList";
        public static string ClearList => "clearList";
        public static string MatchDetectionPhase => "startTrace";
        public static string Continue => "continue";
        public static string ActualResult => "actualResult";
        public static string Result => "result";
    }
}
