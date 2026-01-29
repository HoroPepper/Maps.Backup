using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IWorkTaskNode
    {
        string TaskId { get; set; } 

        string TaskName { get; set; }

        string TaskType { get; set; }

        string Execute(object param);
    }
}
