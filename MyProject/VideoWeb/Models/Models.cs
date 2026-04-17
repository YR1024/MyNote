namespace VideoWeb.Models
{
    public class Video
    {
        public int Id { get; set; } // 数据库主键

        public string Name { get; set; } = "";

        // 注意：原代码是 RelativePath，建议顺手修正拼写为 RelativePath
        public string RelativePath { get; set; } = "";
        public float Size { get; set; }
        public string FullPath { get; set; } = "";
        public DateTime LastWriteTime { get; set; }
        public bool IsStar { get; set; }

        // --- 新增的详细信息字段 ---
        public string CoverPath { get; set; } = "";     // 封面路径
        public string Description { get; set; } = "";   // 简介
        public string Code { get; set; } = "";          // 番号

        // --- 导航属性：多对多关联 ---
        public List<Actor> Actors { get; set; } = new List<Actor>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }

    // 新增：演员表
    public class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        // 导航属性：一个演员参演多部视频
        public List<Video> Videos { get; set; } = new List<Video>();
    }

    // 新增：标签表
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        // 导航属性：一个标签对应多部视频
        public List<Video> Videos { get; set; } = new List<Video>();
    }
}