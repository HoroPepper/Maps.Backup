using Maps.Backup.Core.Impls;
using Maps.Backup.Core.Interfaces;
using Maps.Backup.Core.Models;

namespace Maps.Backup.Shell
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FileWorkFlow fileWorkFlow = new FileWorkFlow();
            fileWorkFlow.Execute(Console.ReadLine());
        }
    }
}
