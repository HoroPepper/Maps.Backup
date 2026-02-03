using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Impls
{
    public class WinSharedFileService : IFileService
    {
        public IFile Download(IFile remoteFile, IFile saveFile)
        {
            try
            {
                return FileHelper.Copy(remoteFile, saveFile);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"下载文件失败：远程路径={remoteFile.Path}，本地路径={saveFile.Path}", ex);
            }
        }

        public List<IFile> FindFile(FileSearchParam searchParam)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(searchParam?.FullPath))
                {
                    if (Path.Exists(searchParam.FullPath))
                    {
                        return new List<IFile> { new LocalFile(searchParam.FullPath) };
                    }
                    else
                    {
                        return new List<IFile>();
                    }
                }

                // 1. 校验搜索根路径（空路径直接抛异常）
                if (string.IsNullOrWhiteSpace(searchParam.RootPath) || !Directory.Exists(searchParam.RootPath))
                    throw new DirectoryNotFoundException("搜索根路径不存在或为空", new Exception(searchParam.RootPath ?? "空路径"));

                // 2. 设置搜索选项：是否递归子目录
                var searchOption = searchParam.IsRecursive
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                // 3. 基础查询：获取根路径下所有文件的完整路径
                var fileQuery = Directory.EnumerateFiles(searchParam.RootPath, "*.*", searchOption).AsQueryable();

                // 4. 叠加AND筛选条件：所有非空参数均需满足，空参数忽略
                // 条件1：文件全称匹配（FullName非空时，匹配「文件名.扩展名」完整名称）
                if (!string.IsNullOrWhiteSpace(searchParam.FullName))
                {
                    fileQuery = fileQuery.Where(path =>
                        Path.GetFileName(path).Equals(searchParam.FullName, StringComparison.OrdinalIgnoreCase));
                }
                // 条件2：文件名前缀匹配（Prefix非空时，文件名以指定前缀开头，忽略大小写）
                else if (!string.IsNullOrWhiteSpace(searchParam.Prefix))
                {
                    fileQuery = fileQuery.Where(path =>
                        Path.GetFileNameWithoutExtension(path).StartsWith(searchParam.Prefix, StringComparison.OrdinalIgnoreCase));
                }
                // 条件3：文件名后缀匹配（Suffix非空时，文件名（不含扩展名）以指定后缀结尾，忽略大小写）
                if (!string.IsNullOrWhiteSpace(searchParam.Suffix))
                {
                    fileQuery = fileQuery.Where(path =>
                        Path.GetFileNameWithoutExtension(path).EndsWith(searchParam.Suffix, StringComparison.OrdinalIgnoreCase));
                }
                // 条件4：文件扩展名匹配（FileType非空时，匹配扩展名，自动补全.，忽略大小写）
                if (!string.IsNullOrWhiteSpace(searchParam.FileType))
                {
                    // 统一扩展名格式：确保以.开头，避免传入txt和.txt的差异
                    var extension = searchParam.FileType.StartsWith(".")
                        ? searchParam.FileType
                        : $".{searchParam.FileType}";
                    fileQuery = fileQuery.Where(path =>
                        Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase));
                }

                // 5. 转换为IFile对象列表并返回
                return fileQuery.Select(path => new LocalFile(path)).ToList<IFile>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"按扩展名查找文件失败：根路径={searchParam.RootPath}，扩展名={searchParam.FileType}", ex);
            }
        }

        public IFile Upload(IFile localFile, IFile targetFile)
        {
            try
            {
                // 1. 校验本地源文件是否存在
                if (!File.Exists(localFile.Path))
                    throw new FileNotFoundException("本地文件不存在", localFile.Path);

                // 核心修复：提取本地文件名，补全远程目标路径
                string localFileName = Path.GetFileName(localFile.Path); // 拿到本地文件的完整文件名（如test.jpg、data.csv）
                string remoteTargetPath = targetFile.Path;
                // 判断如果传入的远程路径是目录，则自动拼接本地文件名，补全为文件路径
                if (Directory.Exists(remoteTargetPath))
                {
                    remoteTargetPath = Path.Combine(remoteTargetPath, localFileName);
                }

                // 2. 获取远程目标目录，不存在则创建（此时remoteTargetPath已确保是文件路径）
                var remoteDir = Path.GetDirectoryName(remoteTargetPath);
                if (!string.IsNullOrEmpty(remoteDir) && !Directory.Exists(remoteDir))
                {
                    Directory.CreateDirectory(remoteDir);
                }

                // 3. 复制本地文件到远程共享目录（覆盖已存在的文件），使用补全后的路径
                File.Copy(localFile.Path, remoteTargetPath, true);

                // 4. 返回填充实时属性的远程文件对象，返回实际上传的路径
                return new WinSharedDirFile(remoteTargetPath, targetFile.Location);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"上传文件失败：本地路径={localFile.Path}，远程路径={targetFile.Path}", ex);
            }
        }
    }
}
