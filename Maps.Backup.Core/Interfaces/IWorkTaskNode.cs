using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IWorkTaskNode
    {
        /// <summary>
        /// 任务ID，用于唯一标识任务
        /// </summary>
        string TaskId { get; set; } 

        /// <summary>
        /// 任务名称
        /// </summary>
        string TaskName { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        string TaskType { get; set; }

        /// <summary>
        /// 任务执行
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        TaskNodeResult Execute(TaskContext context);
    }
}
