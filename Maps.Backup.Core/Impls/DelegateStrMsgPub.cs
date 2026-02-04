using Maps.Backup.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maps.Backup.Core.Impls
{
    public class DelegateStrMsgPub : IMessagePub<string>
    {

        private readonly Action<string> _consumeAction;

        public DelegateStrMsgPub(Action<string> consumeAction)
        {
            _consumeAction += consumeAction;
        }
        public void Publish(string message)
        {
            _consumeAction?.Invoke(message);
        }
    }
}
