using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Identity;

public interface ITokenService
{
    string GenerateJwtToken(IIdentity identity);
}