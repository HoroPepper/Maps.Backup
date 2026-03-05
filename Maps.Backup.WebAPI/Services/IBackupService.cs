using Maps.Backup.WebAPI.Dtos.Req;
using Maps.Backup.WebAPI.Dtos.Res;

namespace Maps.Backup.WebAPI.Services
{
    public interface IBackupService
    {
        Task<RestoreRes> Restore(RestoreReq restoreReq);

        IAsyncEnumerable<RestoreProgressRes> RestoreWithStream(RestoreReq restoreReq, CancellationToken cancellationToken);
    }
}
