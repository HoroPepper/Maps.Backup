using Maps.Backup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Models
{
    public class TaskContext
    {
        public List<IWorkTaskNode> Nodes = new List<IWorkTaskNode>();

        public Dictionary<string, TaskNodeResult> NodeResultList = new Dictionary<string, TaskNodeResult>();

        public Dictionary<string, string> ContextDic = new Dictionary<string, string>();

        public IWorkTaskNode LastTaskNode { get; set; }

        public TaskNodeResult LastTaskResult { get; set; }

        public TaskFlowState FlowState { get; set; } = TaskFlowState.NotStarted;
    }

    public enum TaskFlowState
    {
        NotStarted,
        Running,
        Stoped,
        Finished,
    }
}
