
using LegoDimensions;
using System;
using System.Net.Http.Headers;

namespace Testing
{
    internal class Program
    {
        public static Portal portal = new Portal(true);
        
        static void Main(string[] args)
        {
            if(portal == null) return;
            portal.PortalTagEvent += PortalTagEvent;

            //Thread.Sleep(3000);
            //portal.SetFades(new FadeProperties(new Color(10, 0, 0), 100, 255), new FadeProperties(new Color(0, 10, 0), 100, 255), new FadeProperties(new Color(0, 0, 10), 100, 255));
            Console.ReadLine();

            portal.Close();
        }

        private static void PortalTagEvent(PortalTagEventArgs args)
        {
            try
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
                    byte[] newByte = new byte[4];
                    Array.Copy(bytes, 0, newByte, 0, 4);
                    newByte[0]++;
                    Console.WriteLine(portal.WriteTag((byte)args.ID, 16, newByte));

                    Console.WriteLine("Now Set To " + newByte[0]);



                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            

            Console.WriteLine(portal.presentTags.Count);
        }
    }
}
