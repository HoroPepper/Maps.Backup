using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Test
{
    internal class TaskNodeMock : IWorkTaskNode
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string TaskType { get; set; }

        public Action Action { get; set; }

        public TaskNodeResult Execute(object param, TaskContext context)
        {
            Action?.Invoke();
            return new TaskNodeResult
            {
                IsSuccess = true,
                Message = $"Task {TaskName} executed successfully.",
                ResultData = null
            };
        }
    }
}
