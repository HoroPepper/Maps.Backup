using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IZipService
    {
        /// <summary>
        /// 将文件解压到指定文件夹
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="targetDir"></param>
        /// <returns></returns>
        List<IFile> Unzip(IFile zipFile, IFile targetDir);

        /// <summary>
        /// 将文件压缩到指定文件夹
        /// </summary>
        /// <param name="targetFile"></param>
        /// <param name="zipFile"></param>
        /// <returns></returns>
        IFile Zip(IFile targetDir, IFile zipFile);
    }
}
