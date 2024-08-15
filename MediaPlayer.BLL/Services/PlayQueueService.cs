using MediaPlayer.DAL.Entities;

namespace MediaPlayer.BLL.Services
{
    public class PlayQueueService
    {
        public List<Song> PlayQueue { get; set; }

        public void AddASong(Song song) => PlayQueue.Add(song);

    }
}
