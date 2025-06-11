
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


            // ��Ӵ��м�������� UseHttpsRedirection ֮ǰ
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
            // ���� db �ļ��У���������ڣ�
            var dbFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db");
            Directory.CreateDirectory(dbFolderPath);

            // ���� SQLite ���ݿ�����
            var connectionConfig = new ConnectionConfig
            {
                ConnectionString = $"Data Source={Path.Combine(dbFolderPath, "mydb.db")}", // ���ݿ��ļ�·��
                DbType = DbType.Sqlite, // ���ݿ����͸�Ϊ SQLite
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            };

            bool newDb = !File.Exists(Path.Combine(dbFolderPath, "mydb.db"));

            using (var db = new SqlSugarClient(connectionConfig))
            {
                // SQLite �����ֶ��������ݿ⣬�ļ�������ʱ���Զ�����
                // ����ģ�����ɱ�
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
                // �����ʼ��ǩ����
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
