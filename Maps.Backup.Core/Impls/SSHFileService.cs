using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Maps.Backup.Core.Impls
{
    /// <summary>
    /// 基于SSH协议的文件服务实现（依赖SSH.NET库）
    /// 实现与WinSharedFileService一致的接口行为，支持SSH远程文件操作
    /// </summary>
    public class SSHFileService : IFileService, IDisposable
    {
        // SSH连接核心对象（SFTP用于文件传输，SSH客户端用于远程命令执行）
        private readonly SftpClient _sftpClient;
        private readonly SshClient _sshClient;
        private bool _disposed = false;

        /// <summary>
        /// 构造函数：初始化SSH连接（需传入SSH连接信息，建议从配置注入）
        /// </summary>
        /// <param name="host">SSH服务器IP/域名</param>
        /// <param name="port">SSH端口（默认22）</param>
        /// <param name="username">SSH登录用户名</param>
        /// <param name="password">SSH登录密码</param>
        public SSHFileService(string host, int port = 22, string username = "", string password = "")
        {
            // 初始化SSH连接信息
            var connectionInfo = new ConnectionInfo(host, port, username, new PasswordAuthenticationMethod(username, password));
            // 初始化SFTP客户端（文件上传/下载/遍历）和SSH客户端（远程解压/命令执行）
            _sftpClient = new SftpClient(connectionInfo);
            _sshClient = new SshClient(connectionInfo);
            // 自动打开连接（首次操作前确保连接有效）
            EnsureConnectionOpen();
        }

        /// <summary>
        /// 构造函数：支持密钥认证（替代密码认证，更安全）
        /// </summary>
        /// <param name="host">SSH服务器IP/域名</param>
        /// <param name="port">SSH端口</param>
        /// <param name="username">SSH用户名</param>
        /// <param name="privateKeyPath">本地私钥文件路径（如id_rsa）</param>
        /// <param name="privateKeyPassPhrase">私钥密码（无则传null）</param>
        public SSHFileService(string host, int port, string username, string privateKeyPath, string privateKeyPassPhrase = null)
        {
            // 加载私钥
            var privateKey = new PrivateKeyFile(privateKeyPath, privateKeyPassPhrase);
            var keyAuthMethod = new PrivateKeyAuthenticationMethod(username, privateKey);
            var connectionInfo = new ConnectionInfo(host, port, username, keyAuthMethod);
            // 初始化客户端
            _sftpClient = new SftpClient(connectionInfo);
            _sshClient = new SshClient(connectionInfo);
            EnsureConnectionOpen();
        }

        #region 核心私有方法：SSH连接管理
        /// <summary>
        /// 确保SSH/SFTP连接处于打开状态，断开则重连
        /// </summary>
        private void EnsureConnectionOpen()
        {
            try
            {
                if (!_sftpClient.IsConnected)
                    _sftpClient.Connect();
                if (!_sshClient.IsConnected)
                    _sshClient.Connect();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("SSH/SFTP连接失败，请检查服务器地址、端口、账号密码是否正确", ex);
            }
        }

        /// <summary>
        /// 远程创建目录（递归创建，支持多级目录，如/a/b/c）
        /// </summary>
        /// <param name="remoteDir">远程目录路径</param>
        private void CreateRemoteDirectory(string remoteDir)
        {
            EnsureConnectionOpen();
            // SFTP不支持直接创建多级目录，需递归创建
            var dirs = remoteDir.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string currentDir = "/";
            foreach (var dir in dirs)
            {
                currentDir = Path.Combine(currentDir, dir).Replace('\\', '/');
                if (!_sftpClient.Exists(currentDir))
                {
                    _sftpClient.CreateDirectory(currentDir);
                }
            }
        }

        /// <summary>
        /// 检查远程路径是否为目录
        /// </summary>
        /// <param name="remotePath">远程路径</param>
        /// <returns>true=目录，false=文件/不存在</returns>
        private bool IsRemoteDirectory(string remotePath)
        {
            EnsureConnectionOpen();
            if (!_sftpClient.Exists(remotePath))
                return false;
            var fileAttributes = _sftpClient.GetAttributes(remotePath);
            return fileAttributes.IsDirectory || fileAttributes.IsSymbolicLink;
        }
        #endregion

        #region IFileService 实现：下载/上传/查找/解压
        /// <summary>
        /// 从SSH远程服务器下载文件到本地
        /// 兼容：本地路径为目录时，自动补全远程文件名
        /// </summary>
        public IFile Download(IFile remoteFile, IFile saveFile)
        {
            try
            {
                EnsureConnectionOpen();
                string remotePath = remoteFile.Path.Replace('\\', '/');
                // 1. 校验远程文件是否存在（且不是目录）
                if (!_sftpClient.Exists(remotePath) || IsRemoteDirectory(remotePath))
                {
                    throw new FileNotFoundException("SSH远程文件不存在或为目录", remotePath);
                }

                // 2. 补全本地保存路径：目录则拼接远程文件名
                string sourceFileName = Path.GetFileName(remotePath);
                string localSavePath = saveFile.Path;
                if (Directory.Exists(localSavePath))
                {
                    localSavePath = Path.Combine(localSavePath, sourceFileName);
                }

                // 3. 创建本地保存目录（若不存在）
                string localSaveDir = Path.GetDirectoryName(localSavePath);
                if (!Directory.Exists(localSaveDir))
                {
                    Directory.CreateDirectory(localSaveDir);
                }

                // 4. SFTP下载文件：打开远程文件流，写入本地文件
                using (var remoteFileStream = _sftpClient.OpenRead(remotePath))
                using (var localFileStream = new FileStream(localSavePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    remoteFileStream.CopyTo(localFileStream);
                }

                // 5. 返回本地文件对象（补全后的路径）
                return new LocalFile(localSavePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"SSH文件下载失败：远程路径={remoteFile.Path}，本地路径={saveFile.Path}", ex);
            }
        }

        /// <summary>
        /// 在SSH远程服务器上按条件查找文件
        /// 支持：全称匹配、前缀/后缀匹配、扩展名匹配、递归/非递归查找
        /// </summary>
        public List<IFile> FindFile(FileSearchParam searchParam)
        {
            try
            {
                EnsureConnectionOpen();
                string rootPath = searchParam.RootPath.Replace('\\', '/');
                // 1. 校验搜索根路径
                if (string.IsNullOrWhiteSpace(rootPath) || !_sftpClient.Exists(rootPath) || !IsRemoteDirectory(rootPath))
                {
                    throw new DirectoryNotFoundException("SSH远程搜索根路径不存在或非目录", new Exception(rootPath ?? "空路径"));
                }

                // 2. 递归/非递归遍历远程目录，获取所有文件路径
                var allRemoteFiles = new List<IFile>();
                TraverseRemoteDirectory(rootPath, searchParam.IsRecursive, allRemoteFiles);

                // 3. 转换为IQueryable，叠加AND筛选条件（与WinSharedFileService逻辑完全一致）
                var fileQuery = allRemoteFiles.AsQueryable();

                // 条件1：文件全称匹配（忽略大小写）
                if (!string.IsNullOrWhiteSpace(searchParam.FullName))
                {
                    fileQuery = fileQuery.Where(file =>
                       file.FileFullName.Equals(searchParam.FullName, StringComparison.OrdinalIgnoreCase));
                }
                // 条件2：文件名前缀匹配（不含扩展名，忽略大小写）
                else if (!string.IsNullOrWhiteSpace(searchParam.Prefix))
                {
                    fileQuery = fileQuery.Where(file =>
                        file.FileName.StartsWith(searchParam.Prefix, StringComparison.OrdinalIgnoreCase));
                }
                // 条件3：文件名后缀匹配（不含扩展名，忽略大小写）
                if (!string.IsNullOrWhiteSpace(searchParam.Suffix))
                {
                    fileQuery = fileQuery.Where(file =>
                        file.FileName.EndsWith(searchParam.Suffix, StringComparison.OrdinalIgnoreCase));
                }
                // 条件4：扩展名匹配（自动补全.，忽略大小写）
                if (!string.IsNullOrWhiteSpace(searchParam.FileType))
                {
                    var extension = searchParam.FileType.StartsWith(".")
                        ? searchParam.FileType
                        : $".{searchParam.FileType}";
                    fileQuery = fileQuery.Where(file =>
                        file.FileType.Equals(extension, StringComparison.OrdinalIgnoreCase));
                }

                // 4. 转换为IFile对象列表返回
                return fileQuery.ToList<IFile>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"SSH远程查找文件失败：根路径={searchParam.RootPath}，扩展名={searchParam.FileType}", ex);
            }
        }

        /// <summary>
        /// 从本地上传文件到SSH远程服务器
        /// 兼容：远程路径为目录时，自动补全本地文件名
        /// </summary>
        public IFile Upload(IFile localFile, IFile targetFile)
        {
            try
            {
                EnsureConnectionOpen();
                string localPath = localFile.Path;
                string remoteDirPath = targetFile.IsDirectory ? targetFile.Path.Replace('\\', '/') : Path.GetDirectoryName(targetFile.Path).Replace('\\', '/');
                

                // 1. 校验本地文件是否存在
                if (!File.Exists(localPath))
                {
                    throw new FileNotFoundException("本地文件不存在", localPath);
                }
                // 2. 补全远程目标路径：目录则拼接本地文件名
                
                string localFileName = localFile.FileName + localFile.FileType;
                string remoteTargetPath = Path.Combine(remoteDirPath, localFileName).Replace('\\', '/');
                if (!IsRemoteDirectory(remoteDirPath))
                {
                    CreateRemoteDirectory(remoteDirPath);
                }

                // 4. SFTP上传文件：打开本地文件流，写入远程文件
                using (var localFileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _sftpClient.UploadFile(localFileStream, remoteTargetPath, true); // true=覆盖已存在文件
                }

                // 5. 返回远程文件对象（补全后的实际上传路径）
                return new SFTPFile(remoteTargetPath,false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"SSH文件上传失败：本地路径={localFile.Path}，远程路径={targetFile.Path}", ex);
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 递归遍历SSH远程目录，收集所有文件路径
        /// </summary>
        /// <param name="remoteDir">远程目录根路径</param>
        /// <param name="isRecursive">是否递归子目录</param>
        /// <param name="fileList">文件路径收集容器</param>
        private void TraverseRemoteDirectory(string remoteDir, bool isRecursive, List<IFile> fileList)
        {
            EnsureConnectionOpen();
            // 获取当前目录下的所有文件和子目录
            var files = _sftpClient.ListDirectory(remoteDir);
            foreach (var file in files)
            {
                // 跳过.和..目录
                if (file.Name == "." || file.Name == "..")
                    continue;

                string fullPath = Path.Combine(remoteDir, file.Name).Replace('\\', '/');
                if (file.IsDirectory)
                {
                    // 递归遍历子目录（若开启递归）
                    if (isRecursive)
                    {
                        TraverseRemoteDirectory(fullPath, isRecursive, fileList);
                    }
                }
                else
                {
                    // 收集文件路径
                    fileList.Add(new SFTPFile(fullPath,false) { RealPath = file.FullName });
                }
            }
        }
        #endregion

        #region 资源释放：IDisposable实现
        /// <summary>
        /// 释放SSH/SFTP连接资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 释放托管资源：关闭SFTP/SSH连接
                if (_sftpClient.IsConnected)
                    _sftpClient.Disconnect();
                _sftpClient.Dispose();

                if (_sshClient.IsConnected)
                    _sshClient.Disconnect();
                _sshClient.Dispose();
            }

            _disposed = true;
        }

        ~SSHFileService()
        {
            Dispose(false);
        }
        #endregion
    }
}
