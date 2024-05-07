using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE;
using System.Diagnostics;
using System.Text;

namespace OilTempJan;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void ScanDevicesClicked(object sender, EventArgs e)
    {
        await MainPage.CheckAndRequestBluetoothPermission();
        await MainPage.CheckAndRequestLocationWhenInUsePermission();

        ActivityIndicator.IsRunning = true;

        var ble = CrossBluetoothLE.Current;
        var adapter = CrossBluetoothLE.Current.Adapter;

        var state = ble.State;
        Debug.WriteLine($"The bluetooth state is {state}");

        List<Plugin.BLE.Abstractions.Contracts.IDevice> deviceList = new List<Plugin.BLE.Abstractions.Contracts.IDevice>();

        adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);
        await adapter.StartScanningForDevicesAsync();

        Debug.WriteLine($"Found {deviceList.Count} devices.");

        List<BluetoothDevice> devices = new List<BluetoothDevice>();


        foreach (var device in deviceList)
        {
            BluetoothDevice device2 = new BluetoothDevice();
            device2.Name = device.Name;
            device2.Id = device.Id;
            devices.Add(device2);
            Debug.WriteLine($"\t{device.Name} {device.Id} {device.State}");
        }

        BluetoothDevicesListView.ItemsSource = devices.DistinctBy(x => x.Id);

        ActivityIndicator.IsRunning = false;
    }

    private async void ConnectBtnClicked(object sender, EventArgs e)
    {
        BluetoothDevice device = (BluetoothDevice)BluetoothDevicesListView.SelectedItem;

        if( device == null )
        {
            await DisplayAlert("Info", "Please select an Item from List!", "OK");

            return;
        }

        Debug.WriteLine($"Selected Item: {device.Name}");

        try
        {
            var adapter = CrossBluetoothLE.Current.Adapter;
            var nano33ble = await adapter.ConnectToKnownDeviceAsync(device.Id);
            var service = await nano33ble.GetServiceAsync(Guid.Parse("0000180c-0000-1000-8000-00805f9b34fb"));

            var characteristic = await service.GetCharacteristicAsync(Guid.Parse("00002a56-0000-1000-8000-00805f9b34fb"));
            (byte[] data, int resultcode) = await characteristic.ReadAsync();

            if( resultcode == 0)
            {
                Preferences.Default.Set("bluetooth_id", device.Id.ToString());
                await adapter.DisconnectDeviceAsync(nano33ble);

                await DisplayAlert("Info", "Connection to Device successful!", "OK");
                await Navigation.PopAsync();
            } else
            {
                await adapter.DisconnectDeviceAsync(nano33ble);
            }
        }
        catch
        {
            await DisplayAlert("Error", "Connection to Device failed!", "OK");
            return;
        }

        

    }
}