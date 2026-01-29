using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Test
{
    internal class ShellClientMock : IShellClient
    {
        public ShellExecuteResult Execute(string command)
        {
            return new ShellExecuteResult
            {
                StandardOutput = "Mocked standard output",
                ExitCode = 0
            };
        }

        public void Execute(string command, Action<ShellExecuteResult> afterExecuted)
        {
            
        }
    }
}
