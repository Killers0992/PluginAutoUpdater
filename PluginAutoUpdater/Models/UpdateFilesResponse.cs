using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PluginAutoUpdater.Models
{
    public class UpdateFilesResponse
    {
        public Dictionary<string, UpdateFileInfo> filesToUpdate { get; set; } = new Dictionary<string, UpdateFileInfo>();
    }
}
