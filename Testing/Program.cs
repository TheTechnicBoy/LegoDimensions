
using LegoDimensions;

namespace Testing
{
    internal class Program
    {
        public static Portal portal = new Portal(true);

        static async Task Main(string[] args)
        {
            if (portal == null) return;
            portal.PortalTagEvent += PortalTagEvent;
            portal.nfcEnabled = true;

            await Task.Delay(5000);

            portal.SetColor(Pad.Center, Color.Red);
            portal.SetColor(Pad.Left, Color.Green);
            portal.SetColor(Pad.Right, Color.Blue);
            Console.ReadLine();

            portal.Close();

            return;
        }

        private static void PortalTagEvent(PortalTagEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.White;
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
                Console.WriteLine("Setting Pasword: ");
                portal.SetTagPassword(PortalPassword.Disable, 0);


                Console.WriteLine("Reading tag...");
                bool timeout;
                List<byte[]> data_ = portal.DumpTag(out timeout, (byte)args.ID);
                Console.WriteLine("Timeout: " + timeout);

                foreach (var item in data_)
                {
                    Console.WriteLine(BitConverter.ToString(item));
                }


                Console.WriteLine("Writing tag...");

                byte[] bytes = portal.ReadTag((byte)args.ID, 16);
                bytes[0]++;
                Console.WriteLine(portal.WriteTag((byte)args.ID, 16, bytes));

                Console.WriteLine("Now Set To " + bytes[0]);
            }
            Console.WriteLine(portal.presentTags.Count);
        }
    }
}
