using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Data;
using ProductManagement.Api.Domain.Entities;
using ProductManagement.Api.Dtos.Products;
using ProductManagement.Api.Services;

namespace ProductManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProductsController(AppDbContext context, IReportService reportService) : ControllerBase
{
    private const int ProductNameMaxLength = 150;
    private const int ProductDescriptionMaxLength = 500;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll([FromQuery] ProductQueryParameters query, CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            return BadRequest("Query parameters are required.");
        }

        if (query.Page < 1)
        {
            return BadRequest("Page must be greater than or equal to 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100.");
        }

        if (query.BrandId is < 1)
        {
            return BadRequest("Brand id must be greater than or equal to 1.");
        }

        var page = query.Page;
        var pageSize = query.PageSize;
        var includeInactive = query.IncludeInactive && User.IsInRole("Admin");

        var productsQuery = context.Products.AsNoTracking().Include(x => x.Brand).AsQueryable();

        if (!includeInactive)
        {
            productsQuery = productsQuery.Where(x => x.Status);
        }

        if (query.BrandId is not null)
        {
            productsQuery = productsQuery.Where(x => x.BrandId == query.BrandId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            productsQuery = productsQuery.Where(x => x.Name.ToLower().Contains(term) || (x.Description != null && x.Description.ToLower().Contains(term)));
        }

        var totalCount = await productsQuery.CountAsync(cancellationToken);
        var items = await productsQuery
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductResponse(
                x.Id,
                x.Name,
                x.Description,
                x.ImageUrl,
                x.Price,
                x.Stock,
                x.Status,
                x.BrandId,
                x.Brand.Name,
                x.UsuarioCreacion,
                x.FechaCreacion,
                x.UsuarioModificacion,
                x.FechaModificacion))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ProductResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.AsNoTracking().Include(x => x.Brand).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (!product.Status && !User.IsInRole("Admin"))
        {
            return NotFound();
        }

        return Ok(ToResponse(product));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> Create(ProductCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        var validationError = ValidateProductRequest(request.Name, request.Description, request.Price, request.Stock, request.BrandId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var imageUrl = NormalizeImageUrl(request.ImageUrl, out var imageUrlError);
        if (imageUrlError is not null)
        {
            return BadRequest(imageUrlError);
        }

        var name = request.Name.Trim();
        var description = request.Description?.Trim();

        var brandExists = await context.Brands.AnyAsync(x => x.Id == request.BrandId, cancellationToken);
        if (!brandExists)
        {
            return BadRequest("Selected brand does not exist.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            Price = request.Price,
            Stock = request.Stock,
            BrandId = request.BrandId,
            Status = true,
            UsuarioCreacion = GetActor(),
            FechaCreacion = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
        await context.Entry(product).Reference(x => x.Brand).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, ProductUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        var validationError = ValidateProductRequest(request.Name, request.Description, request.Price, request.Stock, request.BrandId);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var imageUrl = NormalizeImageUrl(request.ImageUrl, out var imageUrlError);
        if (imageUrlError is not null)
        {
            return BadRequest(imageUrlError);
        }

        var product = await context.Products.Include(x => x.Brand).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        var description = request.Description?.Trim();

        var brandExists = await context.Brands.AnyAsync(x => x.Id == request.BrandId, cancellationToken);
        if (!brandExists)
        {
            return BadRequest("Selected brand does not exist.");
        }

        product.Name = name;
        product.Description = description;
        product.ImageUrl = imageUrl;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.BrandId = request.BrandId;
        product.Status = request.Status;
        product.UsuarioModificacion = GetActor();
        product.FechaModificacion = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await context.Entry(product).Reference(x => x.Brand).LoadAsync(cancellationToken);

        return Ok(ToResponse(product));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        product.Status = false;
        product.UsuarioModificacion = GetActor();
        product.FechaModificacion = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("report/pdf")]
    public async Task<IActionResult> DownloadPdf([FromQuery] ProductPdfQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            return BadRequest("Query parameters are required.");
        }

        if (query.BrandId is < 1)
        {
            return BadRequest("Brand id must be greater than or equal to 1.");
        }

        var includeInactive = query.IncludeInactive && User.IsInRole("Admin");
        const int maxExportRows = 1000;

        var productsQuery = context.Products.AsNoTracking().Include(x => x.Brand).AsQueryable();

        if (!includeInactive)
        {
            productsQuery = productsQuery.Where(x => x.Status);
        }

        if (query.BrandId is not null)
        {
            productsQuery = productsQuery.Where(x => x.BrandId == query.BrandId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            productsQuery = productsQuery.Where(x => x.Name.ToLower().Contains(term) || (x.Description != null && x.Description.ToLower().Contains(term)));
        }

        var products = await productsQuery
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Take(maxExportRows)
            .ToListAsync(cancellationToken);

        var bytes = reportService.CreateProductsReport(products, "Products Report", cancellationToken);

        return File(bytes, "application/pdf", $"products-report-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    private static string? ValidateProductRequest(string? name, string? description, decimal price, int stock, int brandId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required.";
        }

        if (name.Trim().Length > ProductNameMaxLength)
        {
            return $"Product name must be {ProductNameMaxLength} characters or fewer.";
        }

        if (description?.Trim().Length > ProductDescriptionMaxLength)
        {
            return $"Description must be {ProductDescriptionMaxLength} characters or fewer.";
        }

        if (price <= 0)
        {
            return "Price must be greater than 0.";
        }

        if (stock < 0)
        {
            return "Stock must be zero or greater.";
        }

        if (brandId < 1)
        {
            return "Brand is required.";
        }

        return null;
    }

    private static string? NormalizeImageUrl(string? imageUrl, out string? validationError)
    {
        validationError = null;

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var normalized = imageUrl.Trim();
        if (normalized.Length > 2048)
        {
            validationError = "Image URL must be 2048 characters or fewer.";
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            validationError = "Image URL must be a valid https URL.";
            return null;
        }

        return normalized;
    }

    private ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.ImageUrl,
            product.Price,
            product.Stock,
            product.Status,
            product.BrandId,
            product.Brand.Name,
            product.UsuarioCreacion,
            product.FechaCreacion,
            product.UsuarioModificacion,
            product.FechaModificacion);
    }

    private string GetActor()
    {
        return User.FindFirstValue("username")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "system";
    }
}
