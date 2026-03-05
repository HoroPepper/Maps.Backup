using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Models;
using Maps.Backup.WebAPI.Dtos.Req;
using Maps.Backup.WebAPI.Dtos.Res;
using Maps.Backup.WorkFlowLib;
using System.Diagnostics;
using System.Text.Json;

namespace Maps.Backup.WebAPI.Services
{
    public class BackupService : IBackupService
    {
        public async Task<RestoreRes> Restore(RestoreReq restoreReq)
        {
            string name = "";
            if(!String.IsNullOrEmpty(restoreReq.QANO))
            {
                name = $"QA{restoreReq.QANO}_{restoreReq.TicketNO}";
            }
            else
            {
                name = $"QA{restoreReq.TicketNO}";
            }
            string dbName = $"{restoreReq.VersionNO}_{name}";

            Dictionary<string, string> config = LoadConfigFromJson();
            config["backUpDir"] = restoreReq.BackupFilePath;
            config["localSaveDir"] = Path.Combine("F:\\zhengqiwen\\QASource", name);
            config["dbFileSaveDir"] = Path.Combine("\\sftp\\D_disk\\DBData", name);
            config["targetDbName"] = dbName;
            var msgPub = new DelegateStrMsgPub((msg) =>
            {
                Debug.WriteLine(msg);
            });
            BackUpWorkFlowCreater workFlowCreater = new BackUpWorkFlowCreater(msgPub, false);
            var taskMgt = workFlowCreater.Create();
            var taskContext = new TaskContext()
            {
                ContextDic = config,
                MessagePub = msgPub,
            };
            taskMgt.ExecuteAllTasks(null, taskContext);
            if (taskContext.NodeResultList.Any(x => !x.Value?.IsSuccess ?? false))
            {
                return new RestoreRes()
                {
                    IsSuccess = false,
                    DBName = "",
                };
            }

            return new RestoreRes()
            {
                IsSuccess = true,
                DBName = dbName,
            };
        }

        public IAsyncEnumerable<RestoreProgressRes> RestoreWithStream(RestoreReq restoreReq, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static Dictionary<string, string> LoadConfigFromJson()
        {
            try
            {
                string configPath = Path.Combine(Environment.CurrentDirectory, CONFIG_FILE_NAME);
                if (!File.Exists(configPath))
                {
                    return new Dictionary<string, string>();
                }

                string jsonContent = File.ReadAllText(configPath);
                var configDic = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                return configDic ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                return new Dictionary<string, string>();
            }
        }
        private const string CONFIG_FILE_NAME = "backup-config.json";
    }
}
