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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Maps.Backup.WorkFlowLib
{
    public class BackUpWorkFlowCreater
    {
        private readonly IMessagePub<string> _messagePub;
        public BackUpWorkFlowCreater(IMessagePub<string> messagePub)
        {
            _messagePub = messagePub;
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

        public TaskFlow Create()
        {
            TaskFlow taskFlow = new TaskFlow();

            taskFlow.AddTaskNode(CreateBackupDownTaskNode());
            taskFlow.AddTaskNode(CreateUnZipTaskNode());
            taskFlow.AddTaskNode(CreateBackupUpTaskNode());
            taskFlow.AddTaskNode(CreateDBCreateTaskNode());
            taskFlow.AddTaskNode(CreateBackupRestoreTaskNode());
            taskFlow.AddTaskNode(CreateDevBackupRestoreTaskNode());
            taskFlow.AddTaskNode(CreateCustomerFieldUpdateTaskNode());

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


        private List<IFile> DownloadBackUpFiles(IFileService fileService, string backUpDir, string localSaveDir)
        {
            List<IFile> targetBackUpFiles = new List<IFile>();
            targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
            {
                RootPath = backUpDir,
                FileType = ".dump",
                IsRecursive = true,
            }));
            targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
            {
                RootPath = backUpDir,
                FileType = ".zip",
                IsRecursive = true,
            }));
            targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
            {
                RootPath = backUpDir,
                FileType = ".backup",
                IsRecursive = true,
            }));
            targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
            {
                FullPath = backUpDir,
            }).Where(x => !x.IsDirectory));

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
                    upLoadFiles.AddRange(localFileService.FindFile(new FileSearchParam()
                    {
                        RootPath = localSaveDir,
                        FileType = ".dump",
                        IsRecursive = true,
                    }));
                    upLoadFiles.AddRange(localFileService.FindFile(new FileSearchParam()
                    {
                        RootPath = localSaveDir,
                        FileType = ".backup",
                        IsRecursive = true,
                    }));

                    List<IFile> result = new List<IFile>();
                    foreach (var file in upLoadFiles)
                    {
                        result.Add(dbFileService.Upload(file, new SFTPFile(dbFileSaveDir, false)));
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
                    if (context.NodeResultList.TryGetValue("backup-upload", out TaskNodeResult nodeResult) && nodeResult?.ResultData is List<IFile> files)
                    {
                        string dbFileSaveDir = context.ContextDic[ContextKeyDbFileSaveDir];
                        string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                        string sshIP = context.ContextDic[ContextKeySshIP];
                        string ssUName = context.ContextDic[ContextKeySshUName];
                        string ssPwd = context.ContextDic[ContextKeySshPwd];
                        string dbUName = context.ContextDic[ContextKeyDbUName];
                        string dbPwd = context.ContextDic[ContextKeydbPwd];
                        IShellClient shellClient = new RemotePGBatShellClient(sshIP, 22, ssUName, ssPwd, dbUName, dbPwd, "", "");
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

                    return new TaskNodeResult()
                    {
                        ResultData = null,
                        IsSuccess = false,
                        Message = "Failed to restore backup files",
                    };

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
                    IShellClient shellClient = new RemotePGBatShellClient(sshIP, 22, ssUName, ssPwd, dbUName, dbPwd, "", "");
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
                            bool isK900002Exist = false;
                            while (!isK900002Exist)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }
                                string schemaSql = @"SELECT schema_name 
                                   FROM information_schema.schemata 
                                   WHERE catalog_name = (SELECT current_database())
                                    AND schema_name = 'k900002'
                                   ORDER BY schema_name;";
                                var qResult = conn.Query<string>(schemaSql).ToList();
                                if (qResult != null && qResult.Count > 0)
                                {
                                    isK900002Exist = true;
                                }
                                else
                                {
                                    Thread.Sleep(5000);
                                }
                            }
                            string dropSchemaSql = "DROP SCHEMA K900002 CASCADE;";
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
                    string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                    string sshIP = context.ContextDic[ContextKeySshIP];
                    string ssUName = context.ContextDic[ContextKeySshUName];
                    string ssPwd = context.ContextDic[ContextKeySshPwd];
                    string devBackup = context.ContextDic[ContextKeyDevBackup];
                    string dbUName = context.ContextDic[ContextKeyDbUName];
                    string dbPwd = context.ContextDic[ContextKeydbPwd];
                    IShellClient shellClient = new RemotePGBatShellClient(sshIP, 22, ssUName, ssPwd, dbUName, dbPwd, "", "");
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
                    var targetKSchema = schema_nameList.FirstOrDefault(x => x.StartsWith("k") && x.Substring(1) != "900002");
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
                        conn.Open();
                        string executeSql = fileContent.Replace("{customerSeq}", targetCustomerSeq);
                        executeSql = executeSql.Replace("{dbName}", targetDbName);
                        var qResult = conn.Execute(executeSql, commandTimeout: 0);
                        if (qResult >= 0)
                        {
                            return new TaskNodeResult()
                            {
                                ResultData = null,
                                IsSuccess = true,
                                Message = "Fields updated successfully",
                            };
                        }
                        else
                        {
                            return new TaskNodeResult()
                            {
                                ResultData = null,
                                IsSuccess = false,
                                Message = "Field update failed",
                            };
                        }

                    }


                }
            };

            return restoreNode;
        }
    }
}