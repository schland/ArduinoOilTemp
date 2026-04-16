using Microsoft.Extensions.DependencyInjection;

namespace OilTempJan2
{
    public partial class App : Application
    {
        public App()
        {
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NBaF5cXmZCe0x3Rnxbf1x0ZF1MYF1bRnVPMyBoS35RckVmWXleeHRURGNVVENz");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzI2OTc4MUAzMjM1MmUzMDJlMzBhekFNZklINDJtRk1tOUoxK0NDYVMvcnRzT0JuYmRwTHR3eFJ2K2J1Q0VrPQ==");
            
            InitializeComponent();
            // Window will be created in CreateWindow override for MAUI apps
            //MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

    }
}
