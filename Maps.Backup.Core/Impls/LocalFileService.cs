using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Impls
{
    public class LocalFileService : IFileService
    {
        public IFile Download(IFile remoteFile, IFile saveFile)
        {
            try
            {

                if (!File.Exists(remoteFile.Path))
                    throw new FileNotFoundException("远程文件不存在", remoteFile.Path);

                string sourceFileName = Path.GetFileName(remoteFile.Path); 
                string localSavePath = saveFile.Path;

                if (saveFile.IsDirectory)
                {
                    localSavePath = Path.Combine(localSavePath, sourceFileName);
                }

                var saveDir = saveFile.Path;
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                File.Copy(remoteFile.Path, localSavePath, true);

                return new LocalFile(localSavePath);
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

                if (string.IsNullOrWhiteSpace(searchParam.RootPath) || !Directory.Exists(searchParam.RootPath))
                    throw new DirectoryNotFoundException("搜索根路径不存在或为空", new Exception(searchParam.RootPath ?? "空路径"));

                var searchOption = searchParam.IsRecursive
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                var fileQuery = Directory.EnumerateFiles(searchParam.RootPath, "*.*", searchOption).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchParam.FullName))
                {
                    fileQuery = fileQuery.Where(path =>
                        Path.GetFileName(path).Equals(searchParam.FullName, StringComparison.OrdinalIgnoreCase));
                }

                else if (!string.IsNullOrWhiteSpace(searchParam.Prefix))
                {
                    fileQuery = fileQuery.Where(path =>
                        Path.GetFileNameWithoutExtension(path).StartsWith(searchParam.Prefix, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(searchParam.Suffix))
                {
                    fileQuery = fileQuery.Where(path =>
                        Path.GetFileNameWithoutExtension(path).EndsWith(searchParam.Suffix, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(searchParam.FileType))
                {

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
                if (!File.Exists(localFile.Path))
                    throw new FileNotFoundException("本地文件不存在", localFile.Path);

                string localFileName = Path.GetFileName(localFile.Path); 
                string remoteTargetPath = targetFile.Path;

                if (Directory.Exists(remoteTargetPath))
                {
                    remoteTargetPath = Path.Combine(remoteTargetPath, localFileName);
                }

                var remoteDir = Path.GetDirectoryName(remoteTargetPath);
                if (!string.IsNullOrEmpty(remoteDir) && !Directory.Exists(remoteDir))
                {
                    Directory.CreateDirectory(remoteDir);
                }


                File.Copy(localFile.Path, remoteTargetPath, true);

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
