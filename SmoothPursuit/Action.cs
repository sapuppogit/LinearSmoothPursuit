using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoothPursuit {
    public class Action {
        public string Type { get; set; }
        public string Text { get; set; }
        public Action(string type) {
            Type = type;
            Text = type;
        }
        public Action(string type, string text) {
            Type = type;
            Text = text;
        }
    }
}
