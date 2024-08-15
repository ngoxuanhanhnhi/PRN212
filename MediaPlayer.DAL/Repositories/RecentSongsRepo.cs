using MediaPlayer.DAL.Entities;
using System.Text.Json;

namespace MediaPlayer.DAL.Repositories
{
    public class RecentSongsRepo
    {
        private readonly string _filePath = @"RecentSongsDB.json";
        public List<Song> LoadData()
        {
            if (File.Exists(_filePath))
            {
                // Đọc nội dung file JSON và chuyển đổi sang List<Song>
                string json = File.ReadAllText(_filePath);
                if (String.IsNullOrEmpty(json))
                {
                    return new List<Song>();
                }
                return JsonSerializer.Deserialize<List<Song>>(json);
            }
            else
            {
                //create DB file
                return new List<Song>(); ;
            }
        }

        public void SaveData(List<Song> recentSongs)
        {
            // Chuyển đổi List<Song> sang chuỗi JSON
            string json = JsonSerializer.Serialize(recentSongs, new JsonSerializerOptions { WriteIndented = true });

            // Ghi chuỗi JSON xuống file (xóa file cũ và ghi lại)
            File.WriteAllText(_filePath, json);
        }

    }
}
