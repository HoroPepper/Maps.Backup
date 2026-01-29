using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IShellClient
    {
        // <summary>
        /// 执行Shell命令
        /// </summary>
        /// <param name="command">要执行的Shell命令</param>
        /// <returns>命令执行结果（包含标准输出、错误输出、退出码）</returns>
        ShellExecuteResult Execute(string command);

        /// <summary>
        /// 执行Shell命令（带执行后回调）
        /// </summary>
        /// <param name="command">要执行的Shell命令</param>
        /// <param name="afterExecuted">执行后回调方法，参数为命令执行结果</param>
        void Execute(string command, Action<ShellExecuteResult> afterExecuted);

    }
}
