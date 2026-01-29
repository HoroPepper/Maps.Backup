using Maps.Backup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Test
{
    public class BackupMock : IBackup
    {
        public string Restore(string targetDatabase, IFile backupFile)
        {
            return "RestoreMocked";
        }

        public string Restore(string targetDatabase, string targetGroup, IFile backupFile)
        {
            return "RestoreMocked";
        }
    }
}
