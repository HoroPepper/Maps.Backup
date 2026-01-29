using Maps.Backup.Core;
using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.Core.TaskNodes;
using Renci.SshNet.Messages;

namespace Maps.Backup.Shell
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool isQuit = false;
            IFileService backUpFileService = new WinSharedFileService();
            IFileService dbFileService = new SSHFileService("111");
            IShellClient shellClient = new RemotePGShellClient("1", 1, "1", "1", "1", "1");
            IBackupService backupService = new PGBackupService(shellClient);

            while(!isQuit)
            {
                string backUpDir = Console.ReadLine();
                string localSaveDir = Console.ReadLine();
                string dbFileSaveDir = Console.ReadLine();
                string targetDbName = Console.ReadLine();


                IWorkTaskNode downLoadNode = new DelegateTaskNode()
                {
                    TaskId = "backup-down",
                    TaskName = "下载Backup文件",
                    TaskType = "download",
                    DelegateFunc = (context) =>
                    {
                        var downloadFiles = DownloadBackUpFiles(backUpFileService, backUpDir, localSaveDir);
                        if (downloadFiles != null && downloadFiles.Any())
                        {
                            return new TaskNodeResult()
                            {
                                ResultData = downloadFiles,
                                IsSuccess = true,
                                Message = "backup文件下载成功",
                            };
                        }
                        else
                        {
                            return new TaskNodeResult()
                            {
                                ResultData = null,
                                IsSuccess = false,
                                Message = "backup文件下载失败",
                            };
                        }
                    }
                };

                IWorkTaskNode unZipNode = new DelegateTaskNode()
                {
                    TaskId = "backup-unzip",
                    TaskName = "解压Backup文件",
                    TaskType = "unzip",
                    DelegateFunc = (context) =>
                    {
                        if(context.NodeResultList.TryGetValue("backup-down",out TaskNodeResult nodeResult))
                        {
                            if(nodeResult?.ResultData is List<IFile> files)
                            {
                                List<IFile> unzipFiles = new List<IFile>();
                                foreach(var file in files)
                                {
                                    if(file.FileType == "zip")
                                    {
                                        backUpFileService.Unzip(file, new LocalFile(localSaveDir));
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
                            Message = "backup文件解压失败",
                        };
                    }
                };

                IWorkTaskNode upLoadNode = new DelegateTaskNode()
                {
                    TaskId = "backup-upload",
                    TaskName = "上传Backup文件",
                    TaskType = "download",
                    DelegateFunc = (context) =>
                    {
                        List<IFile> upLoadFiles = new List<IFile>();
                        if (context.NodeResultList.TryGetValue("backup-down", out TaskNodeResult nodeResult)&& nodeResult?.ResultData is List<IFile> files)
                        {
                            upLoadFiles.AddRange(files.Where(x => x.FileType == "dmp"));
                        }
                        if (context.NodeResultList.TryGetValue("backup-unzip", out TaskNodeResult nodeResult2) && nodeResult2?.ResultData is List<IFile> files2)
                        {
                            upLoadFiles.AddRange(files2.Where(x => x.FileType == "dmp"));
                        }
                        var uploadResult = UploadBackUpFiles(dbFileService, dbFileSaveDir, upLoadFiles);
                        return new TaskNodeResult()
                        {
                            ResultData = uploadResult,
                            IsSuccess = true,
                            Message = "backup文件下载成功",
                        };
                    }
                };

                IWorkTaskNode restoreNode = new DelegateTaskNode()
                {
                    TaskId = "backup-restore",
                    TaskName = "恢复Backup文件",
                    TaskType = "db-restore",
                    DelegateFunc = (context) =>
                    {
                        if (context.NodeResultList.TryGetValue("backup-upload", out TaskNodeResult nodeResult) && nodeResult?.ResultData is List<IFile> files)
                        {
                            foreach(var file in files) 
                            {
                                backupService.Restore(targetDbName,file);
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
                            Message = "backup文件恢复成功",
                        };

                    }
                };


                TaskManager taskManager = new TaskManager();
                taskManager.AddTaskNode(downLoadNode);
                taskManager.AddTaskNode(unZipNode);
                taskManager.AddTaskNode(upLoadNode);
                taskManager.AddTaskNode(restoreNode);


                taskManager.ExecuteAllTasks(null);


                if(Console.ReadLine() == "quit")
                {
                    isQuit = true;
                }

            }
        }


        private static List<IFile> DownloadBackUpFiles(IFileService fileService, string backUpDir, string localSaveDir)
        {
            List<IFile> targetBackUpFiles = new List<IFile>();
            targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
            {
                RootPath = backUpDir,
                FileType = "dmp",
                IsRecursive = true,
            }));
            targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
            {
                RootPath = backUpDir,
                FileType = "zip",
                IsRecursive = true,
            }));
            targetBackUpFiles.AddRange(fileService.FindFile(new FileSearchParam()
            {
                RootPath = backUpDir,
                FileType = "backup",
                IsRecursive = true,
            }));

            List<IFile> downLoadFiles = new List<IFile>();

            foreach (var file in targetBackUpFiles)
            {
                downLoadFiles.Add(fileService.Download(file, new LocalFile(localSaveDir)));
            }

            return downLoadFiles;
        }

        private static List<IFile> UploadBackUpFiles(IFileService fileService, string targetDir, List<IFile> uploadFiles)
        {
            List<IFile> result = new List<IFile>();
            foreach (var file in uploadFiles.Where(x => x.FileType == "dmp"))
            {
                result.Add(fileService.Upload(file,new LocalFile(targetDir)));
            }
            return result;
        }
    }
}
