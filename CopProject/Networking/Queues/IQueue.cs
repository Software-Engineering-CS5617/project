using Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking.Queues
{
    public interface IQueue
    {
        public void Enqueue(Packet packet);

        public Packet Dequeue();

        public Packet Peek();

        public void Clear();

        public int Size();

        public bool IsEmpty();

    }
}
