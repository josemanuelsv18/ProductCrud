using ProductManagement.Api.Domain.Entities;

namespace ProductManagement.Api.Services;

public interface IReportService
{
    byte[] CreateProductsReport(IReadOnlyCollection<Product> products, string title, CancellationToken cancellationToken = default);
}
