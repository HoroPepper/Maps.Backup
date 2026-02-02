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
    public class UnZipNode : IWorkTaskNode
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string TaskType { get; set; }

        private readonly IFile _targetFile;
        private readonly IFile _sourceFile;
        private readonly IZipService _zipService;


        public UnZipNode(IFile sourceFile, IFile targetFile, IZipService zipService)
        {
            TaskType = TaskNodeType.UnZip;

            _sourceFile = sourceFile;
            _targetFile = targetFile;

            _zipService = zipService;
        }

        public TaskNodeResult Execute(TaskContext context)
        {
            var result = _zipService.Unzip(_sourceFile, _targetFile);
            if (result == null)
            {
                return new TaskNodeResult()
                {
                    IsSuccess = false,
                    ResultData = null,
                    Message = $"{_sourceFile?.Path} 解压失败"
                };
            }

            return new TaskNodeResult()
            {
                IsSuccess = true,
                ResultData = result,
                Message = $"{_sourceFile?.Path} 解压成功"
            };
        }
    }
}
