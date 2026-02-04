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

        static void Main(string[] args)
        {
            var configFromFile = LoadConfigFromJson();

            Console.WriteLine("===== Backup Tool =====");
            Console.WriteLine($"Attempted to load config file: {Path.Combine(Environment.CurrentDirectory, ConfigFileName)}");
            Console.WriteLine("Note: Command line parameters will override items with the same name in the config file. Press Enter directly to use only the config file settings.\n");
            Console.WriteLine("Please enter backup command (format: --backUpDir path --localSaveDir path ...):");
            string inputCmd = Console.ReadLine();

            var configFromCmd = string.IsNullOrWhiteSpace(inputCmd)
                ? new Dictionary<string, string>()
                : ParseCmdToKv(inputCmd);

            var finalConfig = MergeConfig(configFromFile, configFromCmd);

            List<string> requiredKeys = new List<string>
            {
                "backUpDir",
                "localSaveDir",
                "dbFileSaveDir",
                "targetDbName"
            };


            var missingKeys = requiredKeys
                .Where(k => !finalConfig.ContainsKey(k) || string.IsNullOrWhiteSpace(finalConfig[k]))
                .ToList();
            if (missingKeys.Any())
            {
                Console.WriteLine($"\nError: Missing required configuration items -> {string.Join(", ", missingKeys)}");
                ShowHelpInfo();
                return;
            }

            try
            {
                Console.WriteLine("\nStarting backup task execution...");
                var msgPub = new DelegateStrMsgPub((msg) =>
                {
                    Console.WriteLine(msg);
                });
                BackUpWorkFlowCreater workFlowCreater = new BackUpWorkFlowCreater(msgPub);
                var taskMgt = workFlowCreater.Create();
                taskMgt.ExecuteAllTasks(null, new TaskContext()
                {
                    ContextDic = finalConfig, 
                    MessagePub = msgPub,
                });
                Console.WriteLine("\n✅ Backup task execution completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Backup task execution failed: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        #region Core new methods: JSON loading, config merging

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

                // Read file content and deserialize to dictionary
                string jsonContent = File.ReadAllText(configPath);
                var configDic = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true, // Support trailing commas in JSON
                    ReadCommentHandling = JsonCommentHandling.Skip // Ignore JSON comments
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
            // First copy basic config from config file
            var finalConfig = new Dictionary<string, string>(fileConfig);
            // Traverse command line config, override same-name items, add new items directly
            foreach (var kv in cmdConfig)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                finalConfig[kv.Key] = kv.Value?.Trim() ?? string.Empty;
            }
            return finalConfig;
        }
        #endregion

        #region Original core methods: Command parsing, help prompt, split with quotes

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

        private static void ShowHelpInfo()
        {
            Console.WriteLine("\n===== Backup Tool Usage Help =====");
            Console.WriteLine("【Method 1: Config File (Recommended, backup-config.json in the same directory as the program)】");
            Console.WriteLine("JSON format example:");
            Console.WriteLine("{");
            Console.WriteLine("  \"backUpDir\": \"/data/backup\",");
            Console.WriteLine("  \"localSaveDir\": \"D:\\\\backup\\\\local\",");
            Console.WriteLine("  \"dbFileSaveDir\": \"D:\\\\backup\\\\db\",");
            Console.WriteLine("  \"targetDbName\": \"map_backup_db\"");
            Console.WriteLine("}");
            Console.WriteLine("\n【Method 2: Command Line Parameters (Can override config file)】");
            Console.WriteLine("Format: --backUpDir path --localSaveDir path --dbFileSaveDir path --targetDbName db_name");
            Console.WriteLine("Example: --backUpDir /data/backup --localSaveDir \"D:\\\\My Backup\\\\local\" --targetDbName map_db");
            Console.WriteLine("【Priority】Command line parameters > JSON config file");
            Console.WriteLine("===========================\n");
        }
        #endregion
    }
}