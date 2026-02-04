using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Shell
{
    public class ShellCommand
    {
        public string MainCommand { get; set; }
        public Dictionary<string, string> KvParams { get; set; } = new Dictionary<string, string>();
    }
}
