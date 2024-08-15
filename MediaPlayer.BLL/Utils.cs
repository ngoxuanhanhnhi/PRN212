using MediaPlayer.DAL.Entities;

namespace MediaPlayer.BLL
{
    public class Utils
    {
        public static Song GetPropertiesFromFilePath(string filePath)
        {
            TagLib.File tagFile = TagLib.File.Create(filePath);

            string artists = tagFile.Tag.Artists?.Length > 0 ? tagFile.Tag.Artists[0] : "Unknown";
            string title = tagFile.Tag.Title ?? "Untitled";
            TimeSpan duration = tagFile.Properties.Duration;

            // Định dạng duration chỉ lấy giờ, phút, giây
            string formattedDuration = string.Format("{0:D2}:{1:D2}:{2:D2}", duration.Hours, duration.Minutes, duration.Seconds);
            return new Song() { Artists = artists, Title = title, Duration = TimeSpan.Parse(formattedDuration), FilePath = filePath };
        }

    }
}
