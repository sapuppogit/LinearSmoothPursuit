using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SmoothPursuit;

namespace SmoothPursuit {

    interface IScreen {
        string Type { get; set; }
        int NLines { get; }
        bool IsHorizontalOrVertical { get; }
        bool Predictor { get; }
        char[] Characters { get; set; }
        List<Action> CharactersGroups { get; set; }
        List<Action> DefaultGroups { get; set; }
        List<Action> BackDeleteGroups { get; set; }
        Screen BackPage { get; }
        Screen NextScreen(Action group);
        void SetCharactersGroups();
    }

    public abstract class Screen : IScreen {
        public Screen(int nLines, bool isHorizontalOrVertical, bool predictor) {
            NLines = nLines;
            IsHorizontalOrVertical = isHorizontalOrVertical;
            Predictor = predictor;
        }
        public Screen(Screen prevPage) {
            BackPage = prevPage;
            NLines = prevPage.NLines;
            IsHorizontalOrVertical = prevPage.IsHorizontalOrVertical;
            Predictor = prevPage.Predictor;
        }
        public string Type { get; set; }
        public int NLines { get; }
        public bool IsHorizontalOrVertical { get; }
        public bool Predictor { get; }
        public char[] Characters { get; set; }
        public List<Action> CharactersGroups { get; set; }
        public List<Action> DefaultGroups { get; set; }
        public List<Action> BackDeleteGroups { get; set; }
        public Screen BackPage { get; }
        public abstract Screen NextScreen(Action group);

        public void SetCharactersGroups() {
            List<Action> tempGroups = new();
            int letterGroups = NLines - DefaultGroups.Count - (IsHorizontalOrVertical ? 0 : BackDeleteGroups.Count);

            if (Characters.Length <= letterGroups) {
                tempGroups = Characters.Select(c => new Action(GroupType.Characters, c.ToString())).ToList();
            }
            else {
                int ceiling = (int)Math.Ceiling((double)Characters.Length / letterGroups);
                int floor = (int)Math.Floor((double)Characters.Length / letterGroups);
                int letterIndex = 0;
                for (int i = 0; i < letterGroups; i++) {
                    char[] skipped = Characters.Skip(letterIndex).ToArray();
                    int takeVal = i < letterGroups - (Characters.Length % letterGroups) ? floor : ceiling;
                    tempGroups.Add(new Action(GroupType.Characters, new string(skipped.Take(takeVal).ToArray())));
                    letterIndex += takeVal;
                }
            }
            CharactersGroups = tempGroups;
        }
    }

    public class InstructionsScreen : Screen {
        public InstructionsScreen(int nLines, bool isHorizontalorVertical, bool predictor) : base(nLines, isHorizontalorVertical, predictor) {
            Type = ScreenType.Instructions;
            CharactersGroups = new();
            DefaultGroups = new() { new(GroupType.Start) };
            BackDeleteGroups = new();
        }
        public override Screen NextScreen(Action group) {
            return new HomeScreen(this);
        }
    }

    public class HomeScreen : Screen {
        public bool LowerCase { get; set; }
        public HomeScreen(Screen prevPage) : base(prevPage) {
            Type = ScreenType.Home;
            CharactersGroups = new();
            DefaultGroups = new() { new(GroupType.Letters), new(GroupType.Numbers) };
            BackDeleteGroups = new();
        }

        public override Screen NextScreen(Action group) {
            if (group.Type == GroupType.Letters) {
                return new LettersScreen(this);
            }
            else {
                return new NumbersScreen(this);
            }
        }
    }

    public class LettersScreen : Screen {
        readonly string letters;
        private readonly HomeScreen prevPage;
        public LettersScreen(HomeScreen prevPage) : base(prevPage) {
            Type = ScreenType.Letters;
            letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string accented = "àèéìòù";
            this.prevPage = prevPage;
            if (prevPage.LowerCase) {
                letters = letters.ToLower();
            }
            else {
                accented = accented.ToUpper();
            }
            Characters = letters.ToCharArray();
            DefaultGroups = new() {
                new(GroupType.Characters, accented),
                new(prevPage.LowerCase ? GroupType.Uppercase : GroupType.Lowercase),
                new(GroupType.Space),
                new(GroupType.Symbols)
            };
            Trace.WriteLine(Predictor);
            if (Predictor) { DefaultGroups.Insert(DefaultGroups.Count - 1, new(GroupType.Prevision)); }
            BackDeleteGroups = new() { new(GroupType.Back), new(GroupType.Delete) };
            SetCharactersGroups();
        }
        public override Screen NextScreen(Action group) {
            if (group.Type == GroupType.Lowercase || group.Type == GroupType.Uppercase) {
                prevPage.LowerCase = !prevPage.LowerCase;
                string accented = "àèéìòù";
                if (prevPage.LowerCase) {
                    Characters = letters.ToLower().ToCharArray();
                    DefaultGroups[1] = new(GroupType.Uppercase);
                }
                else {
                    Characters = letters.ToUpper().ToCharArray();
                    accented = accented.ToUpper();
                    DefaultGroups[1] = new(GroupType.Lowercase);
                }
                DefaultGroups[0] = new(GroupType.Characters, accented);
                SetCharactersGroups();
                return this;
            }
            else if (group.Type == GroupType.Back) {
                return BackPage;
            }
            if (group.Type == GroupType.Space) {
                return this;
            }
            else if (group.Type == GroupType.Delete) {
                return this;
            }
            else if (group.Type == GroupType.Characters && group.Text.Length == 1) {
                return this;
            }
            else if (group.Type == GroupType.Prevision) {
                return new PrevisionScreen(this, group.Text);
            }
            else if (group.Type == GroupType.Symbols) {
                return new SymbolsScreen(this);
            }
            else {
                return new CharacterScreen(this, group.Text.ToCharArray());
            }
        }
    }

    public class SymbolsScreen : Screen {
        public SymbolsScreen(Screen prevPage) : base(prevPage) {
            Type = ScreenType.Symbols;
            CharactersGroups = new();
            DefaultGroups = new() {
                new(GroupType.Characters, ".:,;"),
                new(GroupType.Characters, "?!'\""),
                new(GroupType.Characters, "+-*="),
                new(GroupType.Characters, "@%<>"),
                new(GroupType.Characters, "()[]"),
                new(GroupType.Characters, "/\\{}"),
                new(GroupType.Characters, "€$£&"),
                new(GroupType.Characters, "|~^_"),
            };
            BackDeleteGroups = new() { new(GroupType.Back), new(GroupType.Delete) };
        }
        public override Screen NextScreen(Action group) {
            if (group.Type == GroupType.Back) {
                return BackPage;
            }
            else if (group.Type == GroupType.Delete) {
                return this;
            }
            else if (group.Type == GroupType.Characters && group.Text.Length == 1) {
                return this;
            }
            else {
                return new CharacterScreen(this, group.Text.ToCharArray());
            }
        }
    }

    public class NumbersScreen : Screen {
        public NumbersScreen(Screen prevPage) : base(prevPage) {
            Type = ScreenType.Numbers;
            Characters = "0123456789".ToCharArray();
            DefaultGroups = new() { new(GroupType.Symbols) };
            BackDeleteGroups = new() { new(GroupType.Back), new(GroupType.Delete)};
            SetCharactersGroups();
        }
        public override Screen NextScreen(Action group) {
            if (group.Type == GroupType.Back) {
                return BackPage;
            }
            else if (group.Type == GroupType.Delete) {
                return this;
            }
            else if (group.Type == GroupType.Symbols) {
                return new SymbolsScreen(this);
            }
            else if (group.Type == GroupType.Characters && group.Text.Length == 1) {
                return this;
            }
            else {
                return new CharacterScreen(this, group.Text.ToCharArray());
            }
        }
    }

    public class CharacterScreen : Screen {
        public CharacterScreen(Screen prevPage, char[] characters) : base(prevPage) {
            Type = ScreenType.Characters;
            Characters = characters;
            DefaultGroups = new();
            BackDeleteGroups = new() { new(GroupType.Back) };
            SetCharactersGroups();
        }

        public override Screen NextScreen(Action group) {
            if (group.Type == GroupType.Back) {
                return BackPage;
            }
            else if (group.Type == GroupType.Characters && group.Text.Length == 1) {
                return BackPage;
            }
            else {
                return new CharacterScreen(BackPage, group.Text.ToCharArray());
            }
        }
    }

    public class PrevisionScreen : Screen {
        public PrevisionScreen(Screen prevPage, string words) : base(prevPage) {
            Type = ScreenType.Prevision;
            CharactersGroups = new();
            DefaultGroups = words.Split(' ').Select(x => new Action(GroupType.Prevision, x)).ToList();
            BackDeleteGroups = new() { new(GroupType.Back) };
        }
        public override Screen NextScreen(Action group) {
            return BackPage;
        }
    }
}
