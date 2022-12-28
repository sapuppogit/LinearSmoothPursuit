using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SmoothPursuit {
    public class PogsReturnData {
        public string State { get; set; }
        public int TargetSelected { get; }


        public PogsReturnData(string state) {
            State = state;
        }

        public PogsReturnData(string state, int targetSelected) {
            State = state;
            TargetSelected = targetSelected;
        }
    }

    public class TestData {
        public string Phrase { get; set; }
        public string Time { get; set; }
        public int Errors { get; set; }
        public double WPM { get; set; }
        public string TPW { get; set; }
        public double KSPC { get; set; }

        public TestData (
            string phrase,
            string time,
            int errors,
            double wpm,
            string timePerWord,
            double kspc
            ) {
            Phrase = phrase;
            Time = time;
            Errors = errors;
            WPM = wpm;
            TPW = timePerWord;
            KSPC = kspc;
        }
    }
}
