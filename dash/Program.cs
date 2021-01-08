using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace dash
{
    class Program
    {
        public const int MenuWidth = 20;

        static Thread? RenderThread;
        static ConcurrentQueue<Action> RenderQueue = new ConcurrentQueue<Action>();

        static Timer? ClockTimer;

        static Config Config = new Config();

        static List<(string, List<IMenuItem>)> TopMenu = new List<(string, List<IMenuItem>)>
        {
            ("@", new List<IMenuItem>
            {
                new MenuItem("About This Device", () => { }),
                new MenuSeparator(),
                new MenuItem("System Preferences", () => { }),
                new MenuItem("Command-line Shell", () => { }),
                new MenuSeparator(),
                new MenuItem("Restart", () => { }),
                new MenuItem("Shut Down", () => { })
            } ),
            ( "Applications", new List<IMenuItem>
            {
                new MenuItem("File Explorer", OpenProgram("ranger")),
                new MenuItem("Text Editor", OpenProgram("vim")),
                new MenuSeparator(),
                new MenuItem("Web Browser", OpenProgram("links")),
                new MenuItem("Email", OpenProgram("mutt")),
                new MenuItem("Calendar", OpenProgram("calcurse")),
                new MenuSeparator(),
                new MenuItem("Spreadsheet", OpenProgram("scim")),
                new MenuSeparator(),
                new MenuItem("Music Player", OpenProgram("ncmpcpp")),
            } ),
            ( "Games", new List<IMenuItem>
            {
                new MenuItem("Nethack", OpenProgram("nethack")),
                new MenuItem("ASCII Patrol", OpenProgram("asciipat")),
            } ),
            ( "System", new List<IMenuItem>
            {
                new MenuItem("System Monitor", OpenProgram("htop")),
                new MenuItem("I/O Monitor", OpenProgram("iotop")),
                new MenuSeparator(),
                new MenuItem("System Report", OpenProgram("system-report")),
                new MenuItem("System Update", OpenProgram("system-update")),
            } ),
            ("Help", new List<IMenuItem>
            {
                new MenuItem("RPTD System Manual", OpenProgram("rptd-manual")),
                //new MenuSeparator(),
                new MenuItem("UNIX Manual", OpenProgram("rptd-unix-manual")),
            })
        };
        static int TopMenuSelector = 0;
        static int PopupMenuSelector = 0;
        static (int, int)? VisibleMenuLocation;
        static List<IMenuItem> PopupMenu { get => TopMenu[TopMenuSelector].Item2; }

        #region Entry points

        static void Main(string[] args)
        {
            // set up the render thread
            RenderThread = new Thread(RenderMain);
            RenderThread.Start();
            RenderQueue.Enqueue(DrawTopMenu);

            // set up the clock timer
            ClockTimer = new Timer(stateInfo =>
            {
                RenderQueue.Enqueue(DrawTopTime);
            }, null, 0, 100);

            // always check the key
            while (true)
            {
                var ch = Console.ReadKey(true);
                if (ch.Key == ConsoleKey.LeftArrow)
                {
                    if (TopMenuSelector > 0)
                    {
                        RenderQueue.Enqueue(ClearMenu(TopMenu[TopMenuSelector].Item2));
                        TopMenuSelector--;

                        // if the cursor lands on an unselectable item, go back
                        while (PopupMenuSelector >= PopupMenu.Count || !PopupMenu[PopupMenuSelector].Selectable)
                        {
                            PopupMenuSelector--;
                        }
                    }
                    RenderQueue.Enqueue(DrawTopMenu);
                }
                else if (ch.Key == ConsoleKey.RightArrow)
                {
                    if (TopMenuSelector < TopMenu.Count - 1)
                    {
                        RenderQueue.Enqueue(ClearMenu(TopMenu[TopMenuSelector].Item2));
                        TopMenuSelector++;

                        // if the cursor lands on an unselectable item, go back
                        while (PopupMenuSelector >= PopupMenu.Count || !PopupMenu[PopupMenuSelector].Selectable)
                        {
                            PopupMenuSelector--;
                        }
                    }
                    RenderQueue.Enqueue(DrawTopMenu);
                }
                else if (ch.Key == ConsoleKey.UpArrow)
                {
                    if (PopupMenuSelector > 0)
                    {
                        // keep going over unselectable items
                        while (!PopupMenu[--PopupMenuSelector].Selectable) ;

                        (var x, var y) = VisibleMenuLocation.Value;
                        RenderQueue.Enqueue(DrawMenuGenerator(x, y, PopupMenu));
                    }
                }
                else if (ch.Key == ConsoleKey.DownArrow)
                {
                    if (PopupMenuSelector < PopupMenu.Count - 1)
                    {
                        // keep going over unselectable items
                        while (!PopupMenu[++PopupMenuSelector].Selectable) ;

                        (var x, var y) = VisibleMenuLocation.Value;
                        RenderQueue.Enqueue(DrawMenuGenerator(x, y, PopupMenu));
                    }
                }
                else if (ch.Key == ConsoleKey.Enter)
                {
                    PopupMenu[PopupMenuSelector].Action.Invoke();
                }
            }
        }

        static void RenderMain()
        {
            Console.SetWindowSize(80, 24);
            Console.SetBufferSize(80, 24);
            Console.BackgroundColor = Config.BackgroundColor;
            Console.ForegroundColor = Config.ForegroundColor;
            Console.Clear();

            while (true)
            {
                while (RenderQueue.Count > 0)
                {
                    if (RenderQueue.TryDequeue(out var method))
                    {
                        method.Invoke();
                    }
                }
            }
        }

        #endregion

        #region Drawing Functions

        static void DrawTopMenu()
        {
            int i;

            Console.BackgroundColor = Config.TopMenuBackground;
            Console.ForegroundColor = Config.TopMenuForeground;

            // fill in the chars at the top
            var sb = new StringBuilder();
            for (i = 0; i < Console.WindowWidth; i++)
            {
                sb.Append(" ");
            }
            Console.SetCursorPosition(0, 0);
            Console.Write(sb.ToString());

            // add the menu items
            Console.SetCursorPosition(1, 0);
            i = 0;
            foreach (var (name, items) in TopMenu)
            {
                if (i == TopMenuSelector)
                {
                    var x = Console.CursorLeft;
                    VisibleMenuLocation = (x, 1);
                    DrawMenu(x, 1, items);
                    Console.CursorLeft = x;

                    Console.BackgroundColor = Config.TopMenuSelectedBackground;
                    Console.ForegroundColor = Config.TopMenuSelectedForeground;
                }

                Console.CursorTop = 0;
                Console.Write($" {name} ");

                if (i == TopMenuSelector)
                {
                    Console.BackgroundColor = Config.TopMenuBackground;
                    Console.ForegroundColor = Config.TopMenuForeground;
                }

                i++;
            }

            DrawTopTime();

            // clean up
            Console.WriteLine();
            Console.ResetColor();
        }

        static Action DrawMenuGenerator(int x, int y, ICollection<IMenuItem> menu) => () =>
            {
                DrawMenu(x, y, menu);
            };

        static void DrawMenu(int x, int y, ICollection<IMenuItem> menu)
        {
            Console.BackgroundColor = Config.PopupMenuBackground;
            Console.ForegroundColor = Config.BackgroundColor;
            Console.SetCursorPosition(x, y);
            Console.WriteLine("".PadRight(MenuWidth, '▀'));

            var i = 0;
            Console.BackgroundColor = Config.PopupMenuBackground;
            Console.ForegroundColor = Config.PopupMenuForeground;
            foreach (var item in menu)
            {
                Console.CursorLeft = x;

                if (i == PopupMenuSelector)
                {
                    Console.BackgroundColor = Config.PopupMenuSelectedBackground;
                    Console.ForegroundColor = Config.PopupMenuSelectedForeground;
                }
                Console.WriteLine($" {item.Name} ".PadRight(MenuWidth));
                if (i == PopupMenuSelector)
                {
                    Console.BackgroundColor = Config.PopupMenuBackground;
                    Console.ForegroundColor = Config.PopupMenuForeground;
                }

                i++;
            }

            Console.CursorLeft = x;
            Console.BackgroundColor = Config.PopupMenuBackground;
            Console.ForegroundColor = Config.BackgroundColor;
            Console.WriteLine("".PadRight(MenuWidth, '▄'));
        }

        static Action ClearMenu(ICollection<IMenuItem> menu)
        {
            return () =>
            {
                if (VisibleMenuLocation == null) return;
                (int x, int y) = VisibleMenuLocation.Value;
                Console.SetCursorPosition(x, y);

                // clear a line for every item in the list
                Console.BackgroundColor = Config.BackgroundColor;
                Console.ForegroundColor = Config.ForegroundColor;
                foreach (var _ in menu)
                {
                    Console.CursorLeft = x;
                    Console.WriteLine("".PadRight(MenuWidth));
                }

                // there are two more lines to clear
                Console.CursorLeft = x;
                Console.WriteLine("".PadRight(MenuWidth));
                Console.CursorLeft = x;
                Console.WriteLine("".PadRight(MenuWidth));
            };
        }

        static void DrawTopTime()
        {
            Console.BackgroundColor = Config.TopMenuBackground;
            Console.ForegroundColor = Config.TopMenuForeground;

            var timestr = DateTime.Now.ToString("t");
            Console.SetCursorPosition(Console.BufferWidth - timestr.Length - 2, 0);
            Console.Write(timestr);
        }

        #endregion

        #region Action Handlers

        static Action OpenProgram(string name)
        {
            return () =>
            {
                Process.Start(name);
            };
        }

        #endregion
    }
}
