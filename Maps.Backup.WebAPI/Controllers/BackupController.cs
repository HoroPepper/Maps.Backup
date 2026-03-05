using Maps.Backup.WebAPI.Dtos.Req;
using Maps.Backup.WebAPI.Dtos.Res;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Maps.Backup.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BackupController : ControllerBase
    {
        [HttpPut(Name = "restore")]
        public RestoreRes Restore(RestoreReq restoreReq)
        {
            return new RestoreRes
            {
                IsSuccess = true,
                DBName = "TestDB"
            };
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
