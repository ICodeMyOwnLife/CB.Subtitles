using System.IO;
using System.Text;


namespace CB.Subtitles
{
    public class EncodingDetector
    {
        #region Methods
        public static Encoding Detect(string file)
        {
            var buffer = new byte[4];
            using (var fs = File.OpenRead(file))
            {
                var count = fs.Read(buffer, 0, buffer.Length);
                if (count == 4 && buffer[0] == 0xff && buffer[1] == 0xfe && buffer[2] == 0 && buffer[3] == 0)
                    return Encoding.UTF32;
                if (count < 2) return Encoding.UTF8;
                if (buffer[0] == 0xff && buffer[1] == 0xfe)
                    return Encoding.Unicode;
                if (buffer[0] == 0xfe && buffer[1] == 0xff)
                    return Encoding.BigEndianUnicode;
                return Encoding.UTF8;
            }
        }
        #endregion
    }
}