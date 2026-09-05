using System.Security.Claims;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public string Email => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public bool IsCustomer => Role == "Customer";
    public bool IsAgent => Role == "SupportAgent";
    public bool IsAdmin => Role == "Admin";
    public bool IsStaff => IsAgent || IsAdmin;
}
