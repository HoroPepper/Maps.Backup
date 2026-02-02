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
    public class DownloadNode : IWorkTaskNode
    {
        public string TaskId { get; set; }  
        public string TaskName { get; set; }
        public string TaskType { get; set; }

        private readonly IFile _targetFile;
        private readonly IFile _sourceFile;
        private readonly IFileService _fileService;


        public DownloadNode(IFile sourceFile, IFile targetFile, IFileService fileService) 
        {
            TaskType = TaskNodeType.Download;

            _sourceFile = sourceFile;
            _targetFile = targetFile;

            _fileService = fileService;
        }

        public TaskNodeResult Execute(TaskContext context)
        {
            var result = _fileService.Download(_sourceFile, _targetFile);
            if (result == null)
            {
                return new TaskNodeResult()
                {
                    IsSuccess = false,
                    ResultData = null,
                    Message = $"{_sourceFile?.Path} 下载失败"
                };
            }

            return new TaskNodeResult()
            {
                IsSuccess = true,
                ResultData = result,
                Message = $"{result.Path} 下载成功"
            };
        }
    }
}
