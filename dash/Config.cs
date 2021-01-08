using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;

namespace dash
{
    public class Config
    {
        public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor ForegroundColor { get; set; } = ConsoleColor.Gray;
        public ConsoleColor TopMenuBackground { get; set; } = ConsoleColor.White;
        public ConsoleColor TopMenuForeground { get; set; } = ConsoleColor.Black;
        public ConsoleColor TopMenuSelectedBackground { get; set; } = ConsoleColor.DarkBlue;
        public ConsoleColor TopMenuSelectedForeground { get; set; } = ConsoleColor.White;
        public ConsoleColor PopupMenuBackground { get; set; } = ConsoleColor.Gray;
        public ConsoleColor PopupMenuForeground { get; set; } = ConsoleColor.Black;
        public ConsoleColor PopupMenuSelectedBackground { get; set; } = ConsoleColor.DarkBlue;
        public ConsoleColor PopupMenuSelectedForeground { get; set; } = ConsoleColor.White;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public void Save(string filename) => File.WriteAllText(filename, ToJson());

        public static Config Load(string filename) => Parse(File.ReadAllText(filename));

        public string ToJson() => JsonSerializer.Serialize<Config>(this, _jsonOptions);

        public static Config Parse(string str) => JsonSerializer.Deserialize<Config>(str, _jsonOptions);
    }
}
