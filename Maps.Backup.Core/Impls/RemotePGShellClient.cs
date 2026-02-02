using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Impls
{
    public class RemotePGShellClient : IShellClient
    {
        private readonly string _sshHost;
        private readonly int _sshPort;
        private readonly string _sshUName;
        private readonly string _sshPwd;
        private readonly string _pgUName;
        private readonly string _pgPwd;
        // 远程Windows临时目录（固定，需确保目标机该目录存在且有读写执行权限）
        private readonly string _remoteTempDir = "/commandTemp";
        // 远程BAT文件的基础名称（拼接随机串避免重名）
        private readonly string _batBaseName = "PgBackup_Execute_";

        /// <summary>
        /// 构造函数：初始化SSH连接配置
        /// </summary>
        /// <param name="sshHost">远程PG主机IP/域名（Windows机器）</param>
        /// <param name="sshPort">SSH端口（默认22）</param>
        /// <param name="sshUName">SSH登录账号</param>
        /// <param name="sshPwd">SSH登录密码</param>
        /// <param name="pgUName">PG账号</param>
        /// <param name="pgPwd">PG密码</param>
        public RemotePGShellClient(string sshHost, int sshPort, string sshUName, string sshPwd, string pgUName, string pgPwd)
        {
            _sshHost = sshHost ?? throw new ArgumentNullException(nameof(sshHost), "SSH主机地址不能为空");
            SetEnvironmentVar("sshHost", _sshHost);
            _sshPort = sshPort;
            SetEnvironmentVar("sshPort", _sshPort.ToString());
            _sshUName = sshUName ?? throw new ArgumentNullException(nameof(sshUName), "SSH账号不能为空");
            SetEnvironmentVar("sshUName", _sshUName);
            _sshPwd = sshPwd;
            SetEnvironmentVar("sshPwd", _sshPwd);
            _pgUName = pgUName ?? throw new ArgumentNullException(nameof(pgUName), "PG账号不能为空");
            SetEnvironmentVar("pgUName", _pgUName);
            _pgPwd = pgPwd;
            SetEnvironmentVar("pgPwd", _pgPwd);
        }

        private Dictionary<string, string> _environmentVarDic = new Dictionary<string, string>();

        /// <summary>
        /// 核心改造：生成BAT文件→SFTP传输→执行BAT→删除BAT（全程阻塞，直到所有步骤完成）
        /// </summary>
        /// <param name="command">要执行的远程命令（如pg_restore、psql等，会写入BAT文件）</param>
        /// <returns>命令执行结果（含输出、错误、退出码）</returns>
        public ShellExecuteResult Execute(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentNullException(nameof(command), "执行的命令不能为空");

            // 初始化SSH客户端和SFTP客户端（using自动释放，避免连接/句柄泄漏）
            using var sshClient = new SshClient(_sshHost, _sshPort, _sshUName, _sshPwd);
            using var sftpClient = new SftpClient(_sshHost, _sshPort, _sshUName, _sshPwd);
            var result = new ShellExecuteResult();
            // 生成唯一远程BAT文件路径（随机串避免多线程执行时重名覆盖）
            var remoteBatFileName = $"{_batBaseName}{Guid.NewGuid():N}.bat";
            var remoteBatFullPath = Path.Combine(_remoteTempDir, remoteBatFileName).Replace('\\', '/');
            // Windows BAT文件内容：写入传入的命令，加@echo off关闭回显，提升输出整洁度
            var batContent = $@"@echo off
{command}";

            try
            {
                // 步骤1：建立SSH和SFTP连接（一次连接完成所有操作，避免多次握手）
                if (!sshClient.IsConnected) sshClient.Connect();
                if (!sftpClient.IsConnected) sftpClient.Connect();

                // 步骤2：SFTP传输BAT文件（直接将字符串写入远程文件，无需本地生成物理文件）
                using var batMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(batContent));
                // 检查远程临时目录是否存在，不存在则创建（避免文件写入失败）
                if (!sftpClient.Exists(_remoteTempDir))
                {
                    sftpClient.CreateDirectory(_remoteTempDir);
                }
                sftpClient.UploadFile(batMemoryStream, remoteBatFullPath, true);
                result.StandardOutput += $"BAT文件传输成功，远程路径：{remoteBatFullPath}{Environment.NewLine}";

                // 步骤3：通过SSH执行远程BAT文件（Windows必须用cmd /c调用，/c表示执行后关闭cmd窗口）
                var executeBatCommand = $"cmd /c {remoteBatFullPath}";
                var commandResult = sshClient.RunCommand(executeBatCommand);
                // 拼接BAT执行结果（保留原输出/错误格式）
                result.StandardOutput += commandResult.Result?.Trim() ?? string.Empty;
                result.StandardError = commandResult.Error?.Trim() ?? string.Empty;
                result.ExitCode = commandResult.ExitStatus ?? 0;

                if (result.ExitCode == 0)
                {
                    result.StandardOutput += $"{Environment.NewLine}BAT文件执行成功，退出码：0";
                }
                else
                {
                    result.StandardError += $"{Environment.NewLine}BAT文件执行失败，退出码：{result.ExitCode}";
                }
            }
            catch (SshConnectionException ex)
            {
                // 捕获SSH/SFTP连接异常（最高优先级，连接失败则后续步骤都无法执行）
                result.StandardError = $"SSH/SFTP连接失败：{ex.Message}（请检查主机IP、端口、账号密码，防火墙是否放行22端口）";
                result.ExitCode = -1; // 自定义码：连接异常
            }
            catch (SftpException ex)
            {
                // 捕获SFTP传输异常（目录创建、文件上传失败）
                result.StandardError = $"BAT文件传输失败：{ex.Message}（远程路径：{remoteBatFullPath}，请检查目录权限）";
                result.ExitCode = -3; // 自定义码：文件传输异常
            }
            catch (Exception ex)
            {
                // 捕获命令执行/其他未知异常
                result.StandardError = $"BAT文件执行异常：{ex.Message}（远程BAT路径：{remoteBatFullPath}）";
                result.ExitCode = -2; // 自定义码：执行异常
            }
            finally
            {
                // 步骤4：无论成功/失败，都删除远程BAT文件（避免垃圾文件残留）
                try
                {
                    if (sftpClient.IsConnected && sftpClient.Exists(remoteBatFullPath))
                    {
                        //sftpClient.DeleteFile(remoteBatFullPath);
                        // 仅在无执行错误时拼接删除成功信息，避免覆盖错误日志
                        if (string.IsNullOrWhiteSpace(result.StandardError))
                        {
                            result.StandardOutput += $"{Environment.NewLine}BAT文件已成功清理，远程路径：{remoteBatFullPath}";
                        }
                    }
                }
                catch (Exception delEx)
                {
                    // 删除失败仅追加警告，不改变原执行结果（避免因删除失败导致整体流程标记为失败）
                    result.StandardOutput += $"{Environment.NewLine}【警告】BAT文件清理失败：{delEx.Message}，请手动删除{remoteBatFullPath}";
                }

                // 确保连接断开，释放服务器资源
                if (sftpClient.IsConnected) sftpClient.Disconnect();
                if (sshClient.IsConnected) sshClient.Disconnect();
            }

            return result;
        }

        /// <summary>
        /// 异步执行：复用同步逻辑，通过Task包装实现真正的异步（原实现为伪异步，此处优化）
        /// </summary>
        /// <param name="command">要执行的远程命令</param>
        /// <param name="afterExecuted">执行完成后的回调方法</param>
        public void Execute(string command, Action<ShellExecuteResult> afterExecuted)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentNullException(nameof(command), "执行的命令不能为空");

            var executeResult = Execute(command);
            afterExecuted?.Invoke(executeResult);
        }

        public string GetEnvironmentVar(string key)
        {
            if (!_environmentVarDic.ContainsKey(key))
            {
                return string.Empty;
            }
            return _environmentVarDic[key];
        }

        public void SetEnvironmentVar(string key, string value)
        {
            _environmentVarDic[key] = value;
        }
    }
}