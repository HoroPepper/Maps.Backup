using Maps.Backup.WebAPI.Dtos.Req;
using Maps.Backup.WebAPI.Dtos.Res;

namespace Maps.Backup.WebAPI.Services
{
    public interface IAuthService
    {
        Task<LoginRes> Login(LoginReq loginReq);

        Task<TokenRefreshRes> RefreshToken(TokenRefreshReq tokenRefreshReq);
    }
}
