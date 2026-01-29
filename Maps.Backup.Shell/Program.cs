using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;

namespace Maps.Backup.Shell
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FileShell shell = new FileShell();
            shell.Run();
        }
    }
}
