using FacturacionAPI.DTOs;
using FacturacionAPI.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace FacturacionAPI.Services
{
    public class InvoicePdfService
    {
        // === Overload NUEVO: solo factura ===
        public byte[] GenerarPdfFactura(Models.Facturacion factura)
        {
            if (factura == null) throw new ArgumentNullException(nameof(factura));

            // Defensas por si tu mapeo deja null
            factura.estado_pago ??= "pendiente";
            factura.tipo_pago ??= "-";

            // Licencia (Community)
            QuestPDF.Settings.License = LicenseType.Community;

            var culture = new CultureInfo("es-GT");
            string M(string? s) => string.IsNullOrWhiteSpace(s) ? "-" : s;
            string U(string? s) => M(s).ToUpperInvariant();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Clinica").Bold().FontSize(16);
                                c.Item().Text("NIT: 12345678");
                                c.Item().Text("14 calle, Ciudad de Guatemala");
                                c.Item().Text("Tel: 12345678   Email: clinica@gmail.com");
                            });
                            r.ConstantItem(160).Border(1).Padding(8).Column(c =>
                            {
                                c.Item().Text("FACTURA").FontSize(16).Bold().AlignCenter();
                                c.Item().Text($"No.: {factura.id_factura}").AlignCenter();
                                c.Item().Text($"Fecha: {factura.fecha_emision:yyyy-MM-dd}").AlignCenter();
                            });
                        });
                        col.Item().PaddingVertical(10).LineHorizontal(0.8f);
                    });

                    // Body (solo datos de factura)
                    page.Content().Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Text("Datos de Factura").Bold().FontSize(12);

                        col.Item().Border(1).Padding(10).Column(box =>
                        {
                            box.Spacing(4);
                            box.Item().Text($"ID Factura: {factura.id_factura}");
                            box.Item().Text($"Fecha de Emisión: {factura.fecha_emision:yyyy-MM-dd}");
                            box.Item().Text($"Estado: {U(factura.estado_pago)}");
                            box.Item().Text($"Tipo de pago: {M(factura.tipo_pago)}");
                            box.Item().Text($"ID Consulta: {factura.id_consulta}");
                        });

                        col.Item().AlignRight().PaddingTop(10)
                           .Text($"TOTAL: {factura.monto_total.ToString("C", culture)}")
                           .Bold().FontSize(14);

                        col.Item().PaddingTop(15)
                           .Text("Este documento es una representación impresa de la factura electrónica.")
                           .Italic().FontSize(9);
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generado por Sistema de Facturación | ").FontSize(9);
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(9);
                    });
                });
            });

            return doc.GeneratePdf();
        }

        // === Overload COMPATIBLE: ignora paciente/lineas y usa solo la factura ===
        public byte[] GenerarPdfFactura(Paciente paciente, Models.Facturacion factura, List<LineaFacturaItem>? lineas)
        {
            if (factura == null) throw new ArgumentNullException(nameof(factura));
            // Por compatibilidad, delegamos al método principal:
            return GenerarPdfFactura(factura);
        }
    }
}
