namespace ProductManagement.Api.Dtos.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    decimal Price,
    int Stock,
    bool Status,
    int BrandId,
    string BrandName,
    string UsuarioCreacion,
    DateTime FechaCreacion,
    string? UsuarioModificacion,
    DateTime? FechaModificacion);

public sealed record ProductCreateRequest(string Name, string? Description, string? ImageUrl, decimal Price, int Stock, int BrandId);

public sealed record ProductUpdateRequest(string Name, string? Description, string? ImageUrl, decimal Price, int Stock, int BrandId, bool Status);

public sealed record ProductQueryParameters(
    string? Search,
    int? BrandId,
    bool IncludeInactive = false,
    int Page = 1,
    int PageSize = 10);

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
