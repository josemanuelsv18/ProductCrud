using ProductManagement.Api.Domain.Entities;

namespace ProductManagement.Api.Services;

public interface ITokenService
{
    string CreateToken(User user);
}
