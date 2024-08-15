using MediaPlayer.DAL.Entities;
using MediaPlayer.DAL.Repositories;

namespace MediaPlayer.BLL.Services
{
    public class RecentSongsService
    {
        private RecentSongsRepo _repo = new();
        public List<Song> RecentSongs { get; private set; }

        public void LoadData()
        {
            RecentSongs = _repo.LoadData();
        }

        public void SaveData()
        {
            _repo.SaveData(RecentSongs);
        }

        public void AddNewFile(Song newSong)
        {
            if (RecentSongs.Contains(newSong))
            {
                RecentSongs.Remove(newSong);
            }
            RecentSongs.Add(newSong);
            if (RecentSongs.Count > 20)
            {
                RecentSongs.RemoveAt(0);
            }
        }

    }
}

