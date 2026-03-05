using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Models;
using Maps.Backup.WebAPI.Dtos.Req;
using Maps.Backup.WebAPI.Dtos.Res;
using Maps.Backup.WorkFlowLib;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Maps.Backup.WebAPI.Services
{
    public class BackupService : IBackupService
    {
        private const string CONFIG_FILE_NAME = "backup-config.json";

        public async Task<RestoreRes> Restore(RestoreReq restoreReq)
        {
            string name = "";
            if (!String.IsNullOrEmpty(restoreReq.QANO))
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

        public async IAsyncEnumerable<RestoreProgressRes> RestoreWithStream(RestoreReq restoreReq, CancellationToken cancellationToken)
        {
            string name = string.IsNullOrEmpty(restoreReq.QANO)
                ? $"QA{restoreReq.TicketNO}"
                : $"QA{restoreReq.QANO}_{restoreReq.TicketNO}";
            string dbName = $"{restoreReq.VersionNO}_{name}";

            var progressQueue = new ConcurrentQueue<string>();
            bool isTaskCompleted = false;
            bool isSuccess = false;

            // 3. 加载并配置参数
            Dictionary<string, string> config = LoadConfigFromJson();
            config["backUpDir"] = restoreReq.BackupFilePath;
            config["localSaveDir"] = Path.Combine("F:\\zhengqiwen\\QASource", name);
            config["dbFileSaveDir"] = Path.Combine("\\sftp\\D_disk\\DBData", name);
            config["targetDbName"] = dbName;

            // 4. 自定义消息发布器，将进度消息放入队列
            var msgPub = new DelegateStrMsgPub((msg) =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    progressQueue.Enqueue(msg);
                    Debug.WriteLine(msg);
                }
            });

            // 5. 在后台执行恢复任务
            var executeTask = Task.Run(() =>
            {
                try
                {
                    BackUpWorkFlowCreater workFlowCreater = new BackUpWorkFlowCreater(msgPub, false);
                    var taskMgt = workFlowCreater.Create();
                    var taskContext = new TaskContext()
                    {
                        ContextDic = config,
                        MessagePub = msgPub,
                    };

                    taskMgt.ExecuteAllTasks(null, taskContext);

                    isSuccess = !taskContext.NodeResultList.Any(x => !x.Value?.IsSuccess ?? false);
                }
                catch (Exception ex)
                {
                    progressQueue.Enqueue($"执行出错: {ex.Message}");
                    isSuccess = false;
                }
                finally
                {
                    isTaskCompleted = true;
                }
            }, cancellationToken);

            // 6. 流式返回进度信息
            while (!cancellationToken.IsCancellationRequested)
            {
                // 检查队列是否有消息
                while (progressQueue.TryDequeue(out string progressMsg))
                {
                    yield return new RestoreProgressRes
                    {
                        Message = progressMsg,
                        IsSuccess = false,
                        DBName = dbName
                    };
                }

                // 如果任务已完成，返回最终结果并退出循环
                if (isTaskCompleted)
                {
                    yield return new RestoreProgressRes
                    {
                        Message = "succeed",
                        IsSuccess = true,
                        DBName = dbName
                    };
                    break;
                }
            }

            // 避免过度轮询，短暂等待
            await Task.Delay(100, cancellationToken);
        }
    }

               
}
