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

        /// <summary>
        /// 执行Shell脚本
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        ShellExecuteResult Execute(string command);

        /// <summary>
        /// 设置环境变量
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetEnvironmentVar(string key, string value);

        /// <summary>
        /// 获取环境变量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetEnvironmentVar(string key);

    }
}
