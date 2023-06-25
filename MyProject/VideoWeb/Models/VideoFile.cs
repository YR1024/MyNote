namespace VideoWeb.Models
{
    public class VideoFile
    {
        public string Name { get; set; }

        public string ReleatviePath { get; set; }

        public float Size { get; set; }
        
        public string FullPath { get; set; }
        public DateTime LastWriteTime { get; internal set; }
    }
}
