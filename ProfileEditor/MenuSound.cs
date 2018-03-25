using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProfileEditor
{
    public class MenuSound
    {
        public static readonly int DefaultVolume = 25;

        public string File { get; set; }
        public int Volume { get; set; }

        public MenuSound()
        {
            File = "";
            Volume = DefaultVolume;
        }
        public MenuSound(MenuSound sound)
        {
            File = sound.File;
            Volume = sound.Volume;
        }
    }
}
