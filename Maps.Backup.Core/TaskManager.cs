using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core
{
    public class TaskManager
    {
        private List<IWorkTaskNode> _taskNodes = new List<IWorkTaskNode>();

        public void AddTaskNode(IWorkTaskNode taskNode)
        {
            _taskNodes.Add(taskNode);
        }

        public bool InsertTaskNode(IWorkTaskNode beforeNode, IWorkTaskNode taskNode)
        {
            if(!_taskNodes.Contains(beforeNode))
            {
                return false;
            }
            int index = _taskNodes.IndexOf(beforeNode);
            _taskNodes.Insert(index, taskNode);
            return true;
        }

        public void RemoveTaskNode(IWorkTaskNode taskNode)
        {
            if(_taskNodes.Contains(taskNode))
            {
                _taskNodes.Remove(taskNode);
            }
        }

        public IWorkTaskNode GetTaskNodeById(string taskId)
        {
            return _taskNodes.FirstOrDefault(t => t.TaskId == taskId);
        }

        public List<IWorkTaskNode> GetTaskNodesByType(string taskType)
        {
            return _taskNodes.Where(t => t.TaskType == taskType).ToList();
        }

        public List<IWorkTaskNode> GetAllTaskNodes()
        {
            return new List<IWorkTaskNode>(_taskNodes);
        }

        public void ClearAllTaskNodes()
        {
            _taskNodes.Clear();
        }

        public event Action<TaskContext> AfterTaskNodeExecuted;

        public event Action<TaskContext,IWorkTaskNode> BeforeTaskNodeExecuted;

        public void ExecuteAllTasks(object param)
        {
            TaskContext context = new TaskContext();
            context.Nodes = GetAllTaskNodes();
            foreach (var taskNode in _taskNodes)
            {
                if(taskNode == null)
                {
                    continue;
                }
                BeforeTaskNodeExecuted?.Invoke(context, taskNode);
                var nodeResult = taskNode.Execute(context);
                context.LastTaskNode = taskNode;
                context.LastTaskResult = nodeResult;
                context.NodeResultList[taskNode.TaskId] = nodeResult;
                if (context.LastTaskResult == null || !context.LastTaskResult.IsSuccess)
                {
                    break;
                }
                AfterTaskNodeExecuted?.Invoke(context);
            }
        }
    }
}
