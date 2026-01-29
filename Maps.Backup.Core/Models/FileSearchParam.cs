using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Models
{
    public class FileSearchParam
    {
        public string RootPath { get; set; }

        public string FullName { get; set; }
        
        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public string FileType { get; set; }

        public bool IsRecursive { get; set; }
    }
}
