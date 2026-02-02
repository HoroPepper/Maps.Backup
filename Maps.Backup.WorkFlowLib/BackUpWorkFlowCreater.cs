using Maps.Backup.Core;
using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.Core.TaskNodes;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Maps.Backup.WorkFlowLib
{
    public class BackUpWorkFlowCreater
    {

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
                Console.WriteLine($"开始执行任务节点[{node.TaskName}]...");
            };

            taskFlow.AfterTaskNodeExecuted += (context) =>
            {
                if(context.LastTaskNode != null && context.LastTaskResult != null)
                {
                    Console.WriteLine($"任务节点[{context.LastTaskNode}]执行完成，结果：{(context.LastTaskResult.IsSuccess ? "成功" : "失败")}，信息：{context.LastTaskResult.Message}");
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
                TaskName = "下载Backup文件",
                TaskType = "download",
                DelegateFunc = (context) =>
                {
                    if(context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "上游任务失败",
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
                        Message = "backup文件下载成功",
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
                TaskName = "解压Backup文件",
                TaskType = "unzip",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "上游任务失败",
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
                                Message = "backup文件解压成功",
                            };
                        }
                    }

                    return new TaskNodeResult()
                    {
                        ResultData = null,
                        IsSuccess = false,
                        Message = "前下载节点文件丢失",
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
                TaskName = "上传Backup文件",
                TaskType = "upload",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "上游任务失败",
                        };
                    }
                    string localSaveDir = context.ContextDic[ContextKeyLocalSaveDir];
                    string dbFileSaveDir = context.ContextDic[ContextKeyDbFileSaveDir];
                    string sshIP = context.ContextDic[ContextKeySshIP];
                    string ssUName = context.ContextDic[ContextKeySshUName];
                    string ssPwd = context.ContextDic[ContextKeySshPwd];
                    List<IFile> upLoadFiles = new List<IFile>();
                    IFileService localFileService = new LocalFileService();
                    IFileService dbFileService = new SSHFileService(sshIP,22,ssUName,ssPwd);
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
                        result.Add(dbFileService.Upload(file, new SFTPFile(dbFileSaveDir,false)));
                    }
                    return new TaskNodeResult()
                    {
                        ResultData = result,
                        IsSuccess = true,
                        Message = "backup文件上传成功",
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
                TaskName = "恢复Backup文件",
                TaskType = "db-restore",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "上游任务失败",
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
                            Message = "backup文件恢复成功",
                        };
                    }

                    return new TaskNodeResult()
                    {
                        ResultData = null,
                        IsSuccess = false,
                        Message = "backup文件恢复失败",
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
                TaskName = "恢复开发环境backup文件",
                TaskType = "db-restore",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "上游任务失败",
                        };
                    }
                    string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                    string sshIP = context.ContextDic[ContextKeySshIP];
                    string ssUName = context.ContextDic[ContextKeySshUName];
                    string ssPwd = context.ContextDic[ContextKeySshPwd];
                    string devBackup = context.ContextDic[ContextKeyDevBackup];
                    string dbUName = context.ContextDic[ContextKeyDbUName];
                    string dbPwd = context.ContextDic[ContextKeydbPwd];
                    IShellClient shellClient = new RemotePGBatShellClient(sshIP, 22, ssUName, ssPwd, dbUName, dbPwd, "", "");
                    IBackupService backupService = new PGBackupService(shellClient);
                    IFileService fileService = new SSHFileService(sshIP, 22, ssUName, ssPwd);
                    List<IFile> files = fileService.FindFile(new FileSearchParam()
                    {
                        FullPath = devBackup
                    });
                    foreach (var file in files)
                    {
                        backupService.Restore(targetDbName, "", file);
                    }

                    return new TaskNodeResult()
                    {
                        ResultData = null,
                        IsSuccess = true,
                        Message = "dev-backup文件恢复成功",
                    };

                }
            };

            return restoreNode;
        }

        private IWorkTaskNode CreateDBCreateTaskNode()
        {
            IWorkTaskNode restoreNode = new DelegateTaskNode()
            {
                TaskId = "db_create",
                TaskName = "创建数据库",
                TaskType = "db-create",
                DelegateFunc = (context) =>
                {

                    return new TaskNodeResult()
                    {
                        ResultData = null,
                        IsSuccess = true,
                        Message = "",
                    };

                }
            };

            return restoreNode;
        }

        private IWorkTaskNode CreateCustomerFieldUpdateTaskNode()
        {
            IWorkTaskNode restoreNode = new DelegateTaskNode()
            {
                TaskId = "backup-customerUpdate",
                TaskName = "更新字段",
                TaskType = "db-sql",
                DelegateFunc = (context) =>
                {
                    if (context.LastTaskResult != null && !context.LastTaskResult.IsSuccess)
                    {
                        return new TaskNodeResult()
                        {
                            ResultData = null,
                            IsSuccess = false,
                            Message = "上游任务失败",
                        };
                    }
                    if (context.NodeResultList.TryGetValue("backup-upload", out TaskNodeResult nodeResult) && nodeResult?.ResultData is List<IFile> files)
                    {
                        string sql = context.ContextDic["updateCustomerSQL"];
                        string dbUName = context.ContextDic[ContextKeyDbUName];
                        string dbPwd = context.ContextDic[ContextKeydbPwd];
                        string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                        string targetCustomSeq = "";

                        sql.Replace("{targetCustomSeq}", targetCustomSeq);

                    }

                    return new TaskNodeResult()
                    {
                        ResultData = null,
                        IsSuccess = false,
                        Message = "backup文件恢复失败",
                    };

                }
            };

            return restoreNode;
        }
    }
}
