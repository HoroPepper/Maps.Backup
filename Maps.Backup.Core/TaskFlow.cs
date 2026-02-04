using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core
{
    public class TaskFlow
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

        public void ExecuteAllTasks(object param,TaskContext context)
        {
            try
            {
                context.Nodes = GetAllTaskNodes();
                context.FlowState = TaskFlowState.Running;
                foreach (var taskNode in context.Nodes)
                {
                    if (taskNode == null)
                    {
                        continue;
                    }
                    if (context.FlowState == TaskFlowState.Stoped)
                    {
                        break;
                    }
                    BeforeTaskNodeExecuted?.Invoke(context, taskNode);
                    TaskNodeResult taskNodeResult = new TaskNodeResult()
                    {
                        IsSuccess = false,
                        Message = "Untrack Task Failed",
                    };
                    try
                    {

                        taskNodeResult = taskNode.Execute(context);

                    }
                    catch (Exception ex)
                    {
                        taskNodeResult = new TaskNodeResult()
                        {
                            IsSuccess = false,
                            Message = $"任务节点执行异常，任务ID：{taskNode.TaskId}，异常信息：{ex.Message}",
                        };
                    }
                    context.LastTaskNode = taskNode;
                    context.LastTaskResult = taskNodeResult;
                    context.NodeResultList[taskNode.TaskId] = taskNodeResult;
                    AfterTaskNodeExecuted?.Invoke(context);
                }
            }
            catch(Exception ex)
            {
                context.MessagePub?.Publish($"任务流执行异常，异常信息：{ex.Message}");
            }
            finally
            {
                context.FlowState = TaskFlowState.Finished;
            }
        }
    }
}
