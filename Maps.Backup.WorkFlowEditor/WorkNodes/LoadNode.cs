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
    public class LoadNode : IWorkTaskNode
    {   
            
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string TaskType { get; set; }

        private readonly List<string> _loadKeys = new List<string>();

        private readonly IKeyValueProvider _keyValueProvider;

        public LoadNode(List<string> loadKeys, IKeyValueProvider keyValueProvider)
        {
            TaskType = TaskNodeType.Load;
            _loadKeys = loadKeys ?? new List<string>();
            _keyValueProvider = keyValueProvider;
        }

        public TaskNodeResult Execute(TaskContext context)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach(var key in _loadKeys) 
            {
                string v = _keyValueProvider.Get(key);
                if(string.IsNullOrEmpty(v))
                {
                    result.Add(key, v);
                }
            }

            foreach(var key in result.Keys) 
            {
                context.ContextDic.Add(key, result[key]);
            }

            return new TaskNodeResult()
            {
                ResultData = result,
                IsSuccess = true,
                Message = "变量添加成功",

            };
        }
    }
}
