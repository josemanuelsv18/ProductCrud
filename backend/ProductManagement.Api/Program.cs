using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using ProductManagement.Api.Configuration;
using ProductManagement.Api.Data;
using ProductManagement.Api.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required. Set it via environment variables or a local .env file.");
}

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.Key = FirstConfigured(jwtOptions.Key, builder.Configuration["JWT_SIGNING_KEY"]) ?? string.Empty;
jwtOptions.Issuer = FirstConfigured(jwtOptions.Issuer, builder.Configuration["JWT_ISSUER"]);
jwtOptions.Audience = FirstConfigured(jwtOptions.Audience, builder.Configuration["JWT_AUDIENCE"]);

ValidateJwtOptions(jwtOptions);

var seedAdminOptions = builder.Configuration.GetSection("SeedAdmin").Get<SeedAdminOptions>() ?? new SeedAdminOptions();
seedAdminOptions.UserName = FirstConfigured(seedAdminOptions.UserName, builder.Configuration["SEED_ADMIN_USERNAME"]);
seedAdminOptions.Email = FirstConfigured(seedAdminOptions.Email, builder.Configuration["SEED_ADMIN_EMAIL"]);
seedAdminOptions.FullName = FirstConfigured(seedAdminOptions.FullName, builder.Configuration["SEED_ADMIN_FULL_NAME"]);
seedAdminOptions.Password = FirstConfigured(seedAdminOptions.Password, builder.Configuration["SEED_ADMIN_PASSWORD"]);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductManagement.Api", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token as: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(seedAdminOptions);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOptions.Issuer),
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtOptions.Audience),
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var corsOrigin = builder.Configuration["CORS_ORIGIN"];
        origins = !string.IsNullOrWhiteSpace(corsOrigin) ? [corsOrigin] : origins ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

QuestPDF.Settings.License = LicenseType.Community;
QuestPdfFontConfiguration.Configure();

var app = builder.Build();

await InitializeDatabaseAsync(app.Services, app.Logger);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/swagger", () => Results.Redirect("/swagger/"));
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static void ValidateJwtOptions(JwtOptions options)
{
    if (string.IsNullOrWhiteSpace(options.Key))
    {
        throw new InvalidOperationException("Jwt:Key is required.");
    }

    if (Encoding.UTF8.GetByteCount(options.Key) < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
    }
}

static async Task InitializeDatabaseAsync(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedAdminOptions = scope.ServiceProvider.GetRequiredService<SeedAdminOptions>();

    const int maxAttempts = 5;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await DataSeeder.SeedAsync(db, seedAdminOptions);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts && IsRetryableStartupException(ex))
        {
            logger.LogWarning(ex, "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(attempt));
        }
    }
}

static bool IsRetryableStartupException(Exception exception)
{
    if (exception is NpgsqlException or TimeoutException or IOException or SocketException)
    {
        return true;
    }

    return exception.InnerException is not null && IsRetryableStartupException(exception.InnerException);
}

static string? FirstConfigured(params string?[] values)
{
    return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
