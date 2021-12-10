using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Loader;
using PluginAutoUpdater.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Utf8Json;

namespace PluginAutoUpdater
{
    public class MainClass : Plugin<PluginConfig>
    {
        public override string Name { get; } = "PluginAutoUpdater";
        public override string Author { get; } = "Killers0992";
        public override string Prefix { get; } = "pluginautoupdater";

        public override Version RequiredExiledVersion { get; } = new Version(4, 0, 0);
        public override Version Version { get; } = new Version(1, 0, 5);

        public override PluginPriority Priority { get; } = PluginPriority.Last;

        string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public UpdateFilesResponse CheckUpdates(string apiUrl, CheckUpdatesModel model)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{apiUrl}/checkupdates");
            request.Method = "POST";
            request.ContentType = "application/json";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(model)));
            }

            HttpWebResponse webResponse;
            try
            {
                webResponse = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {
                Log.Info($"[CheckUpdates] API is not responding! (website is down)");
                return null;
            }

            UpdateFilesResponse result = null;
            using (var webStream = webResponse.GetResponseStream())
            {
                using (var responseReader = new StreamReader(webStream))
                {
                    var response = responseReader.ReadToEnd();

                    switch (webResponse.StatusCode)
                    {
                        case HttpStatusCode.BadGateway:
                            Log.Info($"[CheckUpdates] API is not responding! (website is down)");
                            break;
                        case HttpStatusCode.BadRequest:
                            Log.Info($"[CheckStatus] API returned status code \"BadRequest\".");
                            break;
                        default:
                            result = JsonSerializer.Deserialize<UpdateFilesResponse>(response);
                            break;
                    }
                }
            }
            return result;
        }

        public override void OnEnabled()
        {
            var files = new Dictionary<string, string>();
            foreach(var file in Directory.GetFiles(Paths.Plugins, "*.dll", SearchOption.AllDirectories))
            {
                var hash = CalculateMD5(file);

                if (!files.ContainsKey(hash))
                    files.Add(hash, file);
            }

            Log.Info("Get updates from ApiEndpoint...");
            var updates = CheckUpdates(Config.ApiEndpoint, new CheckUpdatesModel()
            {
                ExiledVersion = Loader.Version.ToString(3),
                SLVersion = GameCore.Version.VersionString,
                Hashes = files.Keys.ToList()
            });

            if (updates == null)
                return;

            foreach (var plugin in updates.filesToUpdate)
            {
                var file = files[plugin.Key];

                var name = Path.GetFileNameWithoutExtension(file);

                if (Config.PluginBlacklist.Contains(name))
                {
                    Log.Info($"Skip update for file \"{name}\".");
                    continue;
                }

                Log.Info($"Updating file \"{name}\" from version {plugin.Value.oldVersion} to {plugin.Value.newVersion}.");

                File.Delete(files[plugin.Key]);
                using (var client = new WebClient())
                {
                    client.DownloadFile(plugin.Value.downloadURL, file);
                }
                Log.Info($"{name} was updated successfully!");
            }
            Log.Info("Checking updates ended.");
            base.OnEnabled();
        }
    }
}
