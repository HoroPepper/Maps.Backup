using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.WorkFlowEditor.WorkNodes
{
    public class SQLExecuteNode : IWorkTaskNode
    {
        public string TaskId {  get; set; }
        public string TaskName { get; set; }
        public string TaskType { get; set; }

        public string _sql;

        public SQLExecuteNode(string sql)
        {
            _sql = sql;
        }
        public TaskNodeResult Execute(TaskContext context)
        {
            return new TaskNodeResult()
            {
                IsSuccess = true,
                ResultData = null,
                Message = $"{_sql} 执行成功",
            };
        }
    }
}
