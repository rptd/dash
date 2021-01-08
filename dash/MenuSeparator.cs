using System;
using System.Collections.Generic;
using System.Text;

namespace dash
{
    class MenuSeparator : IMenuItem
    {
        public string Name { get; } = "".PadRight(Program.MenuWidth - 2, '─');

        public Action Action { get; } = () => { };

        public bool Selectable { get; } = false;
    }
}
