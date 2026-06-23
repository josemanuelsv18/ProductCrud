using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Data;
using ProductManagement.Api.Domain.Entities;
using ProductManagement.Api.Dtos.Auth;
using ProductManagement.Api.Services;

namespace ProductManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(AppDbContext context, ITokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = await CreateUserAsync(request is not null, request?.UserName, request?.Email, request?.FullName, request?.Password, "User", cancellationToken);
        if (user.Result is not null)
        {
            return user.Result;
        }

        var createdUser = user.Value!;
        var token = tokenService.CreateToken(createdUser);

        return Ok(new AuthResponse(token, new AuthUserResponse(createdUser.Id, createdUser.UserName, createdUser.Email, createdUser.FullName, createdUser.Role.Name)));
    }

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AuthUserResponse>> CreateUser(AdminCreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var user = await CreateUserAsync(request is not null, request?.UserName, request?.Email, request?.FullName, request?.Password, request?.Role, cancellationToken);
        if (user.Result is not null)
        {
            return user.Result;
        }

        var createdUser = user.Value!;
        return CreatedAtAction(nameof(CreateUser), new { id = createdUser.Id }, new AuthUserResponse(createdUser.Id, createdUser.UserName, createdUser.Email, createdUser.FullName, createdUser.Role.Name));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        var identifier = request.Identifier?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest("Identifier and password are required.");
        }

        var normalizedIdentifier = identifier.ToLowerInvariant();
        var user = await context.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.UserName.ToLower() == normalizedIdentifier || x.Email.ToLower() == normalizedIdentifier, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return Unauthorized("Invalid credentials.");
        }

        var token = tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, new AuthUserResponse(user.Id, user.UserName, user.Email, user.FullName, user.Role.Name)));
    }

    private async Task<ActionResult<User>> CreateUserAsync(bool hasRequest, string? userName, string? email, string? fullName, string? password, string? roleName, CancellationToken cancellationToken)
    {
        if (!hasRequest)
        {
            return BadRequest("Request body is required.");
        }

        userName = userName?.Trim();
        email = email?.Trim();
        fullName = fullName?.Trim();
        roleName = roleName?.Trim();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest("User name, email, full name, and password are required.");
        }

        if (string.IsNullOrWhiteSpace(roleName) || (roleName != "User" && roleName != "Admin"))
        {
            return BadRequest("Role must be either User or Admin.");
        }

        var normalizedUserName = userName.ToLowerInvariant();
        var normalizedEmail = email.ToLowerInvariant();

        var exists = await context.Users.AnyAsync(x => x.UserName == normalizedUserName || x.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return Conflict("A user with the same user name or email already exists.");
        }

        var userRole = await context.Roles.SingleAsync(x => x.Name == roleName, cancellationToken);

        var user = new User
        {
            UserName = normalizedUserName,
            Email = normalizedEmail,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = userRole.Id
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        await context.Entry(user).Reference(x => x.Role).LoadAsync(cancellationToken);
        return user;
    }
}
