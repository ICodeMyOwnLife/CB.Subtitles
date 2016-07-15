using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;


namespace CB.Subtitles
{
    public class Subtitle
    {
        #region Fields
        public static readonly string[] Extensions = { ".ass", ".srt", ".sub" };
        #endregion


        #region  Constructors & Destructor
        public Subtitle()
        {
            SubtitleItems = new List<SubtitleItem>(1024);
        }

        public Subtitle(string file): this(file, GetSubtitleType(file)) { }
        public Subtitle(string file, Encoding encoding): this(file, GetSubtitleType(file), encoding) { }

        public Subtitle(string file, SubtitleType type, Encoding encoding)
            : this()
        {
            switch (type)
            {
                case SubtitleType.Ass:
                    TryParseFromAss(file, encoding);
                    break;
                case SubtitleType.Srt:
                    TryParseFromSrt(file, encoding);
                    break;
                case SubtitleType.Sub:
                    TryParseFromSub(file, encoding);
                    break;
            }
        }

        public Subtitle(string file, SubtitleType type)
            : this(file, type, EncodingDetector.Detect(file)) { }
        #endregion


        #region  Properties & Indexers
        public List<SubtitleItem> SubtitleItems { get; }
        #endregion


        #region Methods
        public static void AdjustTime(string file, TimeSpan start, TimeSpan end)
        {
            AdjustTime(file, start, end, file);
        }

        public static void AdjustTime(string source, TimeSpan start, TimeSpan end, string destination)
        {
            var subtitle = new Subtitle(source);
            subtitle.AdjustTime(start, end);
            subtitle.RecordTo(destination);
        }

        public static void AdjustTime(string file, TimeSpan adjustedTime)
        {
            AdjustTime(file, adjustedTime, file);
        }

        public static void AdjustTime(string source, TimeSpan adjustedTime, string destination)
        {
            var subtitle = new Subtitle(source);
            subtitle.AdjustTime(adjustedTime);
            subtitle.RecordTo(destination);
        }

        public static void Convert(string sourceFile, SubtitleType sourceType, string destinationFile,
            SubtitleType destinationType)
        {
            var sub = new Subtitle(sourceFile, sourceType);
            sub.RecordTo(destinationFile, destinationType);
        }

        public static void Convert(string sourceFile, string destinationFile)
        {
            Convert(sourceFile, GetSubtitleType(sourceFile), destinationFile, GetSubtitleType(destinationFile));
        }

        public static SubtitleType GetSubtitleType(string fileOrExtension)
        {
            SubtitleType result;
            if (TryGetSubtitleType(fileOrExtension, out result))
                return result;
            throw new NotSupportedException($"File or extension {fileOrExtension} not supported");
        }

        public static bool IsSubtitleExtension(string extension)
        {
            CheckExtension(ref extension);
            return Extensions.Any(s => s.Equals(extension, StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool IsSubtitleFile(string file)
        {
            return IsSubtitleExtension(Path.GetExtension(file));
        }

        public static Subtitle Parse(string file, SubtitleType type)
        {
            Subtitle subtitle;
            if (TryParse(file, type, out subtitle))
                return subtitle;
            throw new FormatException();
        }

        public static Subtitle Parse(string file)
        {
            return Parse(file, GetSubtitleType(file));
        }

        public static void SyncTime(string syncedFile, string syncingFile)
        {
            SyncTime(syncedFile, syncingFile, syncedFile);
        }

        public static void SyncTime(string source, string syncFile, string destination)
        {
            var subtitle = new Subtitle(source);
            subtitle.SyncTime(syncFile);
            subtitle.RecordTo(destination);
        }

        public static bool TryGetSubtitleType(string fileOrExtension, out SubtitleType type)
        {
            fileOrExtension = fileOrExtension.ToLowerInvariant();
            if (fileOrExtension == "ass" || fileOrExtension.EndsWith(".ass"))
            {
                type = SubtitleType.Ass;
                return true;
            }
            if (fileOrExtension == "srt" || fileOrExtension.EndsWith(".srt"))
            {
                type = SubtitleType.Srt;
                return true;
            }
            else if (fileOrExtension == "sub" || fileOrExtension.EndsWith(".sub"))
            {
                type = SubtitleType.Sub;
                return true;
            }
            type = 0;
            return false;
        }

        public static bool TryParse(string file, SubtitleType type, out Subtitle subtitle)
        {
            subtitle = new Subtitle();
            switch (type)
            {
                case SubtitleType.Ass:
                    return subtitle.TryParseFromAss(file);
                case SubtitleType.Srt:
                    return subtitle.TryParseFromSrt(file);
                case SubtitleType.Sub:
                    return subtitle.TryParseFromSub(file);
                default:
                    return false;
            }
        }

        public static bool TryParse(string file, out Subtitle subtitle)
        {
            SubtitleType type;
            subtitle = null;
            return TryGetSubtitleType(file, out type) && TryParse(file, type, out subtitle);
        }

        public void AdjustTime(int firstParag, int lastParag, TimeSpan start, TimeSpan end)
        {
            var firstBegin = SubtitleItems[firstParag].Begin;
            var ratio = (end - start).TotalMilliseconds /
                        (SubtitleItems[lastParag].Begin - firstBegin).TotalMilliseconds;
            for (var i = firstParag; i <= lastParag; ++i)
            {
                var newSpan = TimeSpan.FromMilliseconds(ratio * (SubtitleItems[i].Begin - firstBegin).TotalMilliseconds);
                SubtitleItems[i].Begin = start + newSpan;
                newSpan = TimeSpan.FromMilliseconds(ratio * (SubtitleItems[i].End - firstBegin).TotalMilliseconds);
                SubtitleItems[i].End = start + newSpan;
            }
        }

        public void AdjustTime(TimeSpan start, TimeSpan end)
        {
            /*TimeSpan firstBegin = subtitleItems.First().Begin;
            double ratio = (end - start).TotalMilliseconds / (subtitleItems.Last().Begin - firstBegin).TotalMilliseconds;
            foreach (var item in subtitleItems)
            {
                TimeSpan newSpan = TimeSpan.FromMilliseconds(ratio * (item.Begin - firstBegin).TotalMilliseconds);
                item.Begin = start + newSpan;
                newSpan = TimeSpan.FromMilliseconds(ratio * (item.End - firstBegin).TotalMilliseconds);
                item.End = start + newSpan;
            }*/
            AdjustTime(0, SubtitleItems.Count - 1, start, end);
        }

        public void AdjustTime(TimeSpan adjustedTime)
        {
            foreach (var item in SubtitleItems)
            {
                item.Begin += adjustedTime;
                item.End += adjustedTime;
            }
        }

        public SubtitleItem GetItemAt(TimeSpan time)
        {
            var subItem = SubtitleItems.FirstOrDefault(si => si.End > time);
            if (subItem == null || subItem.Begin > time)
                return null;
            return subItem;
        }

        public SubtitleItem GetItemAt(double miliseconds)
        {
            var time = TimeSpan.FromMilliseconds(miliseconds);
            return GetItemAt(time);
        }

        public void RecordTo(string file)
        {
            RecordTo(file, GetSubtitleType(file));
        }

        public void RecordTo(string file, SubtitleType type)
        {
            switch (type)
            {
                case SubtitleType.Ass:
                    RecordToAss(file);
                    break;
                case SubtitleType.Srt:
                    RecordToSrt(file);
                    break;
            }
        }

        public void RecordToAss(TextWriter tw) { }

        public void RecordToAss(string assFile)
        {
            using (var sw = new StreamWriter(assFile, false, Encoding.UTF8))
                RecordToAss(sw);
        }

        public void RecordToSrt(TextWriter tw)
        {
            for (var i = 0; i < SubtitleItems.Count; i++)
            {
                tw.WriteLine("{0}\r\n{1}\r\n", i + 1, SubtitleItems[i].ToSrtString());
            }
        }

        public void RecordToSrt(string srtFile)
        {
            using (var sw = new StreamWriter(srtFile, false, Encoding.Unicode))
                RecordToSrt(sw);
        }

        public void SyncTime(Subtitle subtitle)
        {
            if (SubtitleItems.Count == subtitle.SubtitleItems.Count)
            {
                for (var i = 0; i < SubtitleItems.Count; i++)
                {
                    SubtitleItems[i].Begin = subtitle.SubtitleItems[i].Begin;
                    SubtitleItems[i].End = subtitle.SubtitleItems[i].End;
                }
            }
            else AdjustTime(subtitle.SubtitleItems.First().Begin, subtitle.SubtitleItems.Last().Begin);
        }

        public void SyncTime(string file)
        {
            SyncTime(Parse(file));
        }
        #endregion


        #region Implementation
        private static void CheckExtension(ref string extension)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));

            if (!extension.StartsWith("."))
                extension = "." + extension;
        }

        private static void FillAssLines(string text, SubtitleItem si)
        {
            var tokens = text.Split('{', '}');
            var format = TextFormat.None;
            for (var i = 0; i < tokens.Length; i++)
            {
                var s = tokens[i];
                if (i % 2 == 0)
                {
                    if (s != "")
                        si.AddLine(s, format, "");
                }
                else
                {
                    switch (s)
                    {
                        case "\\b1":
                            format |= TextFormat.Bold;
                            break;
                        case "\\i1":
                            format |= TextFormat.Italic;
                            break;
                        case "\\u1":
                            format |= TextFormat.Underline;
                            break;
                        case "\\s1":
                            format |= TextFormat.Strikethrough;
                            break;
                        case "\\b":
                        case "\\b0":
                            if (format.HasFlag(TextFormat.Bold)) format &= ~TextFormat.Bold;
                            break;
                        case "\\i":
                        case "\\i0":
                            if (format.HasFlag(TextFormat.Italic)) format &= ~TextFormat.Italic;
                            break;
                        case "\\u":
                        case "\\u0":
                            if (format.HasFlag(TextFormat.Underline)) format &= ~TextFormat.Underline;
                            break;
                        case "\\s":
                        case "\\s0":
                            if (format.HasFlag(TextFormat.Strikethrough)) format &= ~TextFormat.Strikethrough;
                            break;
                    }
                }
            }
        }

        private static void FillSrtLines(string text, SubtitleItem si) // font color, font size
        {
            var tokens = text.Split('<', '>');
            var format = TextFormat.None;
            var colors = new Stack<string>();
            for (var i = 0; i < tokens.Length; i++)
            {
                var s = tokens[i];
                if (i % 2 == 0)
                {
                    if (s != "")
                        si.AddLine(s, format, colors.Count > 0 ? colors.First() : "");
                }
                else
                {
                    switch (s)
                    {
                        case "b":
                            format |= TextFormat.Bold;
                            break;
                        case "i":
                            format |= TextFormat.Italic;
                            break;
                        case "u":
                            format |= TextFormat.Underline;
                            break;
                        case "s":
                            format |= TextFormat.Strikethrough;
                            break;
                        case "/b":
                            if (format.HasFlag(TextFormat.Bold)) format &= ~TextFormat.Bold;
                            break;
                        case "/i":
                            if (format.HasFlag(TextFormat.Italic)) format &= ~TextFormat.Italic;
                            break;
                        case "/u":
                            if (format.HasFlag(TextFormat.Underline)) format &= ~TextFormat.Underline;
                            break;
                        case "/s":
                            if (format.HasFlag(TextFormat.Strikethrough)) format &= ~TextFormat.Strikethrough;
                            break;
                        case "/font":
                            if(colors.Any()) colors.Pop();
                            break;
                        default:
                            if (s.StartsWith("font color=\""))
                                colors.Push(s.Substring(12, s.Length - 13));
                            break;
                    }
                }
            }
        }

        private bool TryParseFromAss(string assFile)
        {
            return TryParseFromAss(assFile, EncodingDetector.Detect(assFile));
        }

        private bool TryParseFromAss(string assFile, Encoding encoding)
        {
            using (var sr = new StreamReader(assFile, encoding))
            {
                string input;
                for (input = sr.ReadLine();
                    input != null && !input.StartsWith("[Events]", StringComparison.InvariantCultureIgnoreCase);
                    input = sr.ReadLine()) { }
                for (input = sr.ReadLine();
                    input != null && !input.StartsWith("Format:", StringComparison.InvariantCultureIgnoreCase);
                    input = sr.ReadLine()) { }
                if (input == null)
                    return false;
                var number = input.Split(',').Length;
                for (input = sr.ReadLine();
                    input != null && !input.StartsWith("dialogue:", StringComparison.InvariantCultureIgnoreCase);
                    input = sr.ReadLine()) { }
                for (; input != null && input.StartsWith("dialogue:", StringComparison.InvariantCultureIgnoreCase);
                    input = sr.ReadLine())
                {
                    var tokens = input.Split(new[] { "," }, number, StringSplitOptions.None);
                    TimeSpan begin, end;
                    if (tokens.Length < number || !TimeSpan.TryParse(tokens[1], out begin) ||
                        !TimeSpan.TryParse(tokens[2], out end))
                        return false;
                    var si = new SubtitleItem
                    {
                        Begin = begin,
                        End = end
                    };

                    FillAssLines(tokens.Last().Replace("\\N", "\r\n").Replace("\\n", "\r\n"), si);
                    SubtitleItems.Add(si);
                }
            }
            return true;
        }

        private bool TryParseFromSrt(string srtFile)
        {
            return TryParseFromSrt(srtFile, EncodingDetector.Detect(srtFile));
        }

        private bool TryParseFromSrt(string srtFile, Encoding encoding)
        {
            using (var sr = new StreamReader(srtFile, encoding))
            {
                var ci = new CultureInfo("fr-FR");
                string input;
                while ((input = sr.ReadLine()) != null)
                {
                    for (; input != null && !input.Contains(" --> "); input = sr.ReadLine()) { }
                    if (input == null)
                        break;
                    var tokens = input.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    TimeSpan begin, end;
                    if (tokens.Length < 3 ||
                        (!TimeSpan.TryParse(tokens[0], ci, out begin) && !TimeSpan.TryParse(tokens[0], out begin))
                        || (!TimeSpan.TryParse(tokens[2], ci, out end) && !TimeSpan.TryParse(tokens[2], out end)))
                        return false;
                    var si = new SubtitleItem
                    {
                        Begin = begin,
                        End = end
                    };

                    var lines = "";
                    while (!string.IsNullOrEmpty(input = sr.ReadLine()))
                        lines += input + "\r\n";
                    FillSrtLines(lines.TrimEnd('\r', '\n'), si);
                    SubtitleItems.Add(si);
                }
            }
            return SubtitleItems.Count > 0;
        }

        private bool TryParseFromSub(string subFile)
        {
            return TryParseFromSub(subFile, EncodingDetector.Detect(subFile));
        }

        private bool TryParseFromSub(string subFile, Encoding encoding)
        {
            return false;
        }
        #endregion
    }
}