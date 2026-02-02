using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IFileService
    {

        /// <summary>
        /// 将目标文件下载到指定文件夹
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="saveDir"></param>
        /// <returns></returns>
        IFile Download(IFile sourceFile, IFile saveDir);

        /// <summary>
        /// 将指定文件上传到目标文件夹
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetDir"></param>
        /// <returns></returns>
        IFile Upload(IFile sourceFile, IFile targetDir);


        /// <summary>
        /// 查找文件
        /// </summary>
        /// <param name="searchParam"></param>
        /// <returns></returns>
        List<IFile> FindFile(FileSearchParam searchParam);

    }
}
