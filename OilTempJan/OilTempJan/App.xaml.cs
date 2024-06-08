namespace OilTempJan
{
    public partial class App : Application
    {
        public App()
        {
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NBaF5cXmZCe0x3Rnxbf1x0ZF1MYF1bRnVPMyBoS35RckVmWXleeHRURGNVVENz");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzI2OTc4MUAzMjM1MmUzMDJlMzBhekFNZklINDJtRk1tOUoxK0NDYVMvcnRzT0JuYmRwTHR3eFJ2K2J1Q0VrPQ==");
            
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
            
            //MainPage = new AppShell();
        }
    }
}
