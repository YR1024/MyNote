using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WpfApp2;

namespace SkipDrama_YuanShen
{
    internal sealed class DirectHidInputListener : IDisposable
    {
        private const ushort MicrosoftVendorId = 0x045E;
        private const ushort UsagePageGenericDesktop = 0x01;
        private const ushort UsageJoystick = 0x04;
        private const ushort UsageGamepad = 0x05;

        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly List<SafeFileHandle> _handles = new List<SafeFileHandle>();
        private readonly List<Task> _readers = new List<Task>();
        private bool _started;
        private bool _disposed;

        internal event EventHandler<HidReportEventArgs> HidReport;
        internal event Action<string> DeviceOpened;
        internal event Action<Exception> Error;

        internal void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            foreach (var device in EnumerateHidDevices())
            {
                TryOpenDevice(device);
            }
        }

        private void TryOpenDevice(HidDeviceInfo device)
        {
            var handle = CreateFile(
                device.Path,
                GenericRead,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero);

            if (handle == null || handle.IsInvalid)
            {
                handle?.Dispose();
                return;
            }

            _handles.Add(handle);
            DeviceOpened?.Invoke($"Direct HID: VID {device.VendorId:X4}, PID {device.ProductId:X4}, report {device.InputReportByteLength} bytes");
            _readers.Add(Task.Run(() => ReadLoop(handle, device.InputReportByteLength, _stop.Token)));
        }

        private void ReadLoop(SafeFileHandle handle, ushort inputReportByteLength, CancellationToken token)
        {
            try
            {
                var reportLength = Math.Max(1, (int)inputReportByteLength);
                var buffer = new byte[reportLength];
                while (!token.IsCancellationRequested)
                {
                    int read;
                    if (!ReadFile(handle, buffer, buffer.Length, out read, IntPtr.Zero))
                    {
                        if (!token.IsCancellationRequested)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "ReadFile failed.");
                        }

                        return;
                    }

                    if (read <= 0)
                    {
                        continue;
                    }

                    var report = new byte[read];
                    Buffer.BlockCopy(buffer, 0, report, 0, read);
                    HidReport?.Invoke(this, new HidReportEventArgs(report));
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Error?.Invoke(ex);
                }
            }
        }

        private static IEnumerable<HidDeviceInfo> EnumerateHidDevices()
        {
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);

            var deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
            if (deviceInfoSet == InvalidHandleValue)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SetupDiGetClassDevs failed.");
            }

            try
            {
                var interfaceData = new SP_DEVICE_INTERFACE_DATA();
                interfaceData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

                for (uint index = 0; SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData); index++)
                {
                    string path;
                    if (!TryGetDevicePath(deviceInfoSet, interfaceData, out path))
                    {
                        continue;
                    }

                    var info = TryInspectDevice(path);
                    if (info != null)
                    {
                        yield return info;
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        private static bool TryGetDevicePath(IntPtr deviceInfoSet, SP_DEVICE_INTERFACE_DATA interfaceData, out string path)
        {
            path = null;
            uint requiredSize = 0;
            SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, ref requiredSize, IntPtr.Zero);
            if (requiredSize == 0)
            {
                return false;
            }

            var detailData = Marshal.AllocHGlobal((int)requiredSize);
            try
            {
                Marshal.WriteInt32(detailData, IntPtr.Size == 8 ? 8 : 6);
                if (!SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, detailData, requiredSize, ref requiredSize, IntPtr.Zero))
                {
                    return false;
                }

                path = Marshal.PtrToStringAuto(detailData + 4);
                return !string.IsNullOrEmpty(path);
            }
            finally
            {
                Marshal.FreeHGlobal(detailData);
            }
        }

        private static HidDeviceInfo TryInspectDevice(string path)
        {
            using (var handle = CreateFile(
                path,
                GenericRead,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero))
            {
                if (handle == null || handle.IsInvalid)
                {
                    return null;
                }

                HIDD_ATTRIBUTES attributes;
                attributes.Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES));
                attributes.VendorId = 0;
                attributes.ProductId = 0;
                attributes.VersionNumber = 0;
                if (!HidD_GetAttributes(handle, ref attributes) || attributes.VendorId != MicrosoftVendorId)
                {
                    return null;
                }

                IntPtr preparsedData;
                if (!HidD_GetPreparsedData(handle, out preparsedData))
                {
                    return null;
                }

                try
                {
                    HIDP_CAPS caps;
                    if (HidP_GetCaps(preparsedData, out caps) != 0)
                    {
                        return null;
                    }

                    if (caps.UsagePage != UsagePageGenericDesktop ||
                        (caps.Usage != UsageGamepad && caps.Usage != UsageJoystick) ||
                        caps.InputReportByteLength == 0)
                    {
                        return null;
                    }

                    return new HidDeviceInfo(path, attributes.VendorId, attributes.ProductId, caps.InputReportByteLength);
                }
                finally
                {
                    HidD_FreePreparsedData(preparsedData);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stop.Cancel();
            foreach (var handle in _handles)
            {
                handle.Dispose();
            }

            _stop.Dispose();
        }

        private sealed class HidDeviceInfo
        {
            internal HidDeviceInfo(string path, ushort vendorId, ushort productId, ushort inputReportByteLength)
            {
                Path = path;
                VendorId = vendorId;
                ProductId = productId;
                InputReportByteLength = inputReportByteLength;
            }

            internal string Path { get; }
            internal ushort VendorId { get; }
            internal ushort ProductId { get; }
            internal ushort InputReportByteLength { get; }
        }

        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        private const uint GenericRead = 0x80000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint OpenExisting = 3;
        private const uint DigcfPresent = 0x00000002;
        private const uint DigcfDeviceInterface = 0x00000010;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            internal int cbSize;
            internal Guid InterfaceClassGuid;
            internal int Flags;
            internal IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            internal int Size;
            internal ushort VendorId;
            internal ushort ProductId;
            internal ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            internal ushort Usage;
            internal ushort UsagePage;
            internal ushort InputReportByteLength;
            internal ushort OutputReportByteLength;
            internal ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            internal ushort[] Reserved;
            internal ushort NumberLinkCollectionNodes;
            internal ushort NumberInputButtonCaps;
            internal ushort NumberInputValueCaps;
            internal ushort NumberInputDataIndices;
            internal ushort NumberOutputButtonCaps;
            internal ushort NumberOutputValueCaps;
            internal ushort NumberOutputDataIndices;
            internal ushort NumberFeatureButtonCaps;
            internal ushort NumberFeatureValueCaps;
            internal ushort NumberFeatureDataIndices;
        }

        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, out IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            string enumerator,
            IntPtr hwndParent,
            uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet,
            IntPtr deviceInfoData,
            ref Guid interfaceClassGuid,
            uint memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            uint deviceInterfaceDetailDataSize,
            ref uint requiredSize,
            IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadFile(
            SafeFileHandle file,
            [Out] byte[] buffer,
            int numberOfBytesToRead,
            out int numberOfBytesRead,
            IntPtr overlapped);
    }
}
