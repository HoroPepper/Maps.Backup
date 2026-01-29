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
        /// 从远程地址下载文件到本地
        /// </summary>
        /// <param name="remotePath">远程文件完整路径（如FTP/S3/HTTP的文件地址）</param>
        /// <param name="savePath">本地保存完整路径（含文件名，如D:\files\test.txt）</param>
        /// <returns>本地实际保存的文件路径（与savePath一致，特殊场景如自动重命名则返回新路径）</returns>
        IFile Download(IFile remoteFile, IFile saveFile);

        /// <summary>
        /// 从本地上传文件到远程目标地址
        /// </summary>
        /// <param name="localPath">本地文件完整路径</param>
        /// <param name="targetPath">远程目标完整路径（含文件名）</param>
        /// <returns>远程实际的文件路径</returns>
        IFile Upload(IFile localFile, IFile targetFile);

        /// <summary>
        /// 按文件后缀查找文件
        /// </summary>
        /// <param name="searchParam">查询参数</param>
        /// <returns>符合条件的文件完整路径列表，无结果则返回空列表（非null）</returns>
        List<IFile> FindFile(FileSearchParam searchParam);

    }
}
