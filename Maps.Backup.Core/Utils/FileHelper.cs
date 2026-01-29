using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Utils
{
    public static class FileHelper
    {
        public static bool IsDirectoryPath(string normalizedPath)
        {
            // 规则1：路径以系统默认路径分隔符结尾 → 判定为文件夹
            char dirSeparator = System.IO.Path.DirectorySeparatorChar;
            if (normalizedPath.EndsWith(dirSeparator.ToString()))
                return true;

            // 规则2：路径无有效文件后缀（无.或.后无字符）→ 判定为文件夹
            string extension = System.IO.Path.GetExtension(normalizedPath);
            return string.IsNullOrEmpty(extension);
        }
    }
}
