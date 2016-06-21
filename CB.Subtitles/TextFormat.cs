using System;


namespace CB.Subtitles
{
    [Flags]
    public enum TextFormat: byte
    {
        None = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikethrough = 8
    }
}