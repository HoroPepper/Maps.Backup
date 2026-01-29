using Maps.Backup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Test
{
    internal class FileMock : IFile
    {
        public string Location { get ; set ; }
        public string Path { get ; set ; }
        public string FileType { get; set; }
        public string FileName { get; set; }
    }
}
