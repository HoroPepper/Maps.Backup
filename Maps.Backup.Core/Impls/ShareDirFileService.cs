using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Impls
{
    public class ShareDirFileService : IFileService
    {
        public IFile Download(IFile remoteFile, IFile saveFile)
        {
            try
            {
                // 1. 校验远程源文件是否存在
                
                if (!File.Exists(remoteFile.Path))
                    throw new FileNotFoundException("远程文件不存在", remoteFile.Path);

                // 核心修复：提取源文件的文件名，补全本地保存的文件路径
                string sourceFileName = Path.GetFileName(remoteFile.Path); // 拿到源文件的完整文件名（如test.txt、demo.jpg）
                string localSavePath = saveFile.Path;
                // 判断如果传入的本地路径是目录，则自动拼接源文件名，补全为文件路径
                if (Directory.Exists(localSavePath))
                {
                    localSavePath = Path.Combine(localSavePath, sourceFileName);
                }

                // 2. 获取本地保存目录，不存在则创建（此时localSavePath已确保是文件路径）
                var saveDir = Path.GetDirectoryName(localSavePath);
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                // 3. 复制远程文件到本地（覆盖已存在的文件），使用补全后的文件路径
                File.Copy(remoteFile.Path, localSavePath, true);

                // 4. 返回填充实时属性的本地文件对象，返回补全后的路径
                return new FileModel { Path = localSavePath };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"下载文件失败：远程路径={remoteFile.Path}，本地路径={saveFile.Path}", ex);
            }
        }

        public List<IFile> FindFileByExtension(FileSearchParam searchParam)
        {
            try
            {
                // 校验搜索根路径
                if (!Directory.Exists(searchParam.RootPath))
                    throw new DirectoryNotFoundException("搜索根路径不存在", new Exception(searchParam.RootPath));

                // 搜索选项：是否递归
                var searchOption = searchParam.IsRecursive
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                // 遍历所有文件，筛选符合扩展名的文件
                var filePaths = Directory.EnumerateFiles(searchParam.RootPath, "*.*", searchOption)
                    .Where(path => searchParam.FileType == Path.GetExtension(path).ToLower());

                // 转换为IFile对象列表并返回
                return filePaths.Select(path => new FileModel { Path = path }).ToList<IFile>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"按扩展名查找文件失败：根路径={searchParam.RootPath}，扩展名={searchParam.FileType}", ex);
            }
        }

        public IFile Unzip(IFile zipFile, IFile targetExtractFile)
        {
            try
            {
                // 校验ZIP文件
                if (!File.Exists(zipFile.Path))
                    throw new FileNotFoundException("ZIP压缩文件不存在", zipFile.Path);
               

                // 解压目标为目录，不存在则创建（覆盖已存在的目录内容）
                var extractDir = targetExtractFile.Path;
                if (!Directory.Exists(extractDir))
                {
                    Directory.CreateDirectory(extractDir);
                }
                
                // 解压ZIP文件到目标目录
                ZipFile.ExtractToDirectory(zipFile.Path, extractDir, true);

                // 返回解压后的目录对象
                return new FileModel { Path = extractDir };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"解压文件失败：ZIP路径={zipFile.Path}，目标路径={targetExtractFile.Path}", ex);
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
                return new FileModel { Path = remoteTargetPath };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"上传文件失败：本地路径={localFile.Path}，远程路径={targetFile.Path}", ex);
            }
        }
    }
}
