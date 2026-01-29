using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 构造函数：初始化SSH连接配置
        /// </summary>
        /// <param name="sshHost">远程PG主机IP/域名</param>
        /// <param name="sshPort">SSH端口（默认22）</param>
        /// <param name="sshUName">SSH登录账号</param>
        /// <param name="sshPwd">SSH登录密码</param>
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
            _pgUName = pgUName ?? throw new ArgumentNullException(nameof(pgUName), "PG账号不能为空"); ;
            SetEnvironmentVar("pgUName", _pgUName);
            _pgPwd = pgPwd;
            SetEnvironmentVar("pgPwd", _pgPwd);
        }

        private Dictionary<string, string> _environmentVarDic = new Dictionary<string, string>();

        /// <summary>
        /// 同步执行远程Shell命令（阻塞式，直到命令执行完成）
        /// </summary>
        /// <param name="command">要执行的远程命令（如pg_restore、psql等）</param>
        /// <returns>命令执行结果（含输出、错误、退出码）</returns>
        public ShellExecuteResult Execute(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentNullException(nameof(command), "执行的命令不能为空");

            // 初始化SSH客户端（using自动释放资源，避免连接泄漏）
            using var sshClient = new SshClient(_sshHost, _sshPort, _sshUName, _sshPwd);
            var result = new ShellExecuteResult();

            try
            {
                // 建立SSH连接
                if (!sshClient.IsConnected)
                    sshClient.Connect();

                // 执行远程命令并获取结果
                var commandResult = sshClient.RunCommand(command);

                // 映射执行结果到ShellExecuteResult
                result.StandardOutput = commandResult.Result?.Trim() ?? string.Empty;
                result.StandardError = commandResult.Error?.Trim() ?? string.Empty;
                result.ExitCode = commandResult.ExitStatus ?? 0;
            }
            catch (SshConnectionException ex)
            {
                // 捕获SSH连接异常，封装到错误输出
                result.StandardError = $"SSH连接失败：{ex.Message}（请检查主机IP、端口、账号密码）";
                result.ExitCode = -1; // 自定义非0退出码标识连接异常
            }
            catch (Exception ex)
            {
                // 捕获其他执行异常
                result.StandardError = $"命令执行异常：{ex.Message}";
                result.ExitCode = -2; // 自定义非0退出码标识执行异常
            }
            finally
            {
                // 确保SSH连接断开，释放资源
                if (sshClient.IsConnected)
                    sshClient.Disconnect();
            }

            return result;
        }

        /// <summary>
        /// 异步执行远程Shell命令（非阻塞式，执行完成后通过回调返回结果）
        /// </summary>
        /// <param name="command">要执行的远程命令</param>
        /// <param name="afterExecuted">执行完成后的回调方法（返回执行结果）</param>
        public void Execute(string command, Action<ShellExecuteResult> afterExecuted)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentNullException(nameof(command), "执行的命令不能为空");

            var result = Execute(command);
            // 执行完成后调用回调，返回结果
            afterExecuted?.Invoke(result);
        }

        public string GetEnvironmentVar(string key)
        {
            if (!_environmentVarDic.ContainsKey(key))
            {
                return String.Empty;
            }
            return _environmentVarDic[key];
        }

        public void SetEnvironmentVar(string key, string value)
        {
            _environmentVarDic[key] = value;
        }
    }
}
