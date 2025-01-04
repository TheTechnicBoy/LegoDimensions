using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing
{
    internal class Test
    {
        internal Thread _readThread;
        internal CancellationTokenSource _cancelThread;
        public Test()
        {
            _cancelThread = new CancellationTokenSource();
            _readThread = new Thread(Run);
            _readThread.Start();
        }


        public void test()
        {
            Thread.Sleep(2000);
        }


        public void Run() {
            while (!_cancelThread.IsCancellationRequested)
            {
                Console.WriteLine("Hello World!");
                Thread.Sleep(1000);
            }
        }
    }
}
