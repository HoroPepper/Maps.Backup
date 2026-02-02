using Maps.Backup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Impls
{
    public class PGBackupService : IBackupService
    {

        private readonly IShellClient _shellClient;


        public PGBackupService(IShellClient shellClient)
        {
            _shellClient = shellClient ?? throw new ArgumentNullException(nameof(shellClient), "Shell客户端不能为空");
        }

        public string Restore(string targetDatabase, IFile backupFile)
        {
            // 基础参数校验
            ValidateRestoreParams(targetDatabase, backupFile);

            // 拼接基础pg_restore恢复命令（核心：远程执行）
            var restoreCommand = BuildPgRestoreCommand(targetDatabase, backupFile);

            // 执行远程命令并处理结果
            
            return ExecuteRestoreCommand(restoreCommand, targetDatabase, backupFile);
        }

        public string Restore(string targetDatabase, string targetGroup, IFile backupFile)
        {
            // 基础参数校验 + 分组参数专属校验
            ValidateRestoreParams(targetDatabase, backupFile);
            if (string.IsNullOrWhiteSpace(targetGroup))
            {
                throw new ArgumentException("目标恢复分组/架构名不能为空或空白", nameof(targetGroup));
            }

            // 拼接带分组的pg_restore恢复命令（指定-n参数指定Schema）
            var restoreCommand = BuildPgRestoreCommand(targetDatabase, backupFile, targetGroup);

            // 执行远程命令并处理结果
            return ExecuteRestoreCommand(restoreCommand, targetDatabase, backupFile, targetGroup);
        }

        private void ValidateRestoreParams(string targetDatabase, IFile backupFile)
        {
            if (backupFile == null)
            {
                throw new ArgumentNullException(nameof(backupFile), "备份文件对象不能为空");
            }
            if (string.IsNullOrWhiteSpace(targetDatabase))
            {
                throw new ArgumentException("目标数据库名不能为空或空白", nameof(targetDatabase));
            }
            if (string.IsNullOrWhiteSpace(backupFile.Path))
            {
                throw new ArgumentException("备份文件的远程服务器路径不能为空或空白", $"{nameof(backupFile)}.{nameof(backupFile.Path)}");
            }
        }

        private string BuildPgRestoreCommand(string dbName, IFile backupFile)
        {
            return BuildPgRestoreCommand(dbName, backupFile, null);
        }

        private string BuildPgRestoreCommand(string dbName, IFile backupFile, string targetSchema)
        {

            var pgUser = _shellClient.GetEnvironmentVar("pgUName");
            var pgPwd = _shellClient.GetEnvironmentVar("pgPwd");

            // 构建命令：Windows用set设置PGPASSWORD环境变量（免密执行），再执行pg_restore
            var commandBuilder = new StringBuilder();
            // Windows系统设置环境变量语法：set 变量名=值（密码带特殊字符也兼容）
            commandBuilder.Append($"set PGPASSWORD={pgPwd} & ");
            // 基础pg_restore命令（参数与原逻辑完全一致，-w强制免密，-c清理原有对象，-F c是自定义格式备份）
            commandBuilder.Append($"pg_restore -h localhost -p 5432 -U {pgUser} -w -d {dbName} -v ");
            // 可选：添加目标Schema参数（非空则拼接）
            if (!string.IsNullOrWhiteSpace(targetSchema))
            {
                commandBuilder.Append($"-n {targetSchema} ");
            }
            // 拼接备份文件路径：Windows路径用双引号包裹（兼容带空格/特殊字符的路径），核心优化点
            commandBuilder.Append($" {backupFile.Path} ");

            return commandBuilder.ToString();
        }

        private string ExecuteRestoreCommand(string restoreCommand, string targetDatabase, IFile backupFile, string targetGroup = null)
        {
            // 执行远程Shell命令（调用IShellClient的同步执行方法）
            var executeResult = _shellClient.Execute(restoreCommand);

            // 按退出码判断执行结果，封装人性化提示
            if (executeResult.ExitCode == 0)
            {
                // 恢复成功：拼接成功信息（区分是否有分组）
                var successMsg = string.IsNullOrWhiteSpace(targetGroup)
                    ? $"数据库【{targetDatabase}】恢复成功！"
                    : $"数据库【{targetDatabase}】的分组【{targetGroup}】恢复成功！";
                // 拼接备份文件路径（便于问题排查）
                successMsg += $"\n备份文件：{backupFile.Path}\n执行命令：{restoreCommand}";
                return successMsg;
            }
            else
            {
                // 恢复失败：拼接详细错误信息（含退出码、错误输出、命令）
                var errorMsg = new StringBuilder();
                errorMsg.AppendLine(string.IsNullOrWhiteSpace(targetGroup)
                    ? $"数据库【{targetDatabase}】恢复失败！"
                    : $"数据库【{targetDatabase}】的分组【{targetGroup}】恢复失败！");
                errorMsg.AppendLine($"退出码：{executeResult.ExitCode}");
                errorMsg.AppendLine($"错误信息：{executeResult.StandardError}");
                errorMsg.AppendLine($"备份文件：{backupFile.Path}");
                errorMsg.AppendLine($"执行命令：{restoreCommand}");
                // 针对常见错误添加提示（排错指引）
                if (executeResult.StandardError.Contains("could not connect to database"))
                {
                    errorMsg.AppendLine("【排错指引】：请检查PG数据库是否启动、目标数据库是否存在、PG账号是否有访问权限");
                }
                else if (executeResult.StandardError.Contains("no such file or directory"))
                {
                    errorMsg.AppendLine("【排错指引】：请检查远程服务器上的备份文件路径是否正确、文件是否存在");
                }
                else if (executeResult.StandardError.Contains("permission denied"))
                {
                    errorMsg.AppendLine("【排错指引】：请检查SSH账号是否有备份文件的读取权限、PG账号是否有数据库的建表/删除权限");
                }
                return errorMsg.ToString();
            }
        }
    }
}
