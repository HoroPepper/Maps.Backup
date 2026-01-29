using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Shell
{
    public class FileShell
    {
        public void Run()
        {
            bool isQuit = false;
            IFileService fileService = new WinSharedFileService();
            while (!isQuit)
            {

                string commandStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(commandStr))
                {
                    continue;
                }
                List<string> paramList = commandStr
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                string commandType = paramList[0].ToLower();

                string commandParam1 = paramList.Count >= 2 ? paramList[1] : String.Empty;
                string commandParam2 = paramList.Count >= 3 ? paramList[2] : String.Empty;
                try
                {
                    switch (commandType)
                    {
                        case "down":
                            {
                                if (String.IsNullOrWhiteSpace(commandParam1) || String.IsNullOrWhiteSpace(commandParam2))
                                {
                                    Console.WriteLine("参数异常");
                                    break;
                                }
                                fileService.Download(new FileModel() { Path = commandParam1 }, new FileModel() { Path = commandParam2 });
                                Console.WriteLine("下载完成");
                                break;
                            }
                        case "up":
                            {
                                if (String.IsNullOrWhiteSpace(commandParam1) || String.IsNullOrWhiteSpace(commandParam2))
                                {
                                    Console.WriteLine("参数异常");
                                    break;
                                }
                                fileService.Upload(new FileModel() { Path = commandParam1 }, new FileModel() { Path = commandParam2 });
                                Console.WriteLine("上传完成");
                                break;
                            }
                        case "unzip":
                            {
                                if (String.IsNullOrWhiteSpace(commandParam1) || String.IsNullOrWhiteSpace(commandParam2))
                                {
                                    Console.WriteLine("参数异常");
                                    break;
                                }
                                fileService.Unzip(new FileModel() { Path = commandParam1 }, new FileModel() { Path = commandParam2 });
                                Console.WriteLine("解压完成");
                                break;
                            }
                        case "quit":
                            {
                                isQuit = true;
                                break;
                            }
                        default:
                            {
                                Console.WriteLine("命令未定义");
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }


            }

            Console.WriteLine("已退出");
        }
    }
}
