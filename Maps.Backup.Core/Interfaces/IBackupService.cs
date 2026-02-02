using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IBackupService
    {
        bool Restore(string targetDatabase, string targetGroup, IFile backupFile);

    }
}
