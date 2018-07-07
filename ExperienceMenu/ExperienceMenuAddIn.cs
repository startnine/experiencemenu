using ExperienceMenu.Views;
using System;
using System.AddIn;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace ExperienceMenu
{
    [AddIn("Patrick Start", Description = "Start menu whose name is Patrick", Version = "1.0.0.0", Publisher = "Start9")]
    public class ExperienceMenuAddIn : IModule
    {
        public static ExperienceMenuAddIn Instance { get; private set; }

        public IConfiguration Configuration { get; set; } = new ExperienceMenuConfiguration();

        public IMessageContract MessageContract => null;

        public IReceiverContract ReceiverContract { get; } = new ExperienceMenuReceiverContract();

        public IHost Host { get; private set; }

        public void Initialize(IHost host)
        {
            void Start()
            {
                Instance = this;
                ((ExperienceMenuReceiverContract) ReceiverContract).StartMenuOpenedEntry.MessageReceived += (sender, e) =>
                {
                    MainWindow.Instance.Show();
                    MainWindow.Instance.Focus();
                    MainWindow.Instance.Activate();
                    //MessageBox.Show("WHAT THE DABBERS");
                };
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => MessageBox.Show(e.ExceptionObject.ToString(), "Uh Oh Exception!");

                Application.ResourceAssembly = Assembly.GetExecutingAssembly();
                App.Main();
            }

            var t = new Thread(Start);
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

    }

    public class ExperienceMenuReceiverContract : IReceiverContract
    {
        public IList<IReceiverEntry> Entries => new[] { StartMenuOpenedEntry };
        public IReceiverEntry StartMenuOpenedEntry { get; } = new ReceiverEntry("Open menu");
    }


    public class ExperienceMenuConfiguration : IConfiguration
    {
        public IList<IConfigurationEntry> Entries => new[] { new ConfigurationEntry(PinnedItems, "Pinned Items"), new ConfigurationEntry(Places, "Places") };

        public IList<String> PinnedItems { get; } = new List<String>();
        public IList<String> Places { get; } = new List<String>();
    }
}