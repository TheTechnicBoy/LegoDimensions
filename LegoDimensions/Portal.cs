﻿using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;

namespace LegoDimensions
{
    public class Portal
    {

        [DllImport("libusb-1.0", EntryPoint = "libusb_set_auto_detach_kernel_driver")]
        public static extern Error SetAutoDetachKernelDriver(DeviceHandle devHandle, int enable);

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
        public Dictionary<int, byte[]> presentTags;
        private bool _nfcEnabled = true;
        public bool isAlive = false;
        public bool nfcEnabled
        {
            get => _nfcEnabled;
            set
            {
                if (value == _nfcEnabled) return;
                bool timeout;
                setNFCEnabled(out timeout, value);
                if (timeout)
                {
                    ConsoleColor before = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Timeout while setting NFCEnabled");
                    Console.ForegroundColor = before;
                }
                if (!timeout) _nfcEnabled = value;
            }
        }

        //Event
        public Action<PortalTagEventArgs> PortalTagEvent = (args) => { };

        #region Start/Stop
        public Portal(bool StartStoppAnimations = true, IUsbDevice? usbDevice = null)
        {
            _CommandDictionary = new Dictionary<int, MessageCommand>();
            _FormatedResponse = new Dictionary<int, object>();
            presentTags = new Dictionary<int, byte[]>();
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


            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetAutoDetachKernelDriver(_portal.DeviceHandle, 1);
            }

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
            if (!WakeUp()) throw new Exception("Could Not Wake up Portal");

            isAlive = true;

            //Animation
            if (_startStoppAnimations)
            {
                Task.Run(() =>
                {
                    SetFade(Pad.Center, new FadeProperties(Color.Red, 60, 1));
                    Thread.Sleep(1000);
                    SetFade(Pad.Left, new FadeProperties(Color.Green, 60, 1));
                    Thread.Sleep(1000);
                    SetFade(Pad.Right, new FadeProperties(Color.Blue, 60, 1));
                });
            }
        }
        public void Close()
        {
            isAlive = false;

            _cancelThread.Cancel();

            if (_startStoppAnimations)
            {
                SetFade(Pad.All, new FadeProperties(Color.Off, 20, 1));
            }

            Thread.Sleep(1000);

            _portal.ReleaseInterface(_portal.Configs[0].Interfaces[0].Number);
            _portal.Close();
            _portal.Dispose();
        }
        #endregion


        #region Ulitiliy
        internal bool WakeUp()
        {
            var waitHandle = new ManualResetEvent(false);
            bool result = false;

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

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                result = true;
            }

            return result;
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
        public void SetColors(out bool timeout, Color? center = null, Color? left = null, Color? right = null)
        {
            var waitHandle = new ManualResetEvent(false);

            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x0e; //command length
            byte_[2] = (byte)MessageCommand.ColorAll; //command
            byte_[3] = MessageID_; //Message ID (i think)

            if (center != null)
            {
                byte_[4] = 1; //platform enabled
                byte_[5] = center.red; // r
                byte_[6] = center.green; // g
                byte_[7] = center.blue; // b
            }

            if (left != null)
            {
                byte_[8] = 1; //platform enabled
                byte_[9] = left.red; // r
                byte_[10] = left.blue; // g
                byte_[11] = left.blue; // b
            }

            if (right != null)
            {
                byte_[12] = 1; //platform enabled
                byte_[13] = right.red; // r
                byte_[14] = right.green; // g
                byte_[15] = right.blue; // b
            }

            byte_[16] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        public void SetColors(Color? center = null, Color? left = null, Color? right = null)
        {
            SetColors(out _, center, left, right);
        }

        public void SetColor(out bool timeout, Pad pad, Color color)
        {
            var waitHandle = new ManualResetEvent(false);

            if (color == null) throw new ArgumentNullException(nameof(color));

            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x06; //command length
            byte_[2] = (byte)MessageCommand.Color; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)pad; //platform
            byte_[5] = color.red; // r
            byte_[6] = color.green; // g
            byte_[7] = color.blue; // b

            byte_[8] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        public void SetColor(Pad pad, Color color)
        {
            SetColor(out _, pad, color);
        }

        public void SetFlashs(out bool timeout, FlashProperties? center = null, FlashProperties? left = null, FlashProperties? right = null)
        {
            var waitHandle = new ManualResetEvent(false);

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
                byte_[8] = center.Color.red;
                byte_[9] = center.Color.green;
                byte_[10] = center.Color.blue;
            }

            if (left != null)
            {
                byte_[11] = 1;
                byte_[12] = left.OnLen;
                byte_[13] = left.OffLen;
                byte_[14] = left.PulseCnt;
                byte_[15] = left.Color.red;
                byte_[16] = left.Color.green;
                byte_[17] = left.Color.blue;
            }

            if (right != null)
            {
                byte_[18] = 1;
                byte_[19] = right.OnLen;
                byte_[20] = right.OffLen;
                byte_[21] = right.PulseCnt;
                byte_[22] = right.Color.red;
                byte_[23] = right.Color.green;
                byte_[24] = right.Color.blue;
            }

            byte_[25] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        public void SetFlashs(FlashProperties? center = null, FlashProperties? left = null, FlashProperties? right = null)
        {
            SetFlashs(out _, center, left, right);
        }

        public void SetFlash(out bool timeout, Pad pad, FlashProperties flashProperties)
        {
            var waitHandle = new ManualResetEvent(false);

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
            byte_[8] = flashProperties.Color.red; // r
            byte_[9] = flashProperties.Color.green; // g
            byte_[10] = flashProperties.Color.blue; // b

            byte_[11] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        public void SetFlash(Pad pad, FlashProperties flashProperties)
        {
            SetFlash(out _, pad, flashProperties);
        }


        public void SetFades(out bool timeout, FadeProperties? center = null, FadeProperties? left = null, FadeProperties? right = null)
        {
            var waitHandle = new ManualResetEvent(false);

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
                byte_[7] = center.Color.red;
                byte_[8] = center.Color.green;
                byte_[9] = center.Color.blue;
            }

            if (left != null)
            {
                byte_[10] = 1;
                byte_[11] = left.FadeLen;
                byte_[12] = left.PulseCnt;
                byte_[13] = left.Color.red;
                byte_[14] = left.Color.green;
                byte_[15] = left.Color.blue;
            }

            if (right != null)
            {
                byte_[16] = 1;
                byte_[17] = right.FadeLen;
                byte_[18] = right.PulseCnt;
                byte_[19] = right.Color.red;
                byte_[20] = right.Color.green;
                byte_[21] = right.Color.blue;
            }

            byte_[22] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }

        public void SetFades(FadeProperties? center = null, FadeProperties? left = null, FadeProperties? right = null)
        {
            SetFades(out _, center, left, right);
        }

        public void SetFade(out bool timeout, Pad pad, FadeProperties fadeProperties)
        {
            var waitHandle = new ManualResetEvent(false);

            var MessageID_ = _messageID++;
            byte[] byte_ = new byte[32];

            byte_[0] = 0x55; //start
            byte_[1] = 0x08; //command length
            byte_[2] = (byte)MessageCommand.Fade; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)pad; //platform
            byte_[5] = fadeProperties.FadeLen; //light on length
            byte_[6] = fadeProperties.PulseCnt; //number of pulses
            byte_[7] = fadeProperties.Color.red; // r
            byte_[8] = fadeProperties.Color.green; // g
            byte_[9] = fadeProperties.Color.blue; // b

            byte_[10] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        public void SetFade(Pad pad, FadeProperties fadeProperties)
        {
            SetFade(out _, pad, fadeProperties);
        }

        public void FadeRandom(out bool timeout, Pad pad, RandomFadeProperties randomFadeProperties)
        {
            var waitHandle = new ManualResetEvent(false);

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

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        public void FadeRandom(Pad pad, RandomFadeProperties randomFadeProperties)
        {
            FadeRandom(out _, pad, randomFadeProperties);
        }
        #endregion

        #region Tag

        public byte[] ReadTag(out bool timeout, byte index, byte page)
        {
            var result = _ReadTag(out timeout, index, page);
            byte[] resultFormat = new byte[4];
            Array.Copy(result, 0, resultFormat, 0, 4);
            result = resultFormat;
            return result;
        }
        private byte[] _ReadTag(out bool timeout, byte index, byte page)
        {
            if (!presentTags.ContainsKey(index)) throw new Exception("Tag not present on the portal.");
            var waitHandle = new ManualResetEvent(false);
            byte[] result = { 0x00 };

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
                timeout = false;
            }
            else
            {
                timeout = true;
            }

            return result;
        }
        public byte[] ReadTag(byte index, byte page)
        {
            return ReadTag(out _, index, page);
        }

        public List<byte[]> DumpTag(out bool timeout, byte index)
        {
            if (!presentTags.ContainsKey(index)) throw new Exception("Tag not present on the portal.");
            List<byte[]> result = new List<byte[]>();
            timeout = false;

            for (byte i = 0; i < 0x2c; i += 4)
            {
                bool _timeout;
                var tag = _ReadTag(out _timeout, index, i);
                if (_timeout)
                {
                    timeout = true;
                    break;
                }

                if (tag == null || tag.Length == 0)
                {
                    Console.WriteLine($"Error reading card page 0x{i:X2}");
                    throw new Exception("Error reading card page 0x" + i.ToString("X2"));
                }
                else result.Add(tag);
            }
            return result;
        }
        public List<byte[]> DumpTag(byte index)
        {
            return DumpTag(out _, index);
        }

        public bool WriteTag(out bool timeout, byte index, byte page, byte[] bytes)
        {
            if (!presentTags.ContainsKey(index)) throw new Exception("Tag not present on the portal.");
            if (bytes.Length != 4)
            {
                throw new ArgumentException("Write to card must be 4 bytes.");
            }

            var waitHandle = new ManualResetEvent(false);
            bool result;

            byte[] byte_ = new byte[32];
            var MessageID_ = _messageID++;

            byte_[0] = 0x55; //start
            byte_[1] = 0x08; //command length
            byte_[2] = (byte)MessageCommand.Write; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = index;
            byte_[5] = page;
            byte_[6] = bytes[0];
            byte_[7] = bytes[1];
            byte_[8] = bytes[2];
            byte_[9] = bytes[3];

            byte_[10] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                result = (bool)_FormatedResponse[MessageID_];
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
                result = false;
            }

            return result;

        }
        public bool WriteTag(byte index, byte page, byte[] bytes)
        {
            return WriteTag(out _, index, page, bytes);
        }

        public void SetTagPassword(out bool timeout, PortalPassword password, byte index, byte[]? newPassword = null)
        {
            if (password == PortalPassword.Custom)
            {
                if (newPassword != null && newPassword.Length != 4)
                {
                    throw new ArgumentException("New password must be 4 bytes");
                }
            }

            if (newPassword == null) newPassword = new byte[4];

            var waitHandle = new ManualResetEvent(false);

            byte[] byte_ = new byte[32];
            var MessageID_ = _messageID++;

            byte_[0] = 0x55; //start
            byte_[1] = 0x08; //command length
            byte_[2] = (byte)MessageCommand.ConfigPassword; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)password;
            byte_[5] = index;
            byte_[6] = newPassword[0];
            byte_[7] = newPassword[1];
            byte_[8] = newPassword[2];
            byte_[9] = newPassword[3];

            byte_[10] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        public void SetTagPassword(PortalPassword password, byte index, byte[]? newPassword = null)
        {
            SetTagPassword(out _, password, index, newPassword);
        }

        private void setNFCEnabled(out bool timeout, bool enabled)
        {
            //Only a Payload from 1 -> true/false

            var waitHandle = new ManualResetEvent(false);

            byte[] byte_ = new byte[32];
            var MessageID_ = _messageID++;

            byte_[0] = 0x55; //start
            byte_[1] = 0x03; //command length
            byte_[2] = (byte)MessageCommand.ConfigActive; //command
            byte_[3] = MessageID_; //Message ID (i think)

            byte_[4] = (byte)(enabled ? 1 : 0);

            byte_[5] = ComputeAdditionChecksum(byte_);

            _FormatedResponse[MessageID_] = waitHandle;

            SendMessage(byte_);

            if (waitHandle.WaitOne(ReceiveTimeout, false))
            {
                _FormatedResponse.Remove(MessageID_);
                timeout = false;
            }
            else
            {
                timeout = true;
            }
        }
        #endregion

        #region ReadTag
        private void ReadThread(object? obj)
        {
            var readBuffer_ = new byte[32];
            int bytesRead_;


            while (!_cancelThread.IsCancellationRequested)
            {
                try
                {
                    _endpointReader.Read(readBuffer_, ReadWriteTimeout, out bytesRead_);

                    if (bytesRead_ <= 0)
                    {
                        continue;
                    }

                    string hex = BitConverter.ToString(readBuffer_);

                    if (_Debug)
                    {
                        ConsoleColor before = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Bytes Read: " + bytesRead_);
                        Console.WriteLine(hex);
                        Console.WriteLine("\n");
                        Console.ForegroundColor = before;
                    }

                    //Win has 33
                    //Linux has 32
                    //Why?
                    //I don't know
                    if (bytesRead_ == 33 || bytesRead_ == 32)
                    {
                        //I guess callback/Confirmation from commands send to portal
                        if (readBuffer_[0] == 0x55)
                        {
                            if (_Debug)
                            {
                                ConsoleColor before = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[Commands] " + hex);
                                Console.ForegroundColor = before;
                            }


                            var Length = (int)readBuffer_[1];

                            byte[] payload = new byte[Length];
                            Array.Copy(readBuffer_, 2, payload, 0, Length);

                            var ID = (int)payload[0];
                            var MessageCommand = (MessageCommand)_CommandDictionary[ID];
                            _CommandDictionary.Remove(ID);

                            //Payload 0 is the CommandID send by incrementing _messageID
                            //Payload 1 is the status of the command
                            //Payload 2 and up is the data

                            if (MessageCommand == MessageCommand.Read)
                            {
                                byte[] bytes = payload[2..];

                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                _FormatedResponse[ID] = bytes;
                                waitHandle.Set();

                            }
                            else if (MessageCommand == MessageCommand.Write)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                bool success = payload[1] == 0;
                                _FormatedResponse[ID] = success;
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.Wake)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.ConfigPassword)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.Fade)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.FadeAll)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.Flash)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.FlashAll)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.Color)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.ColorAll)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.FadeRandom)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else if (MessageCommand == MessageCommand.ConfigActive)
                            {
                                var waitHandle = (ManualResetEvent)_FormatedResponse[ID];
                                waitHandle.Set();
                            }
                            else
                            {
                                ConsoleColor before = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                Console.WriteLine("WIP Commands: " + MessageCommand.ToString() + "   -   " + hex);
                                Console.WriteLine("Payload: " + BitConverter.ToString(payload));
                                Console.ForegroundColor = before;
                            }

                        }

                        //I quess Events from the portal
                        else if (readBuffer_[0] == 0x56)
                        {
                            if (_Debug)
                            {
                                ConsoleColor before = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine("[Events] " + hex);
                                Console.ForegroundColor = before;
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

                                Task.Run(() =>
                                {
                                    if (Placed) presentTags[ID] = uuid;
                                    else presentTags.Remove(ID);
                                    PortalTagEvent?.Invoke(new PortalTagEventArgs() { Pad = Pad, ID = ID, Placed = Placed, UUID = uuid });
                                });
                            }
                        }

                        else
                        {
                            ConsoleColor before = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.WriteLine("WIP Events: " + hex);
                            Console.ForegroundColor = before;
                        }
                    }
                }
                catch
                {
                    ConsoleColor before = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine("An Error Occured in the Read Thread.");
                    Console.ForegroundColor = before;
                }
            }
        }
        #endregion
    }
}