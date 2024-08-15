namespace MediaPlayer.DAL.Entities
{
    public class Song
    {
        public string Title { get; set; }
        public string Artists { get; set; }
        public TimeSpan Duration { get; set; }
        public string FilePath { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Song other = (Song)obj;
            return FilePath == other.FilePath;
        }

        public override int GetHashCode()
        {
            return (FilePath).GetHashCode();
        }

    }
}
