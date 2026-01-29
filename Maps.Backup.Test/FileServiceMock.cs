using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Test
{
    public class FileServiceMock : IFileService
    {
        public IFile Download(IFile remoteFile, IFile saveFile)
        {
            return new FileMock();
        }

        public List<IFile> FindFileByExtension(FileSearchParam searchParam)
        {
            return new List<IFile> { new FileMock(), new FileMock() };
        }

        public IFile Unzip(IFile zipFile, IFile targetExtractFile)
        {
            return new FileMock();
        }

        public IFile Upload(IFile localFile, IFile targetFile)
        {
            return new FileMock();
        }
    }
}
