using Spotify2.MouseMovementLibraries.RazerSupport;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;
using Visuality;

namespace Spotify2.Other
{
    internal class RequirementsManager
    {
        public static void MainWindowInit()
        {
            if (Directory.GetCurrentDirectory().Contains("Temp"))
            {
                MessageBox.Show(
                    "Hi, it is made aware that you are running Spotify without extracting it from the zip file." +
                    " Please extract Spotify from the zip file or Spotify will not be able to run properly." +
                    "\n\nThank you.",
                    "Spotify V2"
                    );
            }

            CheckForRequirements();
        }

        public static bool CheckForRequirements()
        {
            if (!IsVCRedistInstalled())
            {
                MessageBox.Show("You don't have VCREDIST Installed, please install it to use Spotify.", "Spotify");
                return false;
            }
            FileManager.LogInfo("Everything seemed good to RequirementsManager.");
            return true;
        }

        #region General Requirements for Spotify
        public static bool IsVCRedistInstalled()
        {
            // Visual C++ Redistributable for Visual Studio 2015, 2017, and 2019 check
            string regKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";

            using var key = Registry.LocalMachine.OpenSubKey(regKeyPath);

            if (key != null && key.GetValue("Installed") != null)
            {
                object? installedValue = key.GetValue("Installed");
                return installedValue != null && (int)installedValue == 1;
            }
            return false;
        }
        #endregion
        #region LGHUB
        public static bool IsMemoryIntegrityEnabled() // false if enabled true if disabled, you want it disabled
        {
            //credits to Themida
            string keyPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforceCodeIntegrity";
            string valueName = "Enabled";
            object? value = Registry.GetValue(keyPath, valueName, null);
            if (value != null && Convert.ToInt32(value) == 1)
            {
                new NoticeBar("You have Memory Integrity enabled, please disable it to use Logitech Driver", 7000).Show();
                return false;
            }
            else return true;
        }

        public static bool CheckForGhub()
        {
            try
            {
                Process? process = Process.GetProcessesByName("lghub").FirstOrDefault(); //gets the first process named "lghub"
                if (process == null)
                {
                    ShowLGHubNotRunningMessage();
                    return false;
                }

                string ghubfilepath = process.MainModule.FileName;
                if (ghubfilepath == null)
                {
                    FileManager.LogError($"An error occurred. Run as admin and try again.", true, 6000);
                    return false;
                }

                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(ghubfilepath);

                if (!versionInfo.ProductVersion.Contains("2021"))
                {
                    ShowLGHubImproperInstallMessage();
                    return false;
                }

                return true;
            }
            catch (AccessViolationException ex)
            {
                FileManager.LogError($"An error occured: {ex.Message}\nRun as admin and try again.", true, 6000);
                return false;
            }
        }

        private static void ShowLGHubNotRunningMessage()
        {
            if (MessageBox.Show("LG HUB is not running, is it installed?", "Spotify - LG HUB Mouse Movement", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
            {
                if (MessageBox.Show("Would you like to install it?", "Spotify - LG HUB Mouse Movement", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    new LGDownloader().Show();
                }
            }
        }

        private static void ShowLGHubImproperInstallMessage()
        {
            if (MessageBox.Show("LG HUB install is improper, would you like to install it?", "Spotify - LG HUB Mouse Movement", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                new LGDownloader().Show();
            }
        }
        #endregion
        #region RAZER
        public static bool CheckForRazerDevices(List<string> Razer_HID)
        {
            Razer_HID.Clear();
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Manufacturer LIKE 'Razer%'");
            var razerDevices = searcher.Get().Cast<ManagementBaseObject>();

            Razer_HID.AddRange(razerDevices.Select(device => device["DeviceID"]?.ToString() ?? string.Empty));

            return Razer_HID.Count != 0;
        }

        private static readonly string[] RazerSynapseProcesses =
        {
            "RazerAppEngine",
            "Razer Synapse",
            "Razer Synapse Beta",
            "Razer Synapse 3",
            "Razer Synapse 3 Beta"
        };
        public static async Task<bool> CheckRazerSynapseInstall() // returns true if running/installed and false if not installed/running
        {
            if (RazerSynapseProcesses.Any(processName => Process.GetProcessesByName(processName).Length != 0)) return true; // If any of them are running , return true

            var result = MessageBox.Show("Razer Synapse is not running (Or we cannot find the process), do you have it installed?",
                                         "Spotify - Razer Synapse", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                await RZMouse.InstallRazerSynapse();
                return false;
            }

            bool isSynapseInstalled = Directory.Exists(@"C:\Program Files\Razer") ||
                                      Directory.Exists(@"C:\Program Files (x86)\Razer") ||
                                      CheckRazerRegistryKey();

            if (!isSynapseInstalled)
            {
                var installConfirmation = MessageBox.Show("Razer Synapse is not installed, would you like to install it?",
                                                          "Spotify - Razer Synapse", MessageBoxButton.YesNo);

                if (installConfirmation == MessageBoxResult.Yes)
                {
                    await RZMouse.InstallRazerSynapse();
                    return false;
                }
            }

            return isSynapseInstalled;
        }

        private static bool CheckRazerRegistryKey()
        {
            using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Razer");
            return key != null;
        }
        #endregion
    }
}