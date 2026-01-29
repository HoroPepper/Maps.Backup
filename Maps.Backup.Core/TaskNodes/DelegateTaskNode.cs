using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.TaskNodes
{
    public class DelegateTaskNode : IWorkTaskNode
    {

        public Func<TaskContext, TaskNodeResult> DelegateFunc { get;set; }

        public string TaskId { get ; set ; }
        public string TaskName { get; set; }
        public string TaskType { get ; set ; }

        public TaskNodeResult Execute(TaskContext context)
        {
            return DelegateFunc?.Invoke(context);
        }
    }
}
