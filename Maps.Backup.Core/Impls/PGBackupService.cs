using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
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
        public bool Restore(string targetDatabase, string targetGroup, IFile backupFile)
        {
            ValidateRestoreParams(targetDatabase, backupFile);

            var restoreCommand = BuildPgRestoreCommand(targetDatabase, backupFile, targetGroup);

            // 执行远程命令并处理结果
            var result = ExecuteRestoreCommand(restoreCommand, targetDatabase, backupFile, targetGroup);

            return result.IsSuccess;
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

        private string BuildPgRestoreCommand(string dbName, IFile backupFile, string targetSchema)
        {

            var pgUser = _shellClient.GetEnvironmentVar("pgUName");
            var pgPwd = _shellClient.GetEnvironmentVar("pgPwd");

            var commandBuilder = new StringBuilder();
            commandBuilder.Append($"set PGPASSWORD={pgPwd}\n ");
            commandBuilder.Append($"pg_restore -h localhost -p 5432 -U {pgUser} -w -d {dbName} -v ");
            // 可选：添加目标Schema参数（非空则拼接）
            if (!string.IsNullOrWhiteSpace(targetSchema))
            {
                commandBuilder.Append($"-n {targetSchema} ");
            }
            commandBuilder.Append($" {backupFile.RealPath} ");

            return commandBuilder.ToString();
        }

        private ShellExecuteResult ExecuteRestoreCommand(string restoreCommand, string targetDatabase, IFile backupFile, string targetGroup = null)
        {
            // 执行远程Shell命令（调用IShellClient的同步执行方法）
            var executeResult = _shellClient.Execute(restoreCommand);

            return executeResult;
        }

        
    }
}
