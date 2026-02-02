using Maps.Backup.Core.Constatns;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Models
{
    public class LocalFile : IFile
    {
        #region 私有字段（存储属性值，仅构造函数赋值）
        private readonly string _location;
        private readonly string _path;
        private readonly string _fileType;
        private readonly string _fileName;
        private readonly bool _isDirectory;
        private readonly string _locationType;
        #endregion

        #region 实现IFile接口的只读属性（仅返回私有字段值）
        public string Location => _location;
        public string Path => _path;
        public string FileType => _fileType;
        public string FileName => _fileName;
        public bool IsDirectory => _isDirectory;
        public string LocationType => _locationType;
        #endregion

        public LocalFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), "路径不能为空或空白！");
            if(path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            string normalizedPath = System.IO.Path.GetFullPath(path);

            _isDirectory = FileHelper.IsDirectoryPath(normalizedPath);

            _location = SystemConstants.FileLocationType.Local;
            _locationType = SystemConstants.FileLocationType.Local;

            _path = normalizedPath;

            if (!_isDirectory)
            {
                _fileName = System.IO.Path.GetFileNameWithoutExtension(normalizedPath);
                _fileType = System.IO.Path.GetExtension(normalizedPath);
            }
            else
            {
                _fileName = new DirectoryInfo(normalizedPath).Name;
                _fileType = string.Empty;
            }
        }
    }
}
