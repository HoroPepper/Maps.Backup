using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IFileService
    {
        string DownLoad(string remotePath,string savePath);

        string UpLoad(string localPath,string targetPath);

        string UnZip(string path);

        List<string> FindFileByExtension(string rootPath, string fileEx, bool recursive);
    }
}
