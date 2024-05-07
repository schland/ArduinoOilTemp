using Plugin.BLE;
using Syncfusion.Maui.Gauges;
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
        ActivityIndicator.IsVisible = true;
        ActivityIndicator.IsRunning = true;

        try
        {
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
            }
            else
            {
                timer.Start();
            }
            ActivityIndicator.IsVisible = false;
            ActivityIndicator.IsRunning = false;
        } catch
        {
            ActivityIndicator.IsRunning = false;
            await DisplayAlert("Error", "Connection to Device failed!", "OK");
        }
        
    }

    private async void OnDisappearing(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"OnDisappearing");
            if (timer != null && timer.IsRunning)
            {
                timer.Stop();
            }

            ActivityIndicator.IsRunning = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Exception: " + ex.ToString() + "\nMessage: " + ex.Message + "\nBacktrace:" + ex.StackTrace.ToString(), "OK");
        }
    }

    void refreshTemperature()
    {
        try
        {
            Debug.WriteLine($"refreshTemperature()");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                (byte[] data, int resultcode) = await nano33ble_characteristic.ReadAsync();

                int oiltemp = BitConverter.ToInt16(data, 0);
                rangePointer.Value = oiltemp;
                annotationLabel.Text = String.Format("{0}°C", oiltemp);
                annotationLabel.TextColor = blue_to_green_to_red(oiltemp);
                //Debug.WriteLine($"refreshTemperature() {annotationLabel.Text}"); 

            });
        } catch { }
    }

    
    Color blue_to_green_to_red(int i)
    {
        byte red = 0; // Red is the top 5 bits of a 16 bit colour value
        byte green = 0;// Green is the middle 6 bits
        byte blue = 0; // Blue is the bottom 5 bits

        // map angle i to value
        int value = i;

        // blue until 10
        if (value < 10)
        {
            blue = 255;
            green = 0;
            red = 0;
        }

        if (value >= 10 && value <= 70)
        {
            float percentage = (value - 10) / 60.0f;
            if (percentage <= 0.5)
            {
                blue = 255;
                green = (byte)(Math.Round(255 * percentage * 2));
            }
            else
            {
                blue = (byte)(255 - (byte)(Math.Round(255 * (percentage - 0.5) * 2)));
                green = 255;
            }

            red = 0;
        }

        if (value > 70 && value < 90)
        {
            blue = 0;
            green = 255;
            red = 0;
        }

        if (value >= 90 && value <= 120)
        {
            float percentage = (value - 90) / 30.0f;
            blue = 0;
            if (percentage < 0.5)
            {
                green = 255;
                red = (byte)(Math.Round(255 * (percentage * 2)));
            }
            else
            {
                green = (byte)(255 - (byte)(Math.Round(255 * (percentage - 0.5) * 2)));
                red = 255;
            }

        }

        if (value > 120)
        {
            blue = 0;
            green = 0;
            red = 255;
        }

        return new Color(red ,green, blue);
    }
}