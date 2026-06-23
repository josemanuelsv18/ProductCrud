namespace ProductManagement.Api.Dtos.Auth;

public sealed record RegisterRequest(string UserName, string Email, string FullName, string Password);

public sealed record AdminCreateUserRequest(string UserName, string Email, string FullName, string Password, string Role);

public sealed record LoginRequest(string Identifier, string Password);

public sealed record AuthUserResponse(int Id, string UserName, string Email, string FullName, string Role);

public sealed record AuthResponse(string Token, AuthUserResponse User);
