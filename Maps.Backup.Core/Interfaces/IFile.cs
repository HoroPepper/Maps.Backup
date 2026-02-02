using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IFile
    {
        string Location { get; }

        string LocationType { get; }

        string Path { get; }

        string RealPath { get; }

        string FileType { get; }

        string FileName { get; }

        string FileFullName { get; }

        bool IsDirectory { get; }

    }
}
