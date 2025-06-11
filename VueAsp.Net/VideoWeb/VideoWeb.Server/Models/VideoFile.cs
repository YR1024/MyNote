using SqlSugar;

namespace VideoWeb.Server.Models
{
    [SugarTable("VideoFile")] 
    public class VideoFile
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)] // 主键且自增
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string ReleatviePath { get; set; } = "";

        public float Size { get; set; }
        
        public string FullPath { get; set; } = ""; 

        public DateTime LastWriteTime { get; set; }

        // 多对多关联
        [Navigate(typeof(VideoFileTag), nameof(VideoFileTag.VideoFileId), nameof(VideoFileTag.TagId))]
        public List<VideoTag> Tags { get; set; } = new List<VideoTag>();

    }

    // 修改后的 VideoTag 实体类（对应数据库表）
    [SugarTable("VideoTag")] // 指定表名
    public class VideoTag
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(Length = 50, IsNullable = false)]
        public string Name { get; set; } // 存储枚举值（如 "巨乳", "少女"）

        // 多对多关联
        [Navigate(typeof(VideoFileTag), nameof(VideoFileTag.TagId), nameof(VideoFileTag.VideoFileId))]
        public List<VideoFile> Tags { get; set; } = new List<VideoFile>();
    }



    // 中间表
    [SugarTable("VideoFileTags")]
    public class VideoFileTag
    {
        [SugarColumn(IsPrimaryKey = true)]
        public int VideoFileId { get; set; }

        [SugarColumn(IsPrimaryKey = true)]
        public int TagId { get; set; }
    }

    /// <summary>
    /// 视频标签
    /// </summary>
    public enum VideoTagType
    {
        巨乳,
        少女,
        凌辱,
        姐姐,
    }

}
