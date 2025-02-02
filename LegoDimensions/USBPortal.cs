using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Runtime.InteropServices;
using System.Transactions;

namespace LegoDimensions
{
    public class USBPortal : IPortal
    {

        [DllImport("libusb-1.0", EntryPoint = "libusb_set_auto_detach_kernel_driver")]
        public static extern Error SetAutoDetachKernelDriver(DeviceHandle devHandle, int enable);

        // Class variables
        internal IUsbDevice _portal;
        internal UsbEndpointReader _endpointReader;
        internal UsbEndpointWriter _endpointWriter;
        internal IUsbDevice? usbDevice;

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

        public USBPortal(bool StartStoppAnimations = true, IUsbDevice ? usbDevice = null) : base( StartStoppAnimations)
        {
            
            this.usbDevice = usbDevice;
        }

        public override bool SetupConnection(int ProductId, int VendorId, int ReadWriteTimeout)
        {
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

            return true;
        }

        public override bool CloseConnection()
        {
            _portal.ReleaseInterface(_portal.Configs[0].Interfaces[0].Number);
            _portal.Close();
            _portal.Dispose();
            return true;
        }

        public override void Input(byte[] buffer, int timeout, out int bytesRead)
        {
            _endpointReader.Read(buffer, ReadWriteTimeout, out bytesRead);
        }

        public override void Output(byte[] buffer, int timeout, out int transferLength)
        {
            _endpointWriter.Write(buffer, timeout, out transferLength);
        }
    }
}