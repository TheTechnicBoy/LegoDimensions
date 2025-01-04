﻿using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.ComponentModel;
using System.Reflection;

namespace LegoDimensions
{
    public class Portal
    {
        //Constants
        internal const int ProductId = 0x0241;
        internal const int VendorId = 0x0E6F;
        internal const int ReadWriteTimeout = 1000;
        internal const int ReceiveTimeout = 2000;

        // Class variables
        internal IUsbDevice _portal;
        internal UsbEndpointReader _endpointReader;
        internal UsbEndpointWriter _endpointWriter;
        internal Thread _readThread;
        internal CancellationTokenSource _cancelThread;

        //Variables
        internal bool _Debug = false;
        internal bool _startStoppAnimations;
        internal byte _messageID;
        internal Dictionary<int, MessageCommand> _CommandDictionary;
        internal Dictionary<int, object> _FormatedResponse;

        //Event
        public Action<PortalTagEventArgs> PortalTagEvent;

        #region Start/Stop
        public Portal(bool StartStoppAnimations = true, IUsbDevice? usbDevice = null)
        {
            _CommandDictionary = new Dictionary<int, MessageCommand>();
            _FormatedResponse = new Dictionary<int, object>();
            _messageID = 0;
            _startStoppAnimations = StartStoppAnimations;

            if (usbDevice != null)
            {
                if (usbDevice.ProductId == ProductId && usbDevice.VendorId == VendorId)
                {
                    _portal = usbDevice;
                }
                else
                {
                    throw new Exception("IUsbDevice is not a valid Lego Dimension Portal.");
                }
            }
            else
            {
                _portal = GetPortal();
            }


            //Open the device
            _portal.Open();

            //Get the first config number of the interface
            _portal.ClaimInterface(_portal.Configs[0].Interfaces[0].Number);

            //Open up the endpoints
            _endpointWriter = _portal.OpenEndpointWriter(WriteEndpointID.Ep01);
            _endpointReader = _portal.OpenEndpointReader(ReadEndpointID.Ep01);

            // Read the first 32 bytes
            var readBuffer = new byte[32];
            _endpointReader.Read(readBuffer, ReadWriteTimeout, out var bytesRead);

            //Reader
            _cancelThread = new CancellationTokenSource();
            _readThread = new Thread(ReadThread);
            _readThread.Start();

            //Activate
            WakeUp();

            //Animation
            if (_startStoppAnimations)
            {
                Task.Run(() =>
                {
                    SetFade(Pad.Center, new FadeProperties(new Color(50, 0, 0), 60, 1));
                    Thread.Sleep(1000);
                    SetFade(Pad.Left, new FadeProperties(new Color(0, 50, 0), 60, 1));
                    Thread.Sleep(1000);
                    SetFade(Pad.Right, new FadeProperties(new Color(0, 0, 50), 60, 1));
                });
            }
        }
        public void Close()
        {
            _cancelThread.Cancel();

            if (_startStoppAnimations)
            {
                SetFade(Pad.All, new FadeProperties(new Color(0, 0, 0), 20, 1));
            }

            Thread.Sleep(1000);

            _portal.ReleaseInterface(_portal.Configs[0].Interfaces[0].Number);
            _portal.Close();
            _portal.Dispose();
        }
        #endregion


        #region Ulitiliy
        internal void WakeUp()
        {
            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x0f; //length
            byte_[2] = (byte)MessageCommand.Wake; //command
            byte_[3] = MessageID_; //Message ID (i think)
            byte_[4] = 0x28; //'('
            byte_[5] = 0x63; //'c'
            byte_[6] = 0x29; //')'
            byte_[7] = 0x20; //' '
            byte_[8] = 0x4c; //'L'
            byte_[9] = 0x45; //'E'
            byte_[10] = 0x47;//'G'
            byte_[11] = 0x4f;//'O'
            byte_[12] = 0x20;//' '
            byte_[13] = 0x32;//'2'
            byte_[14] = 0x30;//'0'
            byte_[15] = 0x31;//'1'
            byte_[16] = 0x34;//'4'
            byte_[17] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }
        internal static byte ComputeAdditionChecksum(byte[] data)
        {
            byte sum = 0;
            foreach (byte b in data)
            {
                sum += b;
            }
            return sum;
        }
        internal int SendMessage(byte[] message)
        {
            _CommandDictionary.Add(message[3], (MessageCommand)message[2]);
            _endpointWriter.Write(message, ReadWriteTimeout, out int _numBytes);
            Console.WriteLine("Written " + (MessageCommand)message[2]);
            return _numBytes;
        }
        internal IUsbDevice GetPortal()
        {
            var context = new UsbContext();
            var usbdeviceCollection = context.List();
            var selectedDevice = usbdeviceCollection.Where(d => d.ProductId == ProductId && d.VendorId == VendorId);
            var portals = selectedDevice.ToArray();
            if (portals.Length == 0)
            {
                throw new Exception("No Lego Dimensions Portal found.");
            }
            return portals[0];
        }
        #endregion


        #region Colors
        public void SetColors(Color? center = null, Color? left = null, Color? right = null)
        {
            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x0e; //command length
            byte_[2] = (byte)MessageCommand.ColorAll; //command
            byte_[3] = MessageID_; //Message ID (i think)

            if (center != null)
            {
                byte_[4] = 1; //platform enabled
                byte_[5] = center.Red; // r
                byte_[6] = center.Green; // g
                byte_[7] = center.Blue; // b
            }

            if (left != null)
            {
                byte_[8] = 1; //platform enabled
                byte_[9] = left.Red; // r
                byte_[10] = left.Green; // g
                byte_[11] = left.Blue; // b
            }

            if (right != null)
            {
                byte_[12] = 1; //platform enabled
                byte_[13] = right.Red; // r
                byte_[14] = right.Green; // g
                byte_[15] = right.Blue; // b
            }

            byte_[16] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }
        public void SetColor(Pad pad, Color color)
        {
            if (color == null) throw new ArgumentNullException(nameof(color));

            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x06; //command length
            byte_[2] = (byte)MessageCommand.Color; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)pad; //platform
            byte_[5] = color.Red; // r
            byte_[6] = color.Green; // g
            byte_[7] = color.Blue; // b

            byte_[8] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }

        public byte[] ReadTag(byte index, byte page)
        {
            var waitHandle = new ManualResetEvent(false);
            byte[] result = null;

            byte[] byte_ = new byte[32];
            var MessageID_ = _messageID++;

            byte_[0] = 0x55; //start
            byte_[1] = 0x04; //command length
            byte_[2] = (byte)MessageCommand.Read; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = index;
            byte_[5] = page;

            byte_[6] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                result = (byte[])_FormatedResponse[MessageID_];
                _FormatedResponse.Remove(MessageID_);
            }
            else
            {
                //throw new TimeoutException("Timeout beim Warten auf die Antwort vom Portal.");
            }

            return result;
        }

        public void SetFlashs(FlashProperties? center = null, FlashProperties? left = null, FlashProperties? right = null)
        {
            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x17; //command length
            byte_[2] = (byte)MessageCommand.FlashAll; //command
            byte_[3] = MessageID_;//Message ID (i think)

            if (center != null)
            {
                byte_[4] = 1;
                byte_[5] = center.OnLen;
                byte_[6] = center.OffLen;
                byte_[7] = center.PulseCnt;
                byte_[8] = center.Color.Red;
                byte_[9] = center.Color.Green;
                byte_[10] = center.Color.Blue;
            }

            if (left != null)
            {
                byte_[11] = 1;
                byte_[12] = left.OnLen;
                byte_[13] = left.OffLen;
                byte_[14] = left.PulseCnt;
                byte_[15] = left.Color.Red;
                byte_[16] = left.Color.Green;
                byte_[17] = left.Color.Blue;
            }

            if (right != null)
            {
                byte_[18] = 1;
                byte_[19] = right.OnLen;
                byte_[20] = right.OffLen;
                byte_[21] = right.PulseCnt;
                byte_[22] = right.Color.Red;
                byte_[23] = right.Color.Green;
                byte_[24] = right.Color.Blue;
            }

            byte_[25] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }
        public void SetFlash(Pad pad, FlashProperties flashProperties)
        {
            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x09; //command length
            byte_[2] = (byte)MessageCommand.Flash; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)pad; //platform
            byte_[5] = flashProperties.OnLen; //light on length
            byte_[6] = flashProperties.OffLen; //light off length
            byte_[7] = flashProperties.PulseCnt; //number of pulses
            byte_[8] = flashProperties.Color.Red; // r
            byte_[9] = flashProperties.Color.Green; // g
            byte_[10] = flashProperties.Color.Blue; // b

            byte_[11] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }


        public void SetFades(FadeProperties? center = null, FadeProperties? left = null, FadeProperties? right = null)
        {
            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x14; //command length
            byte_[2] = (byte)MessageCommand.FadeAll; //command
            byte_[3] = MessageID_; //Message ID (i think)

            if (center != null)
            {
                byte_[4] = 1;
                byte_[5] = center.FadeLen;
                byte_[6] = center.PulseCnt;
                byte_[7] = center.Color.Red;
                byte_[8] = center.Color.Green;
                byte_[9] = center.Color.Blue;
            }

            if (left != null)
            {
                byte_[10] = 1;
                byte_[11] = left.FadeLen;
                byte_[12] = left.PulseCnt;
                byte_[13] = left.Color.Red;
                byte_[14] = left.Color.Green;
                byte_[15] = left.Color.Blue;
            }

            if (right != null)
            {
                byte_[16] = 1;
                byte_[17] = right.FadeLen;
                byte_[18] = right.PulseCnt;
                byte_[19] = right.Color.Red;
                byte_[20] = right.Color.Green;
                byte_[21] = right.Color.Blue;
            }

            byte_[22] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }
        public void SetFade(Pad pad, FadeProperties fadeProperties)
        {
            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x08; //command length
            byte_[2] = (byte)MessageCommand.Fade; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)pad; //platform
            byte_[5] = fadeProperties.FadeLen; //light on length
            byte_[6] = fadeProperties.PulseCnt; //number of pulses
            byte_[7] = fadeProperties.Color.Red; // r
            byte_[8] = fadeProperties.Color.Green; // g
            byte_[9] = fadeProperties.Color.Blue; // b

            byte_[10] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }
        public void FadeRanom(Pad pad, RandomFadeProperties randomFadeProperties)
        {
            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x05; //command length
            byte_[2] = (byte)MessageCommand.FadeRandom; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)pad; //platform
            byte_[5] = randomFadeProperties.FadeLen; //light on length
            byte_[6] = randomFadeProperties.PulseCnt; //number of pulses

            byte_[7] = ComputeAdditionChecksum(byte_);

            SendMessage(byte_);
        }
        #endregion

        private void ReadThread(object? obj)
        {
            var readBuffer_ = new byte[32];
            int bytesRead_;

           
            while (true)
            {
                try
                {
                    Console.WriteLine("1");
                    Thread.Sleep(1000);
                    Console.WriteLine("2");
                    _endpointReader.Read(readBuffer_, ReadWriteTimeout, out bytesRead_);
                    Console.WriteLine("3");

                    if (bytesRead_ <= 0)
                    {                        
                        continue;
                    }

                    string hex = BitConverter.ToString(readBuffer_);

                    if (bytesRead_ == 33)
                    {
                        //I guess callback/Confirmation from commands send to portal
                        if (readBuffer_[0] == 0x55)
                        {
                            if (_Debug)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(hex);
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            var ID = (int)readBuffer_[2];
                            var Length = (int)readBuffer_[1] + 2;
                            var MessageCommand = (MessageCommand)_CommandDictionary[ID];
                            _CommandDictionary.Remove(ID);

                            if (MessageCommand == MessageCommand.Read)
                            {
                                Console.WriteLine("READ Return");
                                byte[] bytes = new byte[16];
                                Array.Copy(readBuffer_, 4, bytes, 0, Length - 4);

                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                _FormatedResponse[ID] = bytes;
                                waitHandle.Set();

                            }
                            else
                            {
                                Console.WriteLine(MessageCommand.ToString() + "   -   " + hex);
                            }

                        }

                        //I quess Events from the portal
                        else if (readBuffer_[0] == 0x56)
                        {
                            if (_Debug)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(hex);
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            //I guess Tag/Chip Event
                            if (readBuffer_[1] == 0x0b)
                            {
                                if (readBuffer_[2] >= 4 || readBuffer_[5] >= 2) throw new NotImplementedException(hex);

                                var Checksum = readBuffer_[13];
                                var Pad = (Pad)readBuffer_[2];
                                var ID = (int)readBuffer_[4];
                                var Placed = (readBuffer_[5] == 1) ? false : true;

                                byte[] uuid = new byte[7];
                                Array.Copy(readBuffer_, 6, uuid, 0, uuid.Length);

                                PortalTagEvent?.Invoke(new PortalTagEventArgs() { Pad = Pad, ID = ID, Placed = Placed, UUID = uuid });
                            }
                        }

                        else
                        {
                            throw new NotImplementedException(hex);
                        }
                    }
                }
                catch
                {

                }
            }
        }
    }
}