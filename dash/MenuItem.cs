using System;
using System.Collections.Generic;
using System.Text;

namespace dash
{
    public class MenuItem : IMenuItem
    {
        public string Name { get; set; }

        public Action Action { get; set; }

        public bool Selectable { get; } = true;

        public MenuItem(string name, Action action)
        {
            Name = name;
            Action = action;
        }
    }
}
