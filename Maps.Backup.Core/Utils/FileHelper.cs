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
        /// <summary>
        /// 判断Win文件完整路径是否为文件夹
        /// </summary>
        /// <param name="normalizedPath"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 将Win本地文件从Copy到另一文件夹
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IFile Copy(IFile sourceFile, IFile targetFile)
        {
            try
            {

                string sourceFilePath = sourceFile.Path;
                if (!File.Exists(sourceFilePath))
                    throw new FileNotFoundException("来源文件不存在", sourceFilePath);

                string sourceFileName = sourceFile.FileName;
                string finalTargetFilePath = string.Empty;

                if (targetFile.IsDirectory)//目标文件类型为文件夹时，最终目标路径为目标文件夹+源文件名
                {
                    finalTargetFilePath = Path.Combine(targetFile.Path, sourceFile.FileFullName);
                    if (!Directory.Exists(targetFile.Path))
                        Directory.CreateDirectory(targetFile.Path);
                }
                else//目标类型为文件时，最终目标路径为目标文件夹 + 目标源文件名
                {
                    finalTargetFilePath = targetFile.Path;
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
                    $"文件复制失败", ex);
            }
        }
    }
}
