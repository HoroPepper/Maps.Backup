using Maps.Backup.Core;
using Maps.Backup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Test
{
    public class QABackupWorkFlow
    {
        private string _dbPCIP = "";
        private string _dbPCAccount = "";
        private string _dbPCPassword = "";
        private string _dbPCBackupPath = "";

        private string _filePCIP = "";
        private string _fileAccount = "";
        private string _filePCPassword = "";

        private string _localFilePath = "";

        private string _dbAccount = "";
        private string _dbPassword = "";

        private string _devBackupPath = "";



        public void Start(string targetCustomerId)
        {
            IFileService fileService = new FileServiceMock();
            IShellClient shellClient = new ShellClientMock();
            IBackup backup = new BackupMock();

            IWorkTaskNode backupDownloadNode = new TaskNodeMock()
            {
                TaskId = "1",
                TaskName = "Backup Download Task",
                TaskType = "BackupDownload",
                Action = () =>
                {
                    Console.WriteLine($"Downloading backup for customer {targetCustomerId} from {_filePCIP} to {_localFilePath}");
                }
            };

            IWorkTaskNode backupUnZipNode = new TaskNodeMock()
            {
                TaskId = "2",
                TaskName = "Backup UnZip Task",
                TaskType = "BackupUnZip",
                Action = () =>
                {
                    Console.WriteLine($"Unzipping backup at {_localFilePath}");
                }
            };

            IWorkTaskNode backupUpLoadNode = new TaskNodeMock()
            {
                TaskId = "3",
                TaskName = "Backup Upload Task",
                TaskType = "BackupUpload",
                Action = () =>
                {
                    Console.WriteLine($"Uploading backup to dev environment at {_devBackupPath}");
                }
            };

            IWorkTaskNode backupNode = new TaskNodeMock()
            {
                TaskId = "4",
                TaskName = "Backup Restore Task",
                TaskType = "BackupRestore",
                Action = () =>
                {
                    Console.WriteLine($"Restoring backup to dev database at {_dbPCIP}");
                }
            };

            IWorkTaskNode updateFilesNode = new TaskNodeMock()
            {
                TaskId = "5",
                TaskName = "Update Files Task",
                TaskType = "UpdateFiles",
                Action = () =>
                {
                    Console.WriteLine($"Updating files in dev environment from backup at {_devBackupPath}");
                }
            };

            TaskManager taskManager = new TaskManager();
            taskManager.AddTaskNode(backupDownloadNode);
            taskManager.AddTaskNode(backupUnZipNode);
            taskManager.AddTaskNode(backupUpLoadNode);
            taskManager.AddTaskNode(backupNode);
            taskManager.AddTaskNode(updateFilesNode);

            taskManager.ExecuteAllTasks(null);

        }

    }
}
