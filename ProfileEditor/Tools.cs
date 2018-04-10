using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;

using Microsoft.Win32;

namespace ProfileEditor
{
    class Tools
    {
        public static string GetBaseDir()
        {
            object gamePath = GetGamePath();
            if (gamePath != null) return gamePath.ToString() + "\\scripts\\NoMoreShortcuts";

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static object GetGamePath()
        {
            object value = null;
            List<string[]> keys = new List<string[]>
            {
                new string[] { "SOFTWARE\\WOW6432Node\\Rockstar Games\\Grand Theft Auto V", "InstallFolder" },
                new string[] { "SOFTWARE\\Rockstar Games\\Grand Theft Auto V", "InstallFolder" },
                new string[] { "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{E01FA564-2094-4833-8F2F-1FFEC6AFCC46}", "InstallLocation" },
            };

            foreach (string[] key in keys)
            {
                RegistryKey reg = Registry.LocalMachine.OpenSubKey(key[0], false);
                value = reg?.GetValue(key[1]) ?? null;

                if (value != null)
                    if (value.ToString() != "") return value;
            }

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
