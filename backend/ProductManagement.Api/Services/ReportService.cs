using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProductManagement.Api.Domain.Entities;

namespace ProductManagement.Api.Services;

public sealed class ReportService : IReportService
{
    public byte[] CreateProductsReport(IReadOnlyCollection<Product> products, string title, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).FontSize(20).Bold();
                    column.Item().Text($"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });

                page.Content().Table(table =>
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
                        header.Cell().Element(CellStyle).Text("Product");
                        header.Cell().Element(CellStyle).Text("Brand");
                        header.Cell().Element(CellStyle).Text("Price");
                        header.Cell().Element(CellStyle).Text("Stock");
                        header.Cell().Element(CellStyle).Text("Status");

                        static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(6).PaddingHorizontal(4).Background(Colors.Grey.Lighten3);
                    });

                    foreach (var product in products)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(4).Text(product.Name);
                        table.Cell().PaddingVertical(4).PaddingHorizontal(4).Text(product.Brand.Name);
                        table.Cell().PaddingVertical(4).PaddingHorizontal(4).Text($"{product.Price:C}");
                        table.Cell().PaddingVertical(4).PaddingHorizontal(4).Text(product.Stock.ToString());
                        table.Cell().PaddingVertical(4).PaddingHorizontal(4).Text(product.Status ? "Active" : "Inactive");
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });

        }).GeneratePdf(stream);

        return stream.ToArray();
    }
}
