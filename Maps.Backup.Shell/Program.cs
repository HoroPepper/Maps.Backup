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
        private const string ConfigFileName = "backup-config.json";
        private static readonly List<string> SupportedCommands = new() { "restore", "help", "quit"};

        static void Main(string[] args)
        {
            var configFromFile = LoadConfigFromJson();

            Console.WriteLine("===== Backup & Restore Tool =====");
            Console.WriteLine($"Attempted to load config file: {Path.Combine(Environment.CurrentDirectory, ConfigFileName)}");
            Console.WriteLine("Note: Command line parameters will override items with the same name in the config file. Press Enter directly to use only the config file settings.\n");
            Console.WriteLine("Please enter operation command (format: [restore/help] --backUpDir path --localSaveDir path ...):");

            bool isQuit = false;
            while(!isQuit)
            {
                string inputCmd = Console.ReadLine();

                string mainCommand = string.Empty;
                Dictionary<string, string> configFromCmd = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(inputCmd))
                {
                    var cmdParseResult = SplitMainCommandAndKvParams(inputCmd);
                    mainCommand = cmdParseResult.MainCommand;
                    configFromCmd = cmdParseResult.KvParams;
                }
                var finalConfig = MergeConfig(configFromFile, configFromCmd);

                if (!SupportedCommands.Contains(mainCommand, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"\nError: Unsupported command -> {mainCommand}");
                    Console.WriteLine($"Supported commands: {string.Join(", ", SupportedCommands)}");
                }
                else if (mainCommand.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    ShowHelpInfo();
                }
                else if(mainCommand.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    isQuit = true;
                }
                else if (mainCommand.Equals("restore", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Console.WriteLine($"\nStarting restore task execution...");
                        var msgPub = new DelegateStrMsgPub((msg) =>
                        {
                            Console.WriteLine(msg);
                        });
                        BackUpWorkFlowCreater workFlowCreater = new BackUpWorkFlowCreater(msgPub);
                        var missingKeys = workFlowCreater.RequiredKeys
                       .Where(k => !finalConfig.ContainsKey(k) || string.IsNullOrWhiteSpace(finalConfig[k]))
                       .ToList();
                        if (missingKeys.Any())
                        {
                            Console.WriteLine($"\nError: Missing required configuration items for [{mainCommand}] -> {string.Join(", ", missingKeys)}");
                            return;
                        }
                        var taskMgt = workFlowCreater.Create();
                        taskMgt.ExecuteAllTasks(null, new TaskContext()
                        {
                            ContextDic = finalConfig,
                            MessagePub = msgPub,
                        });
                        Console.WriteLine($"\n V restore task execution completed!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n X restore task execution failed: {ex.Message}");
                        Console.WriteLine($"Exception details: {ex.StackTrace}");
                    }
                }
                else
                {
                    Console.WriteLine($"\nError: Unsupported command -> {mainCommand}");
                    Console.WriteLine($"Supported commands: {string.Join(", ", SupportedCommands)}");
                }
                
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        #region 新增核心方法：拆分首位命令和后续键值对参数
        /// <summary>
        /// 拆分输入命令：首位无--的主命令 + 后续--开头的键值对参数
        /// </summary>
        /// <param name="cmd">原始输入命令</param>
        /// <returns>主命令 + 键值对参数字典</returns>
        private static ShellCommand SplitMainCommandAndKvParams(string cmd)
        {
            string mainCommand = string.Empty;
            var allParts = SplitCmdWithQuotes(cmd).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

            if (allParts.Count > 0)
            {
                // 首位不是--开头，判定为主命令
                if (!allParts[0].StartsWith("--"))
                {
                    mainCommand = allParts[0].ToLower();
                    // 移除主命令，剩余部分作为键值对参数解析
                    allParts.RemoveAt(0);
                }
            }

            // 解析剩余部分为键值对（复用原有ParseCmdToKv逻辑，适配拆分后的参数）
            var kvParams = ParseCmdToKvFromParts(allParts);
            return new ShellCommand()
            {
                MainCommand = mainCommand,
                KvParams = kvParams,
            };
        }

        /// <summary>
        /// 从拆分后的参数列表解析键值对（复用原有逻辑，解耦字符串输入）
        /// </summary>
        private static Dictionary<string, string> ParseCmdToKvFromParts(List<string> cmdParts)
        {
            Dictionary<string, string> kvDic = new Dictionary<string, string>();
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
        #endregion

        #region 原有核心方法：JSON加载、配置合并（无修改）
        private static Dictionary<string, string> LoadConfigFromJson()
        {
            try
            {
                string configPath = Path.Combine(Environment.CurrentDirectory, ConfigFileName);
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"Note: Config file does not exist, will only use command line parameters");
                    return new Dictionary<string, string>();
                }

                string jsonContent = File.ReadAllText(configPath);
                var configDic = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                return configDic ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load/parse config file, will only use command line parameters | Error: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        private static Dictionary<string, string> MergeConfig(Dictionary<string, string> fileConfig, Dictionary<string, string> cmdConfig)
        {
            var finalConfig = new Dictionary<string, string>(fileConfig);
            foreach (var kv in cmdConfig)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                finalConfig[kv.Key] = kv.Value?.Trim() ?? string.Empty;
            }
            return finalConfig;
        }
        #endregion

        #region 原有方法：命令拆分、键值对解析（仅解耦ParseCmdToKv，原逻辑保留）

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
        #endregion

        #region 改造HelpInfo：更新使用说明，包含新命令
        private static void ShowHelpInfo()
        {
            Console.WriteLine("\n===== Backup & Restore Tool Usage Help =====");
            Console.WriteLine("【Supported Main Commands】");
            Console.WriteLine("  restore  - Execute restore operation");
            Console.WriteLine("  help     - Show this usage help information");
            Console.WriteLine("\n【Method 1: Config File (Recommended, backup-config.json in the same program directory)】");
            Console.WriteLine("JSON format example (supports both backup/restore):");
            Console.WriteLine("{");
            Console.WriteLine("  \"backUpDir\": \"/data/backup\",");
            Console.WriteLine("  \"localSaveDir\": \"D:\\\\backup\\\\local\",");
            Console.WriteLine("  \"dbFileSaveDir\": \"D:\\\\backup\\\\db\",");
            Console.WriteLine("  \"targetDbName\": \"map_backup_db\"");
            Console.WriteLine("}");
            Console.WriteLine("\n【Method 2: Command Line Parameters (Overrides config file items with the same name)】");
            Console.WriteLine("Basic Format: [main-command] --key1 value1 --key2 value2 ...");
            Console.WriteLine("Examples:");
            Console.WriteLine("  1. Backup (default): --backUpDir /data/backup --localSaveDir \"D:\\\\My Backup\"");
            Console.WriteLine("  2. Restore: restore --backUpDir /data/backup --localSaveDir \"D:\\\\My Backup\"");
            Console.WriteLine("  3. Direct help: help");
            Console.WriteLine("\n【Priority】Command line parameters > JSON config file");
            Console.WriteLine("【Note】All commands share the same required configuration items for now");
            Console.WriteLine("==============================================\n");
        }
        #endregion
    }
}