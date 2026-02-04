using Maps.Backup.Core;
using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using Maps.Backup.Core.TaskNodes;
using Maps.Backup.WorkFlowLib;
using Renci.SshNet.Messages;
using System.Text;
using System.Text.Json;

namespace Maps.Backup.Shell
{
    internal class Program
    {
        // 定义JSON配置文件名称（与程序同目录）
        private const string ConfigFileName = "backup-config.json";

        static void Main(string[] args)
        {
            // 1. 加载JSON配置文件，获取基础配置字典
            var configFromFile = LoadConfigFromJson();

            // 2. 读取控制台命令行输入，引导用户操作
            Console.WriteLine("===== 备份工具 =====");
            Console.WriteLine($"已尝试加载配置文件：{Path.Combine(Environment.CurrentDirectory, ConfigFileName)}");
            Console.WriteLine("提示：命令行参数会覆盖配置文件同名项，直接回车则仅使用配置文件配置\n");
            Console.WriteLine("请输入备份命令（格式：--backUpDir 路径 --localSaveDir 路径 ...）：");
            string inputCmd = Console.ReadLine();

            // 3. 解析命令行参数（无输入则返回空字典）
            var configFromCmd = string.IsNullOrWhiteSpace(inputCmd)
                ? new Dictionary<string, string>()
                : ParseCmdToKv(inputCmd);

            // 4. 合并配置：命令行配置覆盖配置文件同名项
            var finalConfig = MergeConfig(configFromFile, configFromCmd);

            // 5. 定义必填配置项（与ContextDic键名一致）
            List<string> requiredKeys = new List<string>
            {
                "backUpDir",
                "localSaveDir",
                "dbFileSaveDir",
                "targetDbName"
            };

            // 6. 校验必填项是否完整（最终合并后的配置）
            var missingKeys = requiredKeys
                .Where(k => !finalConfig.ContainsKey(k) || string.IsNullOrWhiteSpace(finalConfig[k]))
                .ToList();
            if (missingKeys.Any())
            {
                Console.WriteLine($"\n错误：缺失必填配置项 -> {string.Join("、", missingKeys)}");
                ShowHelpInfo();
                return;
            }

            // 7. 初始化工作流并执行任务（注入合并后的最终配置）
            try
            {
                Console.WriteLine("\n开始执行备份任务...");
                var msgPub = new DelegateStrMsgPub((msg) =>
                {
                    Console.WriteLine(msg);
                });
                BackUpWorkFlowCreater workFlowCreater = new BackUpWorkFlowCreater(msgPub);
                var taskMgt = workFlowCreater.Create();
                taskMgt.ExecuteAllTasks(null, new TaskContext()
                {
                    ContextDic = finalConfig, // 注入合并后的配置
                    MessagePub = msgPub,
                });
                Console.WriteLine("\n✅ 备份任务执行完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 备份任务执行失败：{ex.Message}");
                // 调试时开启：打印详细异常堆栈
                // Console.WriteLine($"\n异常详情：{ex.StackTrace}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        #region 核心新增方法：JSON加载、配置合并
        /// <summary>
        /// 从程序同目录加载backup-config.json配置文件
        /// 文件不存在/格式错误则返回空字典，不终止程序
        /// </summary>
        /// <returns>配置文件中的键值对字典</returns>
        private static Dictionary<string, string> LoadConfigFromJson()
        {
            try
            {
                string configPath = Path.Combine(Environment.CurrentDirectory, ConfigFileName);
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"提示：配置文件不存在，将仅使用命令行参数");
                    return new Dictionary<string, string>();
                }

                // 读取文件内容并反序列化为字典
                string jsonContent = File.ReadAllText(configPath);
                var configDic = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true, // 支持JSON末尾逗号
                    ReadCommentHandling = JsonCommentHandling.Skip // 忽略JSON注释
                });

                return configDic ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：配置文件加载/解析失败，将仅使用命令行参数 | 错误：{ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 合并配置：命令行配置覆盖配置文件中的同名项
        /// 非同名项保留，实现「配置文件兜底、命令行灵活覆盖」
        /// </summary>
        /// <param name="fileConfig">配置文件加载的配置</param>
        /// <param name="cmdConfig">命令行解析的配置</param>
        /// <returns>合并后的最终配置</returns>
        private static Dictionary<string, string> MergeConfig(Dictionary<string, string> fileConfig, Dictionary<string, string> cmdConfig)
        {
            // 先复制配置文件的基础配置
            var finalConfig = new Dictionary<string, string>(fileConfig);
            // 遍历命令行配置，覆盖同名项，新增项直接添加
            foreach (var kv in cmdConfig)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                finalConfig[kv.Key] = kv.Value?.Trim() ?? string.Empty;
            }
            return finalConfig;
        }
        #endregion

        #region 原有核心方法：命令解析、帮助提示、带引号分割
        /// <summary>
        /// 解析--key value格式的命令为键值对字典
        /// 支持值包含空格（需用双引号包裹，如--key "a b c"）
        /// </summary>
        private static Dictionary<string, string> ParseCmdToKv(string cmd)
        {
            Dictionary<string, string> kvDic = new Dictionary<string, string>();
            var cmdParts = SplitCmdWithQuotes(cmd).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

            for (int i = 0; i < cmdParts.Count; i++)
            {
                if (cmdParts[i].StartsWith("--") && cmdParts[i].Length > 2)
                {
                    string key = cmdParts[i].Substring(2);
                    if (i + 1 < cmdParts.Count)
                    {
                        string value = cmdParts[i + 1].Trim('"');
                        kvDic[key] = value;
                        i++;
                    }
                }
            }

            return kvDic;
        }

        /// <summary>
        /// 智能分割命令行，保留双引号内的空格（解决值含空格场景）
        /// </summary>
        private static List<string> SplitCmdWithQuotes(string cmd)
        {
            List<string> parts = new List<string>();
            bool inQuote = false;
            StringBuilder currentPart = new StringBuilder();

            foreach (char c in cmd.Trim())
            {
                if (c == '"')
                {
                    inQuote = !inQuote;
                    currentPart.Append(c);
                }
                else if (c == ' ' && !inQuote)
                {
                    if (currentPart.Length > 0)
                    {
                        parts.Add(currentPart.ToString());
                        currentPart.Clear();
                    }
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
            }

            return parts;
        }

        /// <summary>
        /// 显示帮助信息（含配置文件格式、命令行格式）
        /// </summary>
        private static void ShowHelpInfo()
        {
            Console.WriteLine("\n===== 备份工具使用帮助 =====");
            Console.WriteLine("【方式1：配置文件（推荐，backup-config.json 与程序同目录）】");
            Console.WriteLine("JSON格式示例：");
            Console.WriteLine("{");
            Console.WriteLine("  \"backUpDir\": \"/data/backup\",");
            Console.WriteLine("  \"localSaveDir\": \"D:\\\\backup\\\\local\",");
            Console.WriteLine("  \"dbFileSaveDir\": \"D:\\\\backup\\\\db\",");
            Console.WriteLine("  \"targetDbName\": \"map_backup_db\"");
            Console.WriteLine("}");
            Console.WriteLine("\n【方式2：命令行参数（可覆盖配置文件）】");
            Console.WriteLine("格式：--backUpDir 路径 --localSaveDir 路径 --dbFileSaveDir 路径 --targetDbName 库名");
            Console.WriteLine("示例：--backUpDir /data/backup --localSaveDir \"D:\\\\我的备份\\\\local\" --targetDbName map_db");
            Console.WriteLine("【优先级】命令行参数 > JSON配置文件");
            Console.WriteLine("===========================\n");
        }
        #endregion
    }



}

