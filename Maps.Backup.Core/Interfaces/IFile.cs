using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IFile
    {
        string Location { get; set; }

        string Path { get; set; }

        string FileType { get; set; }

        string FileName { get; set; }

    }
}
