using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IFile
    {
        /// <summary>
        /// 文件位置
        /// </summary>
        string Location { get; }

        /// <summary>
        /// 文件位置类型(本地，共享文件夹，SFTP等)
        /// </summary>
        string LocationType { get; }

        /// <summary>
        /// 文件路径
        /// </summary>
        string Path { get; }

        /// <summary>
        /// 文件真实路径(文件源存在路径代理时和Path出现差异)
        /// </summary>
        string RealPath { get; }

        /// <summary>
        /// 文件类型 | 文件后缀
        /// </summary>
        string FileType { get; }

        /// <summary>
        /// 文件名(无后缀)
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// 文件全名(带后缀)
        /// </summary>
        string FileFullName { get; }

        /// <summary>
        /// 是否为文件夹
        /// </summary>
        bool IsDirectory { get; }

    }
}
