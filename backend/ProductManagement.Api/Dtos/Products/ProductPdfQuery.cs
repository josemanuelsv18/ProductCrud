namespace ProductManagement.Api.Dtos.Products;

public sealed record ProductPdfQuery(string? Search, int? BrandId, bool IncludeInactive = false);
