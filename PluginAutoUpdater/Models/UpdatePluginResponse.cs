using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PluginAutoUpdater.Models
{
    public class UpdatePluginResponse
    {
        public Dictionary<string, UpdatePluginInfo> pluginsToUpdate { get; set; } = new Dictionary<string, UpdatePluginInfo>();
    }
}
