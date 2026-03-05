using Maps.Backup.WebAPI.Dtos.Req;
using Maps.Backup.WebAPI.Dtos.Res;
using Maps.Backup.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System;
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
        public async IAsyncEnumerable<RestoreProgressRes> RestoreWithStream(RestoreReq restoreReq, CancellationToken cancellationToken)
        {
            int totalSteps = 10;
            for (int i = 0; i < totalSteps; i++)
            {
                await Task.Delay(1000);
                yield return new RestoreProgressRes { Progress = (i + 1) * 20 }; 
            }
        }

    }
}
