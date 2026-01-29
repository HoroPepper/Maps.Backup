using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Models
{
    public class TaskNodeResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;

        public object ResultData { get; set; }
    }
}
