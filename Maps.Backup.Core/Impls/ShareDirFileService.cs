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
                if (!File.Exists(remoteFile.Path))
                    throw new FileNotFoundException("远程文件不存在", remoteFile.Path);

                // 获取本地保存目录，不存在则创建
                var saveDir = Path.GetDirectoryName(saveFile.Path);
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                // 复制远程文件到本地（覆盖已存在的文件）
                File.Copy(remoteFile.Path, saveFile.Path, true);

                // 返回填充实时属性的本地文件对象
                return new FileModel { Path = saveFile.Path };
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
                if (zipFile.FileType.ToLower() != ".zip")
                    throw new ArgumentException("目标文件不是ZIP压缩文件", nameof(zipFile));

                // 解压目标为目录，不存在则创建（覆盖已存在的目录内容）
                var extractDir = targetExtractFile.Path;
                if (Directory.Exists(extractDir))
                {
                    // 删除原有目录（避免残留文件），重新创建
                    Directory.Delete(extractDir, true);
                }
                Directory.CreateDirectory(extractDir);

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
                // 校验本地文件是否存在
                if (!File.Exists(localFile.Path))
                    throw new FileNotFoundException("本地文件不存在", localFile.Path);

                // 获取远程目标目录，不存在则创建
                var remoteDir = Path.GetDirectoryName(targetFile.Path);
                if (!Directory.Exists(remoteDir))
                    Directory.CreateDirectory(remoteDir);

                // 复制本地文件到远程共享目录（覆盖已存在的文件）
                File.Copy(localFile.Path, targetFile.Path, true);

                // 返回填充实时属性的远程文件对象
                return new FileModel { Path = targetFile.Path };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"上传文件失败：本地路径={localFile.Path}，远程路径={targetFile.Path}", ex);
            }
        }
    }
}
