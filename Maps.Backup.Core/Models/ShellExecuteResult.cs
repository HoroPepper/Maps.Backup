using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Models
{
    public class ShellExecuteResult
    {
        /// <summary>
        /// 命令执行的标准输出（stdout）
        /// </summary>
        public string StandardOutput { get; set; } = string.Empty;

        /// <summary>
        /// 命令执行的错误输出（stderr）
        /// </summary>
        public string StandardError { get; set; } = string.Empty;

        /// <summary>
        /// 命令退出码（0表示执行成功，非0表示执行失败，不同终端退出码语义可能不同）
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// 快捷判断是否执行成功（退出码为0且无错误输出）
        /// </summary>
        public bool IsSuccess => ExitCode == 0 && string.IsNullOrWhiteSpace(StandardError);
    }
}
