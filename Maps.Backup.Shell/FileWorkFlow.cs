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
    public class FileWorkFlow
    {
        public void Execute(string qaNo)
        {
            string downLoadRootPath = "\\\\192.168.1.251\\次世代開発\\28.問合せ調査\\調査資料";
            string savePath = "F:\\zhengqiwen\\QASource";
            string uploadRootPath = "\\\\192.168.1.251\\ems共有\\鄭棋文";

            string qaDirName = $"QA{qaNo}";
            IFileService fileService = new ShareDirFileService();
            var files = fileService.FindFile(new Core.Models.FileSearchParam()
            {
                RootPath = Path.Combine(downLoadRootPath, qaDirName)
            });
            List<IFile> downLoadFiles = new List<IFile>();
            string targetDir = Path.Combine(savePath, qaDirName);
            foreach (var file in files)
            {
                downLoadFiles.Add(fileService.Download(file, new Core.Models.FileModel()
                {
                    Path = targetDir
                }));
            }

            List<IFile> upLoadFiles = new List<IFile>();
            foreach (var file in downLoadFiles)
            {
                if(file == null)
                {
                    continue;
                }
                if(file.FileType == "zip")
                {
                    upLoadFiles.Add(fileService.Unzip(file, new FileModel()
                    {
                        Path = targetDir
                    }));
                }
                else
                {
                    upLoadFiles.Add(file);
                }
            }

            foreach (var file in upLoadFiles)
            {
                fileService.Upload(file, new FileModel()
                {
                    Path = Path.Combine(uploadRootPath, qaDirName)
                });
            }
        }
    }
}
