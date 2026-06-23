using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using ProductManagement.Api.Controllers;
using ProductManagement.Api.Data;
using ProductManagement.Api.Domain.Entities;
using ProductManagement.Api.Dtos.Auth;
using ProductManagement.Api.Dtos.Products;
using ProductManagement.Api.Services;
using Xunit;

namespace ProductManagement.Tests;

public sealed class ControllerContractTests
{
    static ControllerContractTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPdfFontConfiguration.Configure();
    }

    [Fact]
    public async Task Register_returns_bad_request_when_required_fields_missing()
    {
        await using var context = CreateContext();
        SeedRole(context, "User");

        var controller = new AuthController(context, new FakeTokenService());

        var result = await controller.Register(new RegisterRequest(string.Empty, string.Empty, string.Empty, string.Empty));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("User name, email, full name, and password are required.", badRequest.Value);
    }

    [Fact]
    public async Task Login_returns_unauthorized_for_invalid_credentials()
    {
        await using var context = CreateContext();
        var userRoleId = SeedRole(context, "User");
        SeedUser(context, userRoleId, "alice", "alice@example.com", "CorrectHorseBatteryStaple");

        var controller = new AuthController(context, new FakeTokenService());

        var result = await controller.Login(new LoginRequest("alice", "wrong-password"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("Invalid credentials.", unauthorized.Value);
    }

    [Fact]
    public async Task Register_returns_conflict_when_user_exists()
    {
        await using var context = CreateContext();
        var userRoleId = SeedRole(context, "User");
        SeedUser(context, userRoleId, "bob", "bob@example.com", "CorrectHorseBatteryStaple");

        var controller = new AuthController(context, new FakeTokenService());

        var result = await controller.Register(new RegisterRequest("bob", "bob2@example.com", "Bob", "password123"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("A user with the same user name or email already exists.", conflict.Value);
    }

    [Fact]
    public async Task Register_creates_user_role_for_public_signup()
    {
        await using var context = CreateContext();
        var userRoleId = SeedRole(context, "User");

        var controller = new AuthController(context, new FakeTokenService());

        var result = await controller.Register(new RegisterRequest("bob", "bob@example.com", "Bob", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("test-token", response.Token);
        Assert.Equal("User", response.User.Role);

        var stored = await context.Users.Include(x => x.Role).SingleAsync(x => x.UserName == "bob");
        Assert.Equal(userRoleId, stored.RoleId);
        Assert.Equal("User", stored.Role.Name);
    }

    [Fact]
    public async Task CreateUser_forbids_non_admin_user()
    {
        await using var context = CreateContext();
        SeedRole(context, "Admin");
        SeedRole(context, "User");

        var controller = new AuthController(context, new FakeTokenService());
        SetUser(controller, role: "User");

        var result = await controller.CreateUser(new AdminCreateUserRequest("bob", "bob@example.com", "Bob", "password123", "User"));

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_creates_admin_role_when_called_by_admin()
    {
        await using var context = CreateContext();
        var adminRoleId = SeedRole(context, "Admin");
        SeedRole(context, "User");

        var controller = new AuthController(context, new FakeTokenService());
        SetUser(controller, role: "Admin");

        var result = await controller.CreateUser(new AdminCreateUserRequest("boss", "boss@example.com", "Boss", "password123", "Admin"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AuthUserResponse>(created.Value);
        Assert.Equal("Admin", response.Role);

        var stored = await context.Users.Include(x => x.Role).SingleAsync(x => x.UserName == "boss");
        Assert.Equal(adminRoleId, stored.RoleId);
        Assert.Equal("Admin", stored.Role.Name);
    }

    [Fact]
    public async Task CreateUser_returns_bad_request_for_invalid_role()
    {
        await using var context = CreateContext();
        SeedRole(context, "Admin");
        SeedRole(context, "User");

        var controller = new AuthController(context, new FakeTokenService());
        SetUser(controller, role: "Admin");

        var result = await controller.CreateUser(new AdminCreateUserRequest("boss", "boss@example.com", "Boss", "password123", "Manager"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Role must be either User or Admin.", badRequest.Value);
    }

    [Fact]
    public async Task GetAll_returns_bad_request_when_page_size_is_invalid()
    {
        await using var context = CreateContext();
        var controller = new ProductsController(context, new FakeReportService());

        var result = await controller.GetAll(new ProductQueryParameters(null, null, false, 1, 101));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Page size must be between 1 and 100.", badRequest.Value);
    }

    [Fact]
    public async Task Create_returns_bad_request_when_brand_does_not_exist()
    {
        await using var context = CreateContext();
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var result = await controller.Create(new ProductCreateRequest("Phone", null, null, 10m, 5, 99));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Selected brand does not exist.", badRequest.Value);
    }

    [Fact]
    public async Task Create_persists_optional_image_url_and_audit_fields()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var result = await controller.Create(new ProductCreateRequest("Runner", "Lightweight shoe", "https://cdn.example.com/shoes/runner.jpg", 120m, 8, brandId));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(created.Value);
        Assert.Equal("https://cdn.example.com/shoes/runner.jpg", response.ImageUrl);
        Assert.Equal("test-user", response.UsuarioCreacion);
        Assert.Null(response.UsuarioModificacion);

        var stored = await context.Products.AsNoTracking().SingleAsync();
        Assert.Equal(response.Id, stored.Id);
        Assert.Equal(response.ImageUrl, stored.ImageUrl);
        Assert.Equal("test-user", stored.UsuarioCreacion);
    }

    [Fact]
    public async Task Create_accepts_null_image_url()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var result = await controller.Create(new ProductCreateRequest("Runner", null, null, 120m, 8, brandId));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(created.Value);
        Assert.Null(response.ImageUrl);
    }

    [Fact]
    public async Task Create_returns_bad_request_for_invalid_image_url()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var result = await controller.Create(new ProductCreateRequest("Runner", null, "http://example.com/image.png", 120m, 8, brandId));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Image URL must be a valid https URL.", badRequest.Value);
    }

    [Fact]
    public async Task Create_returns_bad_request_for_overlength_name()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var result = await controller.Create(new ProductCreateRequest(new string('a', 151), null, null, 120m, 8, brandId));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Product name must be 150 characters or fewer.", badRequest.Value);
    }

    [Fact]
    public async Task Create_returns_bad_request_for_overlength_description()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var result = await controller.Create(new ProductCreateRequest("Runner", new string('a', 501), null, 120m, 8, brandId));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Description must be 500 characters or fewer.", badRequest.Value);
    }

    [Fact]
    public async Task Create_returns_bad_request_for_overlength_https_image_url()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var longPath = new string('a', 2040);
        var imageUrl = $"https://cdn.example.com/{longPath}.jpg";

        var result = await controller.Create(new ProductCreateRequest("Runner", null, imageUrl, 120m, 8, brandId));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Image URL must be 2048 characters or fewer.", badRequest.Value);
    }

    [Fact]
    public async Task Update_overwrites_image_url_accepts_blank_values_and_tracks_modifier()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var product = SeedProduct(context, brandId, status: true);
        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var updateResult = await controller.Update(product.Id, new ProductUpdateRequest("Runner Pro", "Updated", "https://cdn.example.com/shoes/runner-pro.jpg", 135m, 6, brandId, true));
        var updated = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updatedResponse = Assert.IsType<ProductResponse>(updated.Value);
        Assert.Equal("https://cdn.example.com/shoes/runner-pro.jpg", updatedResponse.ImageUrl);
        Assert.Equal("test-user", updatedResponse.UsuarioModificacion);

        var clearResult = await controller.Update(product.Id, new ProductUpdateRequest("Runner Pro", "Updated", "   ", 135m, 6, brandId, true));
        var cleared = Assert.IsType<OkObjectResult>(clearResult.Result);
        var clearedResponse = Assert.IsType<ProductResponse>(cleared.Value);
        Assert.Null(clearedResponse.ImageUrl);
    }

    [Fact]
    public async Task DownloadPdf_caps_and_orders_products()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");

        for (var i = 1; i <= 1005; i++)
        {
            context.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = "Widget",
                BrandId = brandId,
                ImageUrl = null,
                Price = i,
                Stock = i,
                Status = true,
                UsuarioCreacion = "seed-user",
                FechaCreacion = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await context.SaveChangesAsync();

        var reportService = new FakeReportService();
        var controller = new ProductsController(context, reportService);

        var result = await controller.DownloadPdf(new ProductPdfQuery(null, null, false));

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal(1000, reportService.Products.Count);
        Assert.Equal(1000, reportService.Products.Select(product => product.Id).Distinct().Count());
        Assert.All(reportService.Products, product => Assert.Equal("Widget", product.Name));
    }

    [Fact]
    public void ReportService_generates_non_empty_pdf_for_products()
    {
        var brand = new Brand { Id = 1, Name = "Acme" };
        var reportService = new ReportService();

        var bytes = reportService.CreateProductsReport(
        [
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Runner",
                BrandId = brand.Id,
                Brand = brand,
                Price = 120m,
                Stock = 8,
                Status = true,
                UsuarioCreacion = "seed-user",
                FechaCreacion = DateTime.UtcNow
            }
        ],
        "Products Report");

        Assert.NotEmpty(bytes);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.True(PdfContainsTextOperators(bytes));
    }

    [Fact]
    public void ReportService_generates_non_empty_pdf_for_empty_state()
    {
        var reportService = new ReportService();

        var bytes = reportService.CreateProductsReport([], "Products Report");

        Assert.NotEmpty(bytes);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.True(PdfContainsTextOperators(bytes));
    }

    [Fact]
    public async Task GetById_returns_not_found_for_inactive_product_when_user_is_not_admin()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var product = SeedProduct(context, brandId, status: false);

        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "User");

        var result = await controller.GetById(product.Id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_returns_inactive_product_for_admin_user()
    {
        await using var context = CreateContext();
        var brandId = SeedBrand(context, "Acme");
        var product = SeedProduct(context, brandId, status: false);

        var controller = new ProductsController(context, new FakeReportService());
        SetUser(controller, role: "Admin");

        var result = await controller.GetById(product.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(ok.Value);
        Assert.Equal(product.Id, response.Id);
        Assert.False(response.Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static int SeedRole(AppDbContext context, string name)
    {
        var role = new Role { Name = name };
        context.Roles.Add(role);
        context.SaveChanges();
        return role.Id;
    }

    private static int SeedBrand(AppDbContext context, string name)
    {
        var brand = new Brand { Name = name };
        context.Brands.Add(brand);
        context.SaveChanges();
        return brand.Id;
    }

    private static Product SeedProduct(AppDbContext context, int brandId, bool status)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sneaker",
            BrandId = brandId,
            ImageUrl = null,
            Price = 99.99m,
            Stock = 10,
            Status = status,
            UsuarioCreacion = "seed-user",
            FechaCreacion = DateTime.UtcNow
        };

        context.Products.Add(product);
        context.SaveChanges();
        return product;
    }

    private static void SetUser(ControllerBase controller, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Role, role),
            new Claim("username", "test-user")
        }, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static void SeedUser(AppDbContext context, int roleId, string userName, string email, string password)
    {
        context.Users.Add(new User
        {
            UserName = userName,
            Email = email,
            FullName = userName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = roleId
        });
        context.SaveChanges();
    }

    private static bool PdfContainsTextOperators(byte[] bytes)
    {
        var document = Encoding.Latin1.GetString(bytes);

        foreach (Match match in Regex.Matches(document, "stream\\r?\\n(?<content>.*?)\\r?\\nendstream", RegexOptions.Singleline))
        {
            var rawStream = Encoding.Latin1.GetBytes(match.Groups["content"].Value);

            try
            {
                using var compressed = new MemoryStream(rawStream);
                using var zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);

                var content = Encoding.Latin1.GetString(output.ToArray());
                if (content.Contains("BT", StringComparison.Ordinal) && content.Contains("ET", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            catch
            {
                // Skip streams that are not text content compressed with zlib.
            }
        }

        return false;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string CreateToken(User user) => "test-token";
    }

    private sealed class FakeReportService : IReportService
    {
        public IReadOnlyCollection<Product> Products { get; private set; } = Array.Empty<Product>();

        public byte[] CreateProductsReport(IReadOnlyCollection<Product> products, string title, CancellationToken cancellationToken = default)
        {
            Products = products.ToArray();
            return [1, 2, 3];
        }
    }
}
