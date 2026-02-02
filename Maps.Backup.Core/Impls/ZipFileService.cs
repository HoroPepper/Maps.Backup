using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Impls
{
    public class ZipFileService : IZipService
    {
        public List<IFile> Unzip(IFile zipFile, IFile targetFile)
        {
            try
            {
                // 校验ZIP文件
                if (!File.Exists(zipFile.Path))
                    throw new FileNotFoundException("ZIP压缩文件不存在", zipFile.Path);


                // 解压目标为目录，不存在则创建（覆盖已存在的目录内容）
                var extractDir = Path.Combine(targetFile.Path,zipFile.FileName);
                if (!Directory.Exists(extractDir))
                {
                    Directory.CreateDirectory(extractDir);
                }

                // 解压ZIP文件到目标目录
                ZipFile.ExtractToDirectory(zipFile.Path, extractDir, true);

                IFileService fileService = new LocalFileService();
                // 返回解压后的目录对象
                return fileService.FindFile(new FileSearchParam()
                {
                    RootPath = extractDir,
                    IsRecursive = true,
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"解压文件失败：ZIP路径={zipFile.Path}，目标路径={targetFile.Path}", ex);
            }
        }

        public IFile Zip(IFile targetFile, IFile zipFile)
        {
            throw new NotImplementedException();
        }
    }
}
