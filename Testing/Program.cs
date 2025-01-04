
using LegoDimensions;

namespace Testing
{
    internal class Program
    {
        public static Portal portal;
        static void Main(string[] args)
        {
            Test test = new Test();
            test.test();
            Console.WriteLine("AFTER Thread.sleep()");
        }

        public static void _Main()
        {
            portal = new Portal(true);
            portal.PortalTagEvent += PortalTagEvent;

            Thread.Sleep(3000);
            portal.SetFades(new FadeProperties(new Color(10, 0, 0), 100, 255), new FadeProperties(new Color(0, 10, 0), 100, 255), new FadeProperties(new Color(0, 0, 10), 100, 255));
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
                byte[] test1 = portal.ReadTag((byte)args.ID, 4);
                byte[] test2 = portal.ReadTag((byte)args.ID, 5);
            }
        }
    }
}
