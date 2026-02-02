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
    public class WinSharedDirFile : IFile
    {
        #region 私有只读字段：仅构造函数赋值，保证对象不可变
        private readonly string _location;  // 共享目录固定标识
        private readonly string _path;      // 标准化路径（统一分隔符，无冗余）
        private readonly string _fileType;  // 文件后缀（含.），文件夹置空
        private readonly string _fileName;  // 文件名（无后缀）/最后一级文件夹名
        private readonly bool _isDirectory; // 是否为文件夹路径
        private readonly string _locationType;
        private readonly string _realPath;
        #endregion

        #region 实现IFile只读接口：仅返回私有字段，无修改逻辑
        public string Location => _location;
        public string Path => _path;
        public string FileType => _fileType;
        public string FileName => _fileName;
        public bool IsDirectory => _isDirectory;
        public string LocationType => _locationType;
        public string RealPath => _realPath;
        #endregion

        #region 构造函数：核心解析逻辑（仅基于路径字符串，支持新建传参）
        public WinSharedDirFile(string path,string sharedIP)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), "共享目录路径不能为空或空白！");

            string normalizedPath = System.IO.Path.GetFullPath(path);

            _isDirectory = FileHelper.IsDirectoryPath(normalizedPath);

            _location = sharedIP;
            _locationType = SystemConstants.FileLocationType.WinSharedDir;

            _path = normalizedPath;

            _realPath = normalizedPath;

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
        #endregion
    }
}
