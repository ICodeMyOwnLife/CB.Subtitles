using System;
using System.Collections.Generic;
using System.Text;


namespace CB.Subtitles
{
    public class SubtitleItem
    {
        #region  Constructors & Destructor
        public SubtitleItem()
        {
            Lines = new List<SubtitleLine>();
        }
        #endregion


        #region  Properties & Indexers
        public TimeSpan Begin { get; set; }
        public TimeSpan Duration => End - Begin;
        public TimeSpan End { get; set; }
        public List<SubtitleLine> Lines { get; set; }

        public string Text
        {
            get
            {
                var sb = new StringBuilder();
                Lines.ForEach(l => sb.Append(l.Content));
                return sb.ToString();
            }
        }
        #endregion


        #region Methods
        public void AddLine(string content, TextFormat format = TextFormat.None, string color = "")
        {
            Lines.Add(new SubtitleLine { Content = content, Format = format, Color = color });
        }

        public TimeSpan GetDuration()
        {
            return End - Begin;
        }

        public string ToSrtString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} --> {1}\r\n", Begin.ToString(@"hh\:mm\:ss\,fff"), End.ToString(@"hh\:mm\:ss\,fff"));
            var format = TextFormat.None;
            var color = "";
            Lines.ForEach(l =>
            {
                if (l.Color != color)
                {
                    if (!string.IsNullOrEmpty(color))
                        sb.Append("</font>");
                    color = l.Color;
                    if (!string.IsNullOrEmpty(color))
                        sb.AppendFormat("<font color=\"{0}\">", color);
                }
                sb.AppendFormat("{0}{1}", GetSrtDifferentFormatString(format, l.Format), l.Content);
                format = l.Format;
            });
            sb.Append(GetSrtDifferentFormatString(format, TextFormat.None));
            if (!string.IsNullOrEmpty(color))
                sb.Append("</font>");
            return sb.ToString();
        }
        #endregion


        #region Implementation
        private static string GetSrtDifferentFormatString(TextFormat oldFormat, TextFormat newFormat)
        {
            var result = "";
            foreach (TextFormat item in Enum.GetValues(typeof(TextFormat)))
            {
                if (oldFormat.HasFlag(item) && !newFormat.HasFlag(item))
                {
                    result += GetSrtFormatString(~item);
                }
                else if (!oldFormat.HasFlag(item) && newFormat.HasFlag(item))
                {
                    result += GetSrtFormatString(item);
                }
            }
            return result;
        }

        private static string GetSrtFormatString(TextFormat format)
        {
            switch (format)
            {
                case TextFormat.Bold:
                    return "<b>";
                case TextFormat.Italic:
                    return "<i>";
                case TextFormat.Underline:
                    return "<u>";
                case TextFormat.Strikethrough:
                    return "<s>";
                case ~TextFormat.Bold:
                    return "</b>";
                case ~TextFormat.Italic:
                    return "</i>";
                case ~TextFormat.Underline:
                    return "</u>";
                case ~TextFormat.Strikethrough:
                    return "</s>";
                default:
                    return "";
            }
        }
        #endregion
    }
}