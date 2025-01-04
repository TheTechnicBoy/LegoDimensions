﻿
using LegoDimensions;

namespace Testing
{
    internal class Program
    {
        public static Portal portal;
        static void Main(string[] args)
        {
            _Main();
            //Test test = new Test();
            //test.test();
            //Console.WriteLine("AFTER Thread.sleep()");
        }

        public static void _Main()
        {
            portal = new Portal(false);
            portal.PortalTagEvent += PortalTagEvent;

            //Thread.Sleep(3000);
            //portal.SetFades(new FadeProperties(new Color(10, 0, 0), 100, 255), new FadeProperties(new Color(0, 10, 0), 100, 255), new FadeProperties(new Color(0, 0, 10), 100, 255));
            Console.ReadLine();

            portal.Close();
        }

        private static void PortalTagEvent(PortalTagEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\r");
            Console.Write(DateTime.Now.ToString("HH:mm:ss"));
            Console.Write(" - ");

            Console.Write("[");
            if (args.Placed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("+");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("-");
                Console.ForegroundColor = ConsoleColor.White;
            }
            Console.Write("]");

            Console.Write(" - ");
            Console.Write(args.Pad.ToString());
            Console.Write(" - ");
            Console.WriteLine(BitConverter.ToString(args.UUID));

            if (args.Placed)
            {
                
                Console.WriteLine("Reading tag...");

                List<byte[]> data = portal.DumpTag((byte) args.ID);

                foreach (var item in data)
                {
                    Console.WriteLine(BitConverter.ToString(item));
                }

                Thread.Sleep(1000);

                Console.WriteLine("Writing tag...");

                portal.WriteTag((byte)args.ID,  4,  new byte[] { 0x01, 0x03,0xA0, 0x0C });

            }
        }
    }
}
