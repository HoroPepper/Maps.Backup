using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Models
{
    public class FileSearchParam
    {
        /// <summary>
        /// 查询根目录
        /// </summary>
        public string RootPath { get; set; }

        /// <summary>
        /// 文件全名（包含后缀）
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// 文件名(不包含后缀)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// 文件名前缀
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 文件名后缀
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// 文件类型(.xxx)
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// 是否递归查询
        /// </summary>
        public bool IsRecursive { get; set; }
    }
}
