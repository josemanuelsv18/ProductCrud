using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProductManagement.Api.Domain.Entities;

namespace ProductManagement.Api.Services;

public sealed class ReportService : IReportService
{
    public byte[] CreateProductsReport(IReadOnlyCollection<Product> products, string title, CancellationToken cancellationToken = default)
    {
        QuestPdfFontConfiguration.Configure();

        using var stream = new MemoryStream();
        var generatedAt = DateTime.UtcNow;
        var activeCount = products.Count(product => product.Status);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(QuestPdfFontConfiguration.DefaultFontFamily));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).FontSize(20).Bold();
                    column.Item().Text($"Generated at {generatedAt:yyyy-MM-dd HH:mm} UTC");
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Blue.Lighten5).Padding(10).Text($"Products exported: {products.Count}").SemiBold();
                        row.RelativeItem().Background(Colors.Green.Lighten5).Padding(10).Text($"Active products: {activeCount}").SemiBold();
                    });

                    if (products.Count == 0)
                    {
                        column.Item()
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten5)
                            .Padding(14)
                            .Text("No products found for the selected filters.");
                        return;
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Product");
                            header.Cell().Element(HeaderCellStyle).Text("Brand");
                            header.Cell().Element(HeaderCellStyle).Text("Price");
                            header.Cell().Element(HeaderCellStyle).Text("Stock");
                            header.Cell().Element(HeaderCellStyle).Text("Status");
                        });

                        foreach (var product in products)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            table.Cell().Element(BodyCellStyle).Text(product.Name);
                            table.Cell().Element(BodyCellStyle).Text(product.Brand.Name);
                            table.Cell().Element(BodyCellStyle).Text($"{product.Price:C}");
                            table.Cell().Element(BodyCellStyle).Text(product.Stock.ToString());
                            table.Cell().Element(BodyCellStyle).Text(product.Status ? "Active" : "Inactive");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });

        }).GeneratePdf(stream);

        return stream.ToArray();

        static IContainer HeaderCellStyle(IContainer container) => container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .DefaultTextStyle(x => x.SemiBold())
            .PaddingVertical(8)
            .PaddingHorizontal(6)
            .Background(Colors.Grey.Lighten3);

        static IContainer BodyCellStyle(IContainer container) => container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten4)
            .PaddingVertical(6)
            .PaddingHorizontal(6);
    }
}
