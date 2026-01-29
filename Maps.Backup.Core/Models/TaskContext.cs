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

        public Dictionary<string, TaskNodeResult> NodeReusltList = new Dictionary<string, TaskNodeResult>(); 
    }
}
