using System;
using System.Collections.Generic;
using System.Text;

namespace dash
{
    interface IMenuItem
    {
        string Name { get; }

        Action? Action { get; }

        bool Selectable { get; }
    }
}
