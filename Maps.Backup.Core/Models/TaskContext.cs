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
        /// <summary>
        /// 全部任务节点
        /// </summary>
        public List<IWorkTaskNode> Nodes = new List<IWorkTaskNode>();

        /// <summary>
        /// 任务节点执行结果
        /// </summary>
        public Dictionary<string, TaskNodeResult> NodeResultList = new Dictionary<string, TaskNodeResult>();

        /// <summary>
        /// 上下文变量字典
        /// </summary>
        public Dictionary<string, string> ContextDic = new Dictionary<string, string>();

        /// <summary>
        /// 上次执行完成的任务节点
        /// </summary>
        public IWorkTaskNode LastTaskNode { get; set; }

        /// <summary>
        /// 上次执行完成的任务结果
        /// </summary>
        public TaskNodeResult LastTaskResult { get; set; }

        /// <summary>
        /// 任务流状态
        /// </summary>
        public TaskFlowState FlowState { get; set; } = TaskFlowState.NotStarted;

        public IMessagePub<string> MessagePub { get; set; }
    }

    public enum TaskFlowState
    {
        /// <summary>
        /// 未开始
        /// </summary>
        NotStarted,
        /// <summary>
        /// 进行中
        /// </summary>
        Running,
        /// <summary>
        /// 停止
        /// </summary>
        Stoped,
        /// <summary>
        /// 完成
        /// </summary>
        Finished,
    }
}
