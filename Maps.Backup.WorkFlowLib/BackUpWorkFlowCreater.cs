using Dapper;
using Maps.Backup.Core;
using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.Core.TaskNodes;
using Npgsql;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Maps.Backup.WorkFlowLib
{
    public class BackUpWorkFlowCreater
    {
        private readonly IMessagePub<string> _messagePub;
        private readonly bool _isEmailNotify;
        public BackUpWorkFlowCreater(IMessagePub<string> messagePub, bool isEmailNotify)
        {
            _messagePub = messagePub;
            _isEmailNotify = isEmailNotify;

            RequiredKeys = new List<string>()
            {
                ContextKeyBackUpDir,
                ContextKeyLocalSaveDir,
                ContextKeyDbFileSaveDir,
                ContextKeyTargetDbName,
                ContextKeyDevBackup,
                ContextKeySshIP,
                ContextKeySshUName,
                ContextKeySshPwd,
                ContextKeyDbUName,
                ContextKeydbPwd,
                ContextKeydbIP,
                ContextKeySqlPath,
                ContextKeyBatTempDir,
                ContextDevCustomerSeq,
            };
            if(_isEmailNotify)
            {
                RequiredKeys.AddRange(EmailRequiredKeys);
            }
        }

        public readonly string ContextKeyBackUpDir = "backUpDir";
        public readonly string ContextKeyLocalSaveDir = "localSaveDir";
        public readonly string ContextKeyDbFileSaveDir = "dbFileSaveDir";
        public readonly string ContextKeyTargetDbName = "targetDbName";
        public readonly string ContextKeyDevBackup = "devBackup";
        public readonly string ContextKeySshIP = "sshIP";
        public readonly string ContextKeySshUName = "sshUName";
        public readonly string ContextKeySshPwd = "sshPwd";
        public readonly string ContextKeyDbUName = "dbUName";
        public readonly string ContextKeydbPwd = "dbPwd";
        public readonly string ContextKeydbIP = "dbIP";
        public readonly string ContextKeySqlPath = "sqlPath";
        public readonly string ContextKeyBatTempDir = "batTempDir";
        public readonly string ContextKeySmtpServer = "smtpServer";    
        public readonly string ContextKeySmtpPort = "smtpPort";       
        public readonly string ContextKeySmtpUser = "smtpUser";       
        public readonly string ContextKeySmtpPwd = "smtpPwd";         
        public readonly string ContextKeyRecvEmails = "recvEmails";    
        public readonly string ContextKeyEmailSubject = "emailSubject";
        public readonly string ContextDevCustomerSeq = "devCustomerSeq";
        public readonly string ContextSucceedNotifyEmails = "succeedNotifyEmails";

        public List<string> RequiredKeys { get; set; } = new List<string>();

        private List<string> EmailRequiredKeys => new List<string>()
        {
            ContextKeySmtpServer,
            ContextKeySmtpPort,
            ContextKeySmtpUser,
            ContextKeySmtpPwd,
            ContextKeyRecvEmails,
        };
        public TaskFlow Create()
        {
            TaskFlow taskFlow = new TaskFlow();

            taskFlow.AddTaskNode(CreateBackupDownTaskNode());
            taskFlow.AddTaskNode(CreateUnZipTaskNode());
            taskFlow.AddTaskNode(CreateSearchBackFilesTaskNode());
            taskFlow.AddTaskNode(CreateBackupUpTaskNode());
            taskFlow.AddTaskNode(CreateDBCreateTaskNode());
            taskFlow.AddTaskNode(CreateBackupRestoreTaskNode());
            taskFlow.AddTaskNode(CreateDevBackupRestoreTaskNode());
            taskFlow.AddTaskNode(CreateCustomerFieldUpdateTaskNode());
            if(_isEmailNotify)
            {
                taskFlow.AddTaskNode(CreateEmailSendTaskNode());
            }

            taskFlow.BeforeTaskNodeExecuted += (context, node) =>
            {
                _messagePub.Publish($"Start executing task node [{node.TaskName}]...");
            };

            taskFlow.AfterTaskNodeExecuted += (context) =>
            {
                if (context.LastTaskNode != null && context.LastTaskResult != null)
                {
                    _messagePub.Publish($"Task node [{context.LastTaskNode}] execution completed, result: {(context.LastTaskResult.IsSuccess ? "Success" : "Failed")}, message: {context.LastTaskResult.Message}");
                }
            };
            return taskFlow;

        }


        private List<IFile> DownloadBackUpFiles(IFileService fileService, string backUpFilePath, string localSaveDir)
        {
            List<IFile> targetBackUpFiles = new List<IFile>();
            var backupFile = fileService.FindFile(new FileSearchParam()
            {
                FullPath = backUpFilePath,
            }).FirstOrDefault();
            if (backupFile != null)
            {
                if (!backupFile.IsDirectory)
                {
                    targetBackUpFiles.Add(backupFile);
                }
                else
                {
                    targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
                    {
                        RootPath = backupFile.Path,
                        FileType = ".dump",
                        IsRecursive = true,
                    }));
                    targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
                    {
                        RootPath = backupFile.Path,
                        FileType = ".zip",
                        IsRecursive = true,
                    }));
                    targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
                    {
                        RootPath = backupFile.Path,
                        FileType = ".backup",
                        IsRecursive = true,
                    }));
                }
            }
            
            List<IFile> downLoadFiles = new List<IFile>();

            foreach (var file in targetBackUpFiles)
            {
                downLoadFiles.Add(fileService.Download(file, new LocalFile(localSaveDir)));
            }

            return downLoadFiles;
        }

        private IWorkTaskNode CreateBackupDownTaskNode()
        {
            IWorkTaskNode downLoadNode = new DelegateTaskNode()
            {
                TaskId = "backup-down",
                TaskName = "Download Backup Files",
                TaskType = "download",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    IFileService backUpFileService = new WinSharedFileService();
                    string backUpDir = context.ContextDic[ContextKeyBackUpDir];
                    string localSaveDir = context.ContextDic[ContextKeyLocalSaveDir];
                    var downloadFiles = DownloadBackUpFiles(backUpFileService, backUpDir, localSaveDir);
                    if(downloadFiles == null || !downloadFiles.Any())
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = $"No found files to download",
                        };
                    }

                    return new TaskNodeResult()
                    {
                        ResultData = downloadFiles,
                        IsSuccess = true,
                        Message = $"Backup {downloadFiles.Count} files downloaded successfully",
                    };
                }
            };

            return downLoadNode;
        }

        private IWorkTaskNode CreateUnZipTaskNode()
        {
            IWorkTaskNode unZipNode = new DelegateTaskNode()
            {
                TaskId = "backup-unzip",
                TaskName = "Unzip Backup Files",
                TaskType = "unzip",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    if (context.NodeResultList.TryGetValue("backup-down", out TaskNodeResult nodeResult))
                    {
                        IZipService zipService = new ZipFileService();
                        string localSaveDir = context.ContextDic[ContextKeyLocalSaveDir];
                        if (nodeResult?.ResultData is List<IFile> files)
                        {
                            List<IFile> unzipFiles = new List<IFile>();
                            foreach (var file in files)
                            {
                                if (file.FileType == ".zip" || file.FileType == ".rar")
                                {
                                    unzipFiles.AddRange(zipService.Unzip(file, new LocalFile(localSaveDir)));
                                }
                            }
                            return new TaskNodeResult()
                            {
                                ResultData = unzipFiles,
                                IsSuccess = true,
                                Message = $"Backup {unzipFiles.Count} files unzipped successfully",
                            };
                        }
                    }

                    return new TaskNodeResult()
                    {
                        ResultData = null,
                        IsSuccess = false,
                        Message = "Files from previous download node are missing",
                    };
                }
            };

            return unZipNode;
        }

        private IWorkTaskNode CreateSearchBackFilesTaskNode()
        {
            IWorkTaskNode unZipNode = new DelegateTaskNode()
            {
                TaskId = "backup-search",
                TaskName = "Search Backup Files",
                TaskType = "search",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    List<IFile> backupFiles = new List<IFile>();
                    IFileService localFileService = new LocalFileService();
                    if (context.NodeResultList.TryGetValue("backup-down", out TaskNodeResult downResult) && downResult.ResultData is List<IFile> downFiles)
                    {
                        backupFiles.AddRange(downFiles.Where(x => x.FileType == ".dump" || x.FileType == ".backup"));
                    }
                    if (context.NodeResultList.TryGetValue("backup-unzip", out TaskNodeResult unzipResult) && unzipResult.ResultData is List<IFile> unzipFiles)
                    {
                        backupFiles.AddRange(unzipFiles.Where(x => x.FileType == ".dump" || x.FileType == ".backup"));
                    }

                    return new TaskNodeResult()
                    {
                        ResultData = backupFiles,
                        IsSuccess = true,
                        Message = "Backup files sum search successfully",
                    };
                }
            };

            return unZipNode;
        }


        private IWorkTaskNode CreateBackupUpTaskNode()
        {
            IWorkTaskNode upLoadNode = new DelegateTaskNode()
            {
                TaskId = "backup-upload",
                TaskName = "Upload Backup Files",
                TaskType = "upload",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    string localSaveDir = context.ContextDic[ContextKeyLocalSaveDir];
                    string dbFileSaveDir = context.ContextDic[ContextKeyDbFileSaveDir];
                    string sshIP = context.ContextDic[ContextKeySshIP];
                    string ssUName = context.ContextDic[ContextKeySshUName];
                    string ssPwd = context.ContextDic[ContextKeySshPwd];
                    List<IFile> upLoadFiles = new List<IFile>();
                    IFileService localFileService = new LocalFileService();
                    IFileService dbFileService = new SSHFileService(sshIP, 22, ssUName, ssPwd);
                    if (context.NodeResultList.TryGetValue("backup-search", out TaskNodeResult downResult) &&  downResult.ResultData is List<IFile> backFiles)
                    {
                        upLoadFiles.AddRange(backFiles);
                    }
                    List<IFile> result = new List<IFile>();
                    foreach (var file in upLoadFiles)
                    {
                        result.Add(dbFileService.Upload(file, new SFTPFile(dbFileSaveDir, false)));
                    }
                    if(!result.Any())
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = result,
                            IsSuccess = false,
                            Message = $"No Files to Upload",
                        };
                    }

                    return new TaskNodeResult()
                    {
                        ResultData = result,
                        IsSuccess = true,
                        Message = $"Backup {result.Count} files uploaded successfully",
                    };
                }
            };

            return upLoadNode;
        }

        private IWorkTaskNode CreateBackupRestoreTaskNode()
        {
            IWorkTaskNode restoreNode = new DelegateTaskNode()
            {
                TaskId = "backup-restore",
                TaskName = "Restore Backup Files",
                TaskType = "db-restore",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    if (context.NodeResultList.TryGetValue("backup-upload", out TaskNodeResult nodeResult) && nodeResult?.ResultData is List<IFile> files && files.Any())
                    {
                        string dbFileSaveDir = context.ContextDic[ContextKeyDbFileSaveDir];
                        string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                        string sshIP = context.ContextDic[ContextKeySshIP];
                        string ssUName = context.ContextDic[ContextKeySshUName];
                        string ssPwd = context.ContextDic[ContextKeySshPwd];
                        string dbUName = context.ContextDic[ContextKeyDbUName];
                        string dbPwd = context.ContextDic[ContextKeydbPwd];
                        string tempDir = context.ContextDic[ContextKeyBatTempDir];
                        IShellClient shellClient = new RemotePGBatShellClient(sshIP, 22, ssUName, ssPwd, dbUName, dbPwd, tempDir, "");
                        IBackupService backupService = new PGBackupService(shellClient);
                        foreach (var file in files)
                        {
                            backupService.Restore(targetDbName, "", file);
                        }

                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = true,
                            Message = "Backup files restored successfully",
                        };
                    }
                    else
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Failed to find restore backup files",
                        };
                    }

                }
            };

            return restoreNode;
        }

        private IWorkTaskNode CreateDevBackupRestoreTaskNode()
        {
            IWorkTaskNode restoreNode = new DelegateTaskNode()
            {
                TaskId = "backup-devRestore",
                TaskName = "Restore Development Environment Backup Files",
                TaskType = "db-restore",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                    string sshIP = context.ContextDic[ContextKeySshIP];
                    string ssUName = context.ContextDic[ContextKeySshUName];
                    string ssPwd = context.ContextDic[ContextKeySshPwd];
                    string devBackup = context.ContextDic[ContextKeyDevBackup];
                    string dbUName = context.ContextDic[ContextKeyDbUName];
                    string dbPwd = context.ContextDic[ContextKeydbPwd];
                    string dbIP = context.ContextDic[ContextKeydbIP];
                    string connStr = $"Host={dbIP};Port=5432;Database={targetDbName};Username={dbUName};Password={dbPwd};";
                    string tempDir = context.ContextDic[ContextKeyBatTempDir];
                    string devCustomerSeq = context.ContextDic[ContextDevCustomerSeq];
                    IShellClient shellClient = new RemotePGBatShellClient(sshIP, 22, ssUName, ssPwd, dbUName, dbPwd, tempDir, "");
                    IBackupService backupService = new PGBackupService(shellClient);
                    IFileService fileService = new SSHFileService(sshIP, 22, ssUName, ssPwd);
                    List<IFile> files = fileService.FindFile(new FileSearchParam()
                    {
                        FullPath = devBackup
                    });

                    using var cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = cts.Token;
                    Task.Run(() => {

                        using (var conn = new NpgsqlConnection(connStr))
                        {
                            conn.Open();
                            bool isDevKSchemaExist = false;
                            while (!isDevKSchemaExist)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }
                                string schemaSql = $@"SELECT schema_name 
                                   FROM information_schema.schemata 
                                   WHERE catalog_name = (SELECT current_database())
                                    AND schema_name = 'k{devCustomerSeq}'
                                   ORDER BY schema_name;";
                                var qResult = conn.Query<string>(schemaSql).ToList();
                                if (qResult != null && qResult.Count > 0)
                                {
                                    isDevKSchemaExist = true;
                                }
                                else
                                {
                                    Thread.Sleep(5000);
                                }
                            }
                            string dropSchemaSql = $"DROP SCHEMA k{devCustomerSeq} CASCADE;";
                            conn.Execute(dropSchemaSql);
                        }
                    }, cancellationToken);
                    try
                    {
                        foreach (var file in files)
                        {
                            backupService.Restore(targetDbName, "", file);
                        }

                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = true,
                            Message = "Dev backup files restored successfully",
                        };
                    }
                    finally
                    {
                        if (!cts.IsCancellationRequested)
                        {
                            cts.Cancel();
                        }
                    }


                }
            };

            return restoreNode;
        }

        private IWorkTaskNode CreateDBCreateTaskNode()
        {
            IWorkTaskNode restoreNode = new DelegateTaskNode()
            {
                TaskId = "db_create",
                TaskName = "Create Database",
                TaskType = "db-create",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                    string sshIP = context.ContextDic[ContextKeySshIP];
                    string ssUName = context.ContextDic[ContextKeySshUName];
                    string ssPwd = context.ContextDic[ContextKeySshPwd];
                    string devBackup = context.ContextDic[ContextKeyDevBackup];
                    string dbUName = context.ContextDic[ContextKeyDbUName];
                    string dbPwd = context.ContextDic[ContextKeydbPwd];
                    string tempDir = context.ContextDic[ContextKeyBatTempDir];
                    IShellClient shellClient = new RemotePGBatShellClient(sshIP, 22, ssUName, ssPwd, dbUName, dbPwd, tempDir, "");
                    var result = shellClient.Execute($@"set PGPASSWORD={dbPwd}
                    createdb -h localhost -p 5432 -U {dbUName} -w {targetDbName} ");
                    if (result.IsSuccess || result.StandardError.Contains("already exists"))
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = true,
                            Message = $"Database {targetDbName} created successfully",
                        };
                    }
                    else
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = $"Failed to create database {targetDbName}, error message: {result.StandardError}",
                        };
                    }

                }
            };

            return restoreNode;
        }

        private IWorkTaskNode CreateCustomerFieldUpdateTaskNode()
        {
            IWorkTaskNode restoreNode = new DelegateTaskNode()
            {
                TaskId = "backup-customerUpdate",
                TaskName = "Update Fields",
                TaskType = "db-sql",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Upstream task failed",
                        };
                    }
                    string sqlPath = context.ContextDic[ContextKeySqlPath];
                    IFileService localFileService = new LocalFileService();
                    var sqlFile = localFileService.FindFile(new FileSearchParam()
                    {
                        FullPath = sqlPath,
                    }).FirstOrDefault();
                    if (sqlFile == null)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "SQL file not found",
                        };
                    }
                    string fileContent = File.ReadAllText(sqlFile.Path);
                    string dbIP = context.ContextDic[ContextKeydbIP];
                    string dbUName = context.ContextDic[ContextKeyDbUName];
                    string dbPwd = context.ContextDic[ContextKeydbPwd];
                    string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                    string devCustomerSeq = context.ContextDic[ContextDevCustomerSeq];
                    string connStr = $"Host={dbIP};Port=5432;Database={targetDbName};Username={dbUName};Password={dbPwd};";
                    string schemaSql = @"SELECT schema_name 
                           FROM information_schema.schemata 
                           WHERE catalog_name = (SELECT current_database())  -- 仅查询当前连接的数据库
                           ORDER BY schema_name;";
                    List<string> schema_nameList = new List<string>();
                    using (var conn = new NpgsqlConnection(connStr))
                    {
                        conn.Open();
                        var qResult = conn.Query<string>(schemaSql).ToList();
                        if (qResult != null && qResult.Count > 0)
                        {
                            schema_nameList.AddRange(qResult);
                        }
                    }
                    var targetKSchema = schema_nameList.FirstOrDefault(x => x.StartsWith("k") && x.Substring(1) != devCustomerSeq);
                    if (targetKSchema == null)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "Field update failed",
                        };
                    }
                    string targetCustomerSeq = targetKSchema.Substring(1);
                    using (var conn = new NpgsqlConnection(connStr))
                    {
                        string executeSql = fileContent.Replace("{customerSeq}", targetCustomerSeq);
                        executeSql = executeSql.Replace("{dbName}", targetDbName);
                        executeSql = executeSql.Replace("{devCustomerSeq}", devCustomerSeq);
                        List<string> splitedSql = executeSql.Split(';').ToList();
                        int updateCount = 0;
                        List<string> errorMsgList = new List<string>();
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            int savepointIndex = 0;
                            foreach (var sql in splitedSql)
                            {
                                if (string.IsNullOrWhiteSpace(sql))
                                {
                                    continue;
                                }
                                savepointIndex++;
                                string savepointName = $"sp_{savepointIndex}";
                                try
                                {
                                    transaction.Save(savepointName);
                                    var qResult = conn.Execute(sql, commandTimeout: 0, transaction:transaction);
                                    updateCount += qResult;
                                }
                                catch (PostgresException pgEx)
                                {
                                    string postgresErrorCode_TableNotFound = "42P01";
                                    if (pgEx.SqlState == postgresErrorCode_TableNotFound)
                                    {
                                        transaction.Rollback(savepointName);
                                        errorMsgList.Add(pgEx.Message);
                                    }
                                    else
                                    {
                                        throw pgEx;
                                    }
                                }
                            }
                            transaction.Commit();
                        }       
                        if (updateCount > 0)
                        {
                            string message = $"Fields updated successfully, Count:{updateCount}";
                            if(errorMsgList.Any())
                            {
                                message += $", but some errors occurred: {string.Join(" | ", errorMsgList)}";
                            }
                            return new TaskNodeResult()
                            {
                                ResultData = null,
                                IsSuccess = true,
                                Message = message,
                            };
                        }
                        else
                        {
                            return new TaskNodeResult()
                            {
                                ResultData = null,
                                IsSuccess = false,
                                Message = $"Field update failed : {string.Join(" | ", errorMsgList)}",
                            };
                        }

                    }


                }
            };

            return restoreNode;
        }

        /// <summary>
        /// 创建邮箱发送任务节点（工作流末尾执行，不依赖前序节点结果）
        /// </summary>
        /// <returns>邮箱发送任务节点</returns>
        private IWorkTaskNode CreateEmailSendTaskNode()
        {
            IWorkTaskNode emailNode = new DelegateTaskNode()
            {
                TaskId = "workflow-email-send",
                TaskName = "Send Workflow Summary Email",
                TaskType = "email-notify",
                DelegateFunc = (context) =>
                {
                    try
                    {
                        string smtpServer = context.ContextDic[ContextKeySmtpServer];
                        int smtpPort = int.Parse(context.ContextDic[ContextKeySmtpPort]);
                        string smtpUser = context.ContextDic[ContextKeySmtpUser];
                        string smtpPwd = context.ContextDic[ContextKeySmtpPwd];
                        string recvEmails = context.ContextDic[ContextKeyRecvEmails];
                        string emailSubject = context.ContextDic.ContainsKey(ContextKeyEmailSubject)
                            ? context.ContextDic[ContextKeyEmailSubject]
                            : $"【备份工作流】执行摘要 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                        string succeedNotifyEmails = context.ContextDic.ContainsKey(ContextSucceedNotifyEmails) ?
                            context.ContextDic[ContextSucceedNotifyEmails] : string.Empty;

                        // 2. 统计所有任务节点执行结果
                        int totalTask = context.NodeResultList.Count;
                        int successTask = context.NodeResultList.Values.Count(r => r.IsSuccess);
                        int failTask = totalTask - successTask;
                        bool isAllSuccess = failTask == 0;
                        // 工作流整体状态：所有任务成功则为成功，否则失败
                        string workflowStatus = isAllSuccess ? "✅ 执行成功" : "❌ 执行失败（部分节点出错）";

                        // 3. 构建详细的任务执行明细（HTML格式，排版清晰）
                        StringBuilder taskDetailBuilder = new StringBuilder();
                        taskDetailBuilder.Append("<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse;width:100%;'>");
                        taskDetailBuilder.Append("<tr style='background:#f5f5f5;'><th>任务ID</th><th>任务名称</th><th>执行状态</th><th>执行消息</th></tr>");
                        foreach (var kv in context.NodeResultList)
                        {
                            string taskId = kv.Key;
                            TaskNodeResult result = kv.Value;
                            string status = result.IsSuccess ? "<span style='color:green;'>成功</span>" : "<span style='color:red;'>失败</span>";
                            string message = string.IsNullOrWhiteSpace(result.Message) ? "无消息" : result.Message.Replace("\r\n", "<br/>");
                            string taskName = context.Nodes.FirstOrDefault(n => n.TaskId == taskId)?.TaskName ?? "未知任务";
                            taskDetailBuilder.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>",
                                taskId, taskName, status, message);
                        }
                        taskDetailBuilder.Append("</table>");

                        string emailBody = $@"
                        <h3>备份工作流执行摘要</h3>
                        <p><strong>执行时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                        <p><strong>整体状态：</strong>{workflowStatus}</p>
                        <p><strong>任务统计：</strong>总任务{totalTask}个 | 成功{successTask}个 | 失败{failTask}个</p>
                        <h4>任务执行明细：</h4>
                        {taskDetailBuilder.ToString()}
                        <p style='margin-top:20px;color:#666;'>此邮件由系统自动发送，无需回复</p>";
                        List<string> toAddresses = new List<string>();
                        toAddresses.AddRange(GetEmailsFromStr(recvEmails));
                        if(isAllSuccess)
                        {
                            toAddresses.AddRange(GetEmailsFromStr(succeedNotifyEmails));
                        }
                        // 5. 发送邮件
                        var sendSuccess = SendWorkflowSummaryEmail(
                            smtpServer, smtpPort, smtpUser, smtpPwd, toAddresses, emailSubject, emailBody);

                        // 6. 返回任务节点结果
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = sendSuccess,
                        };
                    }
                    catch (Exception ex)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = $"邮箱任务执行异常：{ex.Message}"
                        };
                    }
                }
            };

            return emailNode;
        }

        private List<string> GetEmailsFromStr(string recvEmails)
        {
            List<string> toAddresses = new List<string>();
            if (string.IsNullOrWhiteSpace(recvEmails))
            {
                return toAddresses;
            }

            toAddresses.AddRange(recvEmails.Split(',', ';')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            return toAddresses;
        }

        private bool SendWorkflowSummaryEmail(
        string smtpServer, int smtpPort, string smtpUser, string smtpPwd,
        List<string> toAddresses, string subject, string body)
        {
            if (toAddresses == null || !toAddresses.Any())
            {
                return false;
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser,"Maps.BackupBot"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };

            foreach (var email in toAddresses)
            {
                mailMessage.To.Add(new MailAddress(email));
            }

            using var smtpClient = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPwd),
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true,
                Timeout = 30000
            };

            smtpClient.Send(mailMessage);
            return true;
        }
    }
}