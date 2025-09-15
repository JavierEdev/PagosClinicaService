using FacturacionAPI.Repositories;
using FacturacionAPI.Services;
using MySql.Data.MySqlClient; // OJO: entonces tu repositorio debe usar este mismo paquete (MySql.Data)

namespace FacturacionAPI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<InvoicePdfService>();

            builder.Services.AddScoped<MySqlConnection>(sp =>
            {
                var cs = builder.Configuration.GetConnectionString("MySqlConnection");
                return new MySqlConnection(cs);
            });

            const string CorsPolicy = "SpaDev";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: CorsPolicy, policy =>
                    policy
                        .WithOrigins(
                            "http://localhost:5173", "http://127.0.0.1:5173",
                            "http://localhost:4173", "http://127.0.0.1:4173"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                );
            });

            //Injección de dependencias
            builder.Services.AddScoped<IFacturacionRepository, FacturacionRepository>();
            builder.Services.AddScoped<IFacturacionService, FacturacionService>();
            builder.Services.AddScoped<InvoicePdfService>();
            builder.Services.AddScoped<IFacturacionRepository, FacturacionRepository>();
            builder.Services.AddScoped<IFacturacionService, FacturacionService>();

            builder.Services.AddControllers().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.MapControllers();
            app.UseCors(CorsPolicy);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            app.Run();
        }
    }
}
