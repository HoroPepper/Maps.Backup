using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Interfaces
{
    public interface IMessagePub<T>
    {
        void Publish(T message);
    }
}
