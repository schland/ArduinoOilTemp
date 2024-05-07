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

        private async void OnContentLoaded(object sender, EventArgs e)
        {
            try
            {
                if (Preferences.Default.Get("bluetooth_id", "null") != "null")
                {
                    await Navigation.PushAsync(new OilTemp(), true);
                }
            } catch (Exception ex)
            {
                await DisplayAlert("Error", "Exception: " + ex.ToString() + "\nMessage: " + ex.Message + "\nBacktrace:" + ex.StackTrace.ToString(), "OK");
            }
        }
    }

}
