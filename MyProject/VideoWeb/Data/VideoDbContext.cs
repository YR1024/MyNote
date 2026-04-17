using Microsoft.EntityFrameworkCore;
using VideoWeb.Models;

namespace VideoWeb.Data
{
    public class VideoDbContext : DbContext
    {
        public VideoDbContext(DbContextOptions<VideoDbContext> options) : base(options)
        {
        }

        // 定义数据表
        public DbSet<Video> Videos { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 可选：在这里配置一些字段约束，比如限制"番号"不要重复
            // modelBuilder.Entity<Video>().HasIndex(v => v.Code).IsUnique();
        }
    }
}