using XConnect.Models;

namespace XConnect.Auth.Interface
{
    public interface IAuthenticationTokenAgent
    {
        Task<AuthData> Authenticate(TokenData TokenData, CommonRequest data, string IP = "", string UserAgent = "", bool IsRefereshCache = false );
    }
}
