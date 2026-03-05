using Maps.Backup.WebAPI.Dtos.Req;
using Maps.Backup.WebAPI.Dtos.Res;
using Maps.Backup.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Maps.Backup.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BackupController : ControllerBase
    {

        private readonly IBackupService _backupService;

        public BackupController(IBackupService backupService) 
        {
            _backupService = backupService;
        }

        [HttpPut(Name = "restore")]
        public async Task<RestoreRes> Restore(RestoreReq restoreReq)
        {
            return await _backupService.Restore(restoreReq);
        }

        [HttpPut("restore/stream")]
        [Produces("text/event-stream")]
        public async IAsyncEnumerable<RestoreProgressRes> RestoreWithStream([FromBody] RestoreReq restoreReq, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var progress in _backupService.RestoreWithStream(restoreReq, cancellationToken)
                                .WithCancellation(cancellationToken)
                                .ConfigureAwait(false))
            {
                yield return progress;
            }
        }

    }
}
