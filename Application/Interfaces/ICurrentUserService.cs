namespace Application.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }
    string Email { get; }
    string Role { get; }
    bool IsCustomer { get; }
    bool IsAgent { get; }
    bool IsAdmin { get; }
    bool IsStaff { get; }
}
