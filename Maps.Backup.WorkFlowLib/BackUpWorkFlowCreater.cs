using Maps.Backup.Core;
using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.Core.TaskNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.WorkFlowLib
{
    public class BackUpWorkFlowCreater
    {

        public readonly string ContextKeyBackUpDir = "backUpDir";
        public readonly string ContextKeyLocalSaveDir = "localSaveDir";
        public readonly string ContextKeyDbFileSaveDir = "dbFileSaveDir";
        public readonly string ContextKeyTargetDbName = "targetDbName";
        public readonly string ContextKeySshIP = "sshIP";
        public readonly string ContextKeySshUName = "sshUName";
        public readonly string ContextKeySshPwd = "sshPwd";

        public TaskManager Create()
        {
            TaskManager taskManager = new TaskManager();

            taskManager.AddTaskNode(CreateBackupDownTaskNode());
            taskManager.AddTaskNode(CreateUnZipTaskNode());
            taskManager.AddTaskNode(CreateBackupUpTaskNode());
            //taskManager.AddTaskNode(CreateBackupRestoreTaskNode());

            return taskManager;

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
                        result.Add(dbFileService.Upload(file, new SFTPFile(dbFileSaveDir)));
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
                    if (context.NodeResultList.TryGetValue("backup-upload", out TaskNodeResult nodeResult) && nodeResult?.ResultData is List<IFile> files)
                    {
                        string dbFileSaveDir = context.ContextDic[ContextKeyDbFileSaveDir];
                        string targetDbName = context.ContextDic[ContextKeyTargetDbName];
                        IShellClient shellClient = new RemotePGShellClient("1", 1, "1", "1", "1", "1");
                        IBackupService backupService = new PGBackupService(shellClient);
                        foreach (var file in files)
                        {
                            backupService.Restore(targetDbName, file);
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
    }
}
