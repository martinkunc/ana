using System;
using System.Threading.Tasks;

public interface ITokenService
{
    Task<string> GetValidAccessTokenAsync();
    Task<bool> IsTokenExpiringSoonAsync();
    event EventHandler<TokenExpiredEventArgs> TokenExpired;
}

public class TokenExpiredEventArgs : EventArgs
{
    public string Message { get; set; }
    public bool RequiresRedirect { get; set; }
}