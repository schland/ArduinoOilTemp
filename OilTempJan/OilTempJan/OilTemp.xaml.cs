using Plugin.BLE;
using System.Diagnostics;

namespace OilTempJan;

public partial class OilTemp : ContentPage
{
    private Plugin.BLE.Abstractions.Contracts.IAdapter adapter_global = CrossBluetoothLE.Current.Adapter;
    private Plugin.BLE.Abstractions.Contracts.IDevice nano33ble_global = null;
    private Plugin.BLE.Abstractions.Contracts.IService nano33ble_service = null;
    private Plugin.BLE.Abstractions.Contracts.ICharacteristic nano33ble_characteristic = null;
    private IDispatcherTimer timer;

    public OilTemp()
	{
		InitializeComponent();
	}

    private async void OnContentLoaded(object sender, EventArgs e)
	{
        ActivityIndicator.IsRunning = true;

        Debug.WriteLine($"OnContentLoaded");

        if (nano33ble_global == null)
        {
            nano33ble_global = await adapter_global.ConnectToKnownDeviceAsync(Guid.Parse(Preferences.Default.Get("bluetooth_id", "null")));
        }

        if (nano33ble_service == null)
        {
            nano33ble_service = await nano33ble_global.GetServiceAsync(Guid.Parse("0000180c-0000-1000-8000-00805f9b34fb"));
        }

        if (nano33ble_characteristic == null)
        {
            nano33ble_characteristic = await nano33ble_service.GetCharacteristicAsync(Guid.Parse("00002a56-0000-1000-8000-00805f9b34fb"));
        }

        //(byte[] data, int resultcode) = await nano33ble_characteristic.ReadAsync();

        //int oiltemp = BitConverter.ToInt16(data, 0);

        //OiltempLabel.Text = oiltemp.ToString();

        if (timer is null)
        {
            timer = Application.Current.Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => refreshTemperature();
            timer.Start();
        } else
        {
            timer.Start();
        }

        ActivityIndicator.IsRunning = false;
    }

    private async void OnDisappearing(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnDisappearing");
        timer.Stop();
        ActivityIndicator.IsRunning = false;
    }

    async void refreshTemperature()
    {
        Debug.WriteLine($"refreshTemperature()");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            (byte[] data, int resultcode) = await nano33ble_characteristic.ReadAsync();

            int oiltemp = BitConverter.ToInt16(data, 0);
            rangePointer.Value = oiltemp;
            temperatureLabel.Text = String.Format("°C",oiltemp );
            OiltempLabel.Text = oiltemp.ToString();
        });
    }
}