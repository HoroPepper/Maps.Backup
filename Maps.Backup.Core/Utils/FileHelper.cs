using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
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

        public static IFile Copy(IFile sourceFile, IFile targetDir)
        {
            try
            {

                string sourceFilePath = sourceFile.Path;
                if (!File.Exists(sourceFilePath))
                    throw new FileNotFoundException("远程文件不存在", sourceFilePath);

                string sourceFileName = sourceFile.FileName;
                string finalTargetFilePath = string.Empty;

                // 4. 区分本地目标是【文件夹】还是【文件】，处理最终保存路径
                if (targetDir.IsDirectory)
                {
                    // 目标是文件夹：最终路径 = 文件夹路径 + 远程文件名
                    finalTargetFilePath = Path.Combine(targetDir.Path, sourceFileName + sourceFile.FileType);
                    // 自动创建【目标文件夹】（如果不存在）
                    if (!Directory.Exists(targetDir.Path))
                        Directory.CreateDirectory(targetDir.Path);
                }
                else
                {
                    finalTargetFilePath = targetDir.Path;
                    string targetFileParentDir = Path.GetDirectoryName(finalTargetFilePath);
                    if (!Directory.Exists(targetFileParentDir))
                        Directory.CreateDirectory(targetFileParentDir);
                }

                File.Copy(sourceFilePath, finalTargetFilePath, overwrite: true);

                return new LocalFile(finalTargetFilePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"下载文件失败", ex);
            }
        }
    }
}
