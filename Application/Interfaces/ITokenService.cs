using Application.DTOs.Auth;

namespace Application.Interfaces;

public interface ITokenService
{
    LoginResponse GenerateToken(Domain.Entities.ApplicationUser user, string role);
}
