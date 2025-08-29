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
            builder.Services.AddScoped<FacturacionAPI.Services.IInvoicePdfService, FacturacionAPI.Services.InvoicePdfService>();

            // MySQL: registra MySql.Data.MySqlClient.MySqlConnection
            builder.Services.AddScoped<MySqlConnection>(sp =>
            {
                var cs = builder.Configuration.GetConnectionString("MySqlConnection");
                return new MySqlConnection(cs);
            });

            // Registros de DI
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

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            app.Run();
        }
    }
}
