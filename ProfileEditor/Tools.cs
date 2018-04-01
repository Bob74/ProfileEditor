using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ProfileEditor
{
    class Tools
    {
        public static string GetGamePath(string key = "SOFTWARE\\WOW6432Node\\Rockstar Games\\Grand Theft Auto V")
        {
            object value = "";

            RegistryKey reg = Registry.LocalMachine.OpenSubKey(key, false);
            value = reg?.GetValue("InstallFolder") ?? null;

            if (value != null)
                return value.ToString();
            else
                return null;
        }

        public static bool IsUpdateAvailable()
        {
            string downloadedString = "";
            Version onlineVersion;

            try
            {
                WebClient client = new WebClient();
                downloadedString = client.DownloadString("https://raw.githubusercontent.com/Bob74/ProfileEditor/master/version");

                downloadedString = downloadedString.Replace("\r", "");
                downloadedString = downloadedString.Replace("\n", "");

                onlineVersion = new Version(downloadedString);

                client.Dispose();

                if (onlineVersion.CompareTo(Assembly.GetExecutingAssembly().GetName().Version) > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {

            }
            return false;
        }

        public static string[] GetResourceNames()
        {
            var asm = Assembly.GetEntryAssembly();
            string resName = asm.GetName().Name + ".g.resources";
            using (var stream = asm.GetManifestResourceStream(resName))
            using (var reader = new ResourceReader(stream))
            {
                return reader.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();
            }
        }

    }
}
