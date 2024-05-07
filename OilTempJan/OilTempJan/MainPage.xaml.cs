using Microsoft.Maui.Platform;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Exceptions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace OilTempJan
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

        }

        public static async Task<PermissionStatus> CheckAndRequestBluetoothPermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.Bluetooth>())
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<Permissions.Bluetooth>();

            return status;
        }

        public static async Task<PermissionStatus> CheckAndRequestLocationWhenInUsePermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            return status;
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            await CheckAndRequestBluetoothPermission();
            await CheckAndRequestLocationWhenInUsePermission();

            Label.Text = "";

            var ble = CrossBluetoothLE.Current;
            var adapter = CrossBluetoothLE.Current.Adapter;

            var state = ble.State;
            Debug.WriteLine($"The bluetooth state is {state}");

            List<Plugin.BLE.Abstractions.Contracts.IDevice> deviceList = new List<Plugin.BLE.Abstractions.Contracts.IDevice>();

            adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);
            await adapter.StartScanningForDevicesAsync();

            Debug.WriteLine($"Found {deviceList.Count} devices.");

            foreach (var device in deviceList)
            {
                Debug.WriteLine($"\t{device.Name} {device.Id} {device.State}");
            }

            //var nano33ble = deviceList.First(device => device.Name == "Nano33BLE");

            //try
            //{
            //    await adapter.ConnectToDeviceAsync(nano33ble);
            //}
            //catch (DeviceConnectionException ex)
            //{
            //    throw ex;
            //    // ... could not connect to device
            //}


            //var services = await nano33ble.GetServicesAsync();

            //try
            //{
            //    await adapter.DisconnectDeviceAsync(nano33ble);
            //}
            //catch (DeviceConnectionException ex)
            //{
            //    throw ex;
            //    // ... could not connect to device
            //}


            // 00000000-0000-0000-0000-30a7c44c2462
            try
            {
                var nano33ble = await adapter.ConnectToKnownDeviceAsync(Guid.Parse("00000000-0000-0000-0000-ecda3b60165d"));
                var service = await nano33ble.GetServiceAsync(Guid.Parse("0000180c-0000-1000-8000-00805f9b34fb"));

                //var characteristics = await service.GetCharacteristicsAsync();
                // 		Uuid	"00002a56-0000-1000-8000-00805f9b34fb"	string
                var characteristic = await service.GetCharacteristicAsync(Guid.Parse("00002a56-0000-1000-8000-00805f9b34fb"));
                (byte[] data, int resultcode) = await characteristic.ReadAsync();

                Debug.WriteLine($"resultcode {resultcode}");
                Debug.WriteLine($"data {Encoding.UTF8.GetString(data)}");

                Label.Text = Encoding.UTF8.GetString(data);

                await adapter.DisconnectDeviceAsync(nano33ble);
            }
            catch (DeviceConnectionException ex)
            {
                throw ex;
                // ... could not connect to device
            }



            // 0000180c-0000-1000-8000-00805f9b34fb



        }

        private Plugin.BLE.Abstractions.Contracts.IAdapter adapter_global = CrossBluetoothLE.Current.Adapter;
        private Plugin.BLE.Abstractions.Contracts.IDevice nano33ble_global = null;
        private Plugin.BLE.Abstractions.Contracts.IService nano33ble_service = null;
        private Plugin.BLE.Abstractions.Contracts.ICharacteristic nano33ble_characteristic = null;

        private async void OnGetValueFast(object sender, EventArgs e)
        {
            await CheckAndRequestBluetoothPermission();
            await CheckAndRequestLocationWhenInUsePermission();

            if (nano33ble_global == null )
            {
                nano33ble_global = await adapter_global.ConnectToKnownDeviceAsync(Guid.Parse("00000000-0000-0000-0000-ecda3b60165d"));
            }

            if (nano33ble_service == null)
            {
                nano33ble_service = await nano33ble_global.GetServiceAsync(Guid.Parse("0000180c-0000-1000-8000-00805f9b34fb"));
            }

            if (nano33ble_characteristic == null)
            {
                nano33ble_characteristic = await nano33ble_service.GetCharacteristicAsync(Guid.Parse("00002a56-0000-1000-8000-00805f9b34fb"));
            }

            (byte[] data, int resultcode) = await nano33ble_characteristic.ReadAsync();

            int oiltemp = BitConverter.ToInt16(data, 0);
            
            Debug.WriteLine($"resultcode {resultcode}");
            Debug.WriteLine($"data {oiltemp}");

            Label.Text = oiltemp.ToString();
        }

        private async void ShowOiltempBtnClicked(object sender, EventArgs e)
        {
            await CheckAndRequestBluetoothPermission();
            await CheckAndRequestLocationWhenInUsePermission();

            await Navigation.PushAsync(new OilTemp(), true);
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            Debug.WriteLine($"OnSettingsClicked");
            await Navigation.PushAsync(new SettingsPage(), true);
        }
    }

}
