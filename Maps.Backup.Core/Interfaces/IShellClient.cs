using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IShellClient
    {
        void Excete(string command);

        void Excete(string command, Action afterExceted);

    }
}
