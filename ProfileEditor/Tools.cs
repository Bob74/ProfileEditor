using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
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

    }
}
