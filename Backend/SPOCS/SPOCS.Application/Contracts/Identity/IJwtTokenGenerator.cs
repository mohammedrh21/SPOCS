using SPOCS.Domain.Entities;

namespace SPOCS.Application.Contracts.Identity;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IList<string> roles);
}
