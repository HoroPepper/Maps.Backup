using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.WorkFlowEditor.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.WorkFlowEditor.WorkNodes
{
    public class RestoreNode : IWorkTaskNode
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string TaskType { get; set; }

        private readonly IFile _backupFile;
        private readonly IBackupService _backupService;
        private readonly string _targetDBName;
        private readonly string _targetGroupName;


        public RestoreNode(IFile backupFile, string targetDBName, string targetGroupName, IBackupService backupService)
        {
            TaskType = TaskNodeType.Restore;
            _backupFile = backupFile;
            _backupService = backupService;
            _targetDBName = targetDBName;
            _targetGroupName = targetGroupName;

        }

        public TaskNodeResult Execute(TaskContext context)
        {
            var result = _backupService.Restore(_targetDBName, _targetGroupName, _backupFile);
            return new TaskNodeResult()
            {
                IsSuccess = false,
                ResultData = null,
                Message = $"{_backupFile?.Path} 备份恢复成功"
            };
        }
    }
}
