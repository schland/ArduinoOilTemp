namespace OilTempJan
{
    public partial class App : Application
    {
        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NBaF5cXmZCe0x3Rnxbf1x0ZF1MYF1bRnVPMyBoS35RckVmWXleeHRURGNVVENz");

            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
            
            //MainPage = new AppShell();
        }
    }
}
