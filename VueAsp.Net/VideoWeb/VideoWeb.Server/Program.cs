
using SqlSugar;
using VideoWeb.Server.Models;

namespace VideoWeb.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
           
            if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            // 添加此中间件，放在 UseHttpsRedirection 之前
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    context.Response.Redirect("/swagger");
                    return;
                }
                await next();
            });

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.MapControllers();
            app.MapFallbackToFile("/index.html");

            if(InitDatabase())
            {
                SeedData();
            }

            app.Run();
        }


        static bool InitDatabase()
        {
            // 创建 db 文件夹（如果不存在）
            var dbFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db");
            Directory.CreateDirectory(dbFolderPath);

            // 配置 SQLite 数据库连接
            var connectionConfig = new ConnectionConfig
            {
                ConnectionString = $"Data Source={Path.Combine(dbFolderPath, "mydb.db")}", // 数据库文件路径
                DbType = DbType.Sqlite, // 数据库类型改为 SQLite
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            };

            bool newDb = !File.Exists(Path.Combine(dbFolderPath, "mydb.db"));

            using (var db = new SqlSugarClient(connectionConfig))
            {
                // SQLite 无需手动创建数据库，文件不存在时会自动生成
                // 根据模型生成表
                db.CodeFirst.InitTables(
                    typeof(VideoFile),
                    typeof(VideoTag),
                    typeof(VideoFileTag)
                );
            }
            return newDb;
        }

        static void SeedData()
        {
            var dbFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db");
            using (var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = $"Data Source={Path.Combine(dbFolderPath, "mydb.db")}",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true
            }))
            {
                // 插入初始标签数据
                if (!db.Queryable<VideoTag>().Any())
                {
                    var tags = Enum.GetNames(typeof(VideoTagType))
                        .Select(name => new VideoTag { Name = name })
                        .ToList();

                    db.Insertable(tags).ExecuteCommand();
                }
            }
           
        }
    }
}
