using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IZipService
    {
        /// <summary>
        /// 解压压缩文件到指定目标路径
        /// </summary>
        /// <param name="zipFilePath">压缩文件完整路径（如.zip/.7z/.rar）</param>
        /// <param name="targetExtractPath">解压目标目录路径（不存在则自动创建）</param>
        /// <returns>解压后的实际目录路径</returns>
        List<IFile> Unzip(IFile zipFile, IFile targetFile);

        IFile Zip(IFile targetFile, IFile zipFile);
    }
}
