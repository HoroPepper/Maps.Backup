using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.WorkFlowEditor.CreateParam
{
    public class WorkNodeCreateParam
    {
        public string TaskName { get; set; }

        public string TaskType { get; set; }

        public List<RefNodeParam> RefNodes { get; set; } = new List<RefNodeParam>();

        public List<RefDicVarParam> RefDicVars { get; set; } = new List<RefDicVarParam> { };

        public string RemoteFileServiceType { get; set; }

        public string TargetFilePath { get; set; }

        public string SourceFilePath { get; set; }

        public string Command { get; set; }

        public string IsCommandReplaceEnvValue { get;set; }

        public Dictionary<string , string> Extensions { get; set; }
    }
}
