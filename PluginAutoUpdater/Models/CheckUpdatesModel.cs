using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAutoUpdater.Models
{
    public class CheckUpdatesModel
    {
        public string SLVersion { get; set; }
        public string ExiledVersion { get; set; }
        public List<string> PluginHashes { get; set; } = new List<string>();
    }
}
