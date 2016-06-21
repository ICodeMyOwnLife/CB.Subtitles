using System;
using System.Linq;


namespace CB.Subtitles
{
    public struct SubtitleLine
    {
        public string Content;
        public TextFormat Format;
        public string Color;

        public string ToSrtString()
        {
            string openColor = "", closeColor = "";
            if (!string.IsNullOrEmpty(Color))
            {
                openColor = $"<font color=\"{Color}\">";
                closeColor = "</font>";
            }
            string openFormat = "", closeFormat = "";
            if (Format != 0)
            {
                var format = Format;
                foreach (var item in Enum.GetValues(typeof(TextFormat)).Cast<TextFormat>().Where(item => format.HasFlag(item))) {
                    switch (item)
                    {
                        case TextFormat.Bold:
                            openFormat += "<b>";
                            closeFormat += "</b>";
                            break;
                        case TextFormat.Italic:
                            openFormat += "<i>";
                            closeFormat += "</i>";
                            break;
                        case TextFormat.Underline:
                            openFormat += "<u>";
                            closeFormat += "</u>";
                            break;
                        case TextFormat.Strikethrough:
                            openFormat += "<s>";
                            closeFormat += "</s>";
                            break;
                    }
                }
            }
            return openColor + openFormat + Content + closeFormat + closeColor;
        }
    }
}