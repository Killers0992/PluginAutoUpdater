using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PluginAutoUpdater.Models
{
    public class UpdatePluginInfo
    {
        public string newVersion { get; set; }
        public string newSLVersion { get; set; }
        public string newExiledVersion { get; set; }
        public string downloadURL { get; set; }
    }
}
