using Maps.Backup.Core.Constatns;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Utils;
using System;
using System.IO;

namespace Maps.Backup.Core.Models
{
    /// <summary>
    /// SFTP/SSH远程文件/目录的抽象实现（实现IFile接口）
    /// 参照LocalFile设计规范，适配SFTP Unix风格路径
    /// </summary>
    public class SSHFile : IFile
    {
        #region 私有字段（存储属性值，仅构造函数赋值）
        /// <summary>
        /// 远程文件/目录的位置类型（SSH/SFTP）
        /// </summary>
        private readonly string _location;
        /// <summary>
        /// SFTP标准Unix风格绝对路径（核心：统一路径格式）
        /// </summary>
        private readonly string _path;
        /// <summary>
        /// 文件扩展名（远程文件为空/目录为空）
        /// </summary>
        private readonly string _fileType;
        /// <summary>
        /// 文件名/目录名（无扩展名）
        /// </summary>
        private readonly string _fileName;
        /// <summary>
        /// 是否为目录（远程路径特征判断）
        /// </summary>
        private readonly bool _isDirectory;
        /// <summary>
        /// 位置类型（和Location保持一致，兼容LocalFile设计）
        /// </summary>
        private readonly string _locationType;
        #endregion

        #region 实现IFile接口的只读属性（仅返回私有字段值，和LocalFile完全一致）
        public string Location => _location;
        public string Path => _path;
        public string FileType => _fileType;
        public string FileName => _fileName;
        public bool IsDirectory => _isDirectory;
        public string LocationType => _locationType;
        #endregion

        /// <summary>
        /// 构造函数：通过SFTP远程路径初始化SSH文件/目录对象
        /// 自动标准化为Unix风格绝对路径，区分文件/目录
        /// </summary>
        /// <param name="sftpPath">SFTP远程路径（支持Windows/相对/绝对路径，自动标准化）</param>
        public SSHFile(string sftpPath)
        {
            // 1. 基础校验：路径不能为空/空白
            if (string.IsNullOrWhiteSpace(sftpPath))
                throw new ArgumentNullException(nameof(sftpPath), "SFTP远程路径不能为空或空白！");

            // 2. 核心：SFTP路径标准化（解决Bad message关键，统一为Unix风格绝对路径）
            string normalizedSshPath = NormalizeSftpPath(sftpPath);

            // 3. 判断是否为远程目录（SFTP路径特征：以/结尾 或 无扩展名的纯目录名）
            _isDirectory = FileHelper.IsDirectoryPath(normalizedSshPath) || normalizedSshPath.EndsWith("/");

            // 4. 初始化位置类型（和SystemConstants中远程类型常量匹配，建议定义为Ssh/Remote）
            _location = SystemConstants.FileLocationType.SSH;
            _locationType = SystemConstants.FileLocationType.SSH;

            // 5. 赋值标准化后的SFTP绝对路径（所有操作基于此路径，避免格式错误）
            _path = normalizedSshPath;

            // 6. 区分文件/目录，初始化文件名/文件类型
            if (!_isDirectory)
            {
                // 远程文件：截取文件名（无扩展名）和扩展名
                _fileName = GetSftpFileNameWithoutExtension(normalizedSshPath);
                _fileType = GetSftpFileExtension(normalizedSshPath);
            }
            else
            {
                // 远程目录：截取最后一级目录名，文件类型为空
                _fileName = GetSftpDirectoryName(normalizedSshPath);
                _fileType = string.Empty;
            }
        }

        #region 私有工具方法：SFTP路径标准化+解析（适配Unix风格，独立封装，便于维护）
        /// <summary>
        /// 将任意格式的SFTP路径标准化为【Unix风格绝对路径】
        /// 处理：替换反斜杠/去重连续斜杠/补全根目录/去除末尾多余/
        /// </summary>
        /// <param name="rawPath">原始路径（支持\分隔/相对路径/绝对路径）</param>
        /// <returns>SFTP标准Unix风格绝对路径</returns>
        private string NormalizeSftpPath(string rawPath)
        {
            return rawPath.Trim()
                          .Replace('\\', '/')          // 替换Windows反斜杠为Unix正斜杠（核心）
                          .Replace("//", "/")           // 去重连续斜杠，避免服务端解析异常
                          .TrimEnd('/');                // 去除末尾多余/（统一格式，目录通过_isDirectory判断）
        }

        /// <summary>
        /// 解析SFTP文件路径，获取【无扩展名的文件名】（适配Unix风格）
        /// 示例：/data/backup/20260130.log → 20260130
        /// </summary>
        private string GetSftpFileNameWithoutExtension(string sftpAbsolutePath)
        {
            // 截取最后一个/后的完整文件名
            string fullFileName = sftpAbsolutePath.Substring(sftpAbsolutePath.LastIndexOf("/") + 1);
            // 截取扩展名前的文件名（Unix风格扩展名以.开头）
            int extIndex = fullFileName.LastIndexOf(".");
            return extIndex > 0 ? fullFileName.Substring(0, extIndex) : fullFileName;
        }

        /// <summary>
        /// 解析SFTP文件路径，获取【文件扩展名】（含.，适配Unix风格）
        /// 示例：/data/backup/20260130.log → .log；无扩展名返回空
        /// </summary>
        private string GetSftpFileExtension(string sftpAbsolutePath)
        {
            string fullFileName = sftpAbsolutePath.Substring(sftpAbsolutePath.LastIndexOf("/") + 1);
            int extIndex = fullFileName.LastIndexOf(".");
            return extIndex > 0 ? fullFileName.Substring(extIndex) : string.Empty;
        }

        /// <summary>
        /// 解析SFTP目录路径，获取【最后一级目录名】（适配Unix风格）
        /// 示例：/data/backup/202601 → 202601；/ → /（根目录特殊处理）
        /// </summary>
        private string GetSftpDirectoryName(string sftpAbsolutePath)
        {
            // 根目录特殊处理：直接返回/
            if (sftpAbsolutePath == "/")
                return "/";
            // 截取最后一个/后的目录名
            return sftpAbsolutePath.Substring(sftpAbsolutePath.LastIndexOf("/") + 1);
        }
        #endregion
    }
}