using FacturacionAPI.DTOs;
using FacturacionAPI.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace FacturacionAPI.Services
{
    public interface IInvoicePdfService
    {
        // documento completo (con detalle) → bytes PDF
        byte[] GenerarPdfFactura( Paciente paciente, Models.Facturacion factura, List<LineaFacturaItem>? lineas);
    }

    public class InvoicePdfService : IInvoicePdfService
    {
        public byte[] GenerarPdfFactura(Paciente paciente, Models.Facturacion factura, List<LineaFacturaItem>? lineas)
        {
            // Formateo
            var culture = new CultureInfo("es-GT");
            string M(string s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Clinica").Bold().FontSize(16);
                                c.Item().Text($"NIT: 12345678");
                                c.Item().Text(M("14 calle"));
                                c.Item().Text($"Tel: 12345678   Email: clinica@gmail.com");
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

                    page.Content().Column(col =>
                    {
                        // Datos Paciente / Factura
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PACIENTE").Bold();
                                c.Item().Text($"{paciente.nombres} {paciente.apellidos}");
                                c.Item().Text($"DPI/NIT: {paciente.dpi}");
                                c.Item().Text($"Dirección: {M(paciente.direccion)}");
                                c.Item().Text($"Correo/Tel: {M(paciente.correo)} / {M(paciente.telefono)}");
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FACTURACIÓN").Bold();
                                c.Item().Text($"Estado: {factura.estado_pago.ToUpper()}");
                                c.Item().Text($"Tipo de pago: {factura.tipo_pago}");
                                c.Item().Text($"Monto total: {factura.monto_total.ToString("C", culture)}");
                            });
                        });

                        // Detalle de líneas
                        if (lineas != null && lineas.Count > 0)
                        {
                            col.Item().PaddingTop(10).Text("Detalle de procedimientos").Bold();
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(6); // procedimiento
                                    cols.RelativeColumn(2); // precio
                                });

                                t.Header(h =>
                                {
                                    h.Cell().Element(CellHeader).Text("Procedimiento");
                                    h.Cell().Element(CellHeader).AlignRight().Text("Precio");
                                });

                                foreach (var l in lineas)
                                {
                                    t.Cell().Element(CellBody).Text(l.procedimiento);
                                    t.Cell().Element(CellBody).AlignRight().Text(l.precio.ToString("C", culture));
                                }

                                static IContainer CellHeader(IContainer c) =>
                                    c.DefaultTextStyle(x => x.SemiBold()).Padding(4).Background(Colors.Grey.Lighten3).BorderBottom(1);

                                static IContainer CellBody(IContainer c) =>
                                    c.Padding(4).BorderBottom(0.5f);
                            });
                        }

                        // Total
                        col.Item().AlignRight().PaddingTop(10).Text($"TOTAL: {factura.monto_total.ToString("C", culture)}").Bold().FontSize(14);

                        // Nota legal
                        col.Item().PaddingTop(15).Text("Este documento es una representación impresa de la factura electrónica.").Italic().FontSize(9);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generado por Sistema de Facturación | ").FontSize(9);
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(9);
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
