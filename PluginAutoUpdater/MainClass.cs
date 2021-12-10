using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using PluginAutoUpdater.Enums;
using PluginAutoUpdater.Models;
using RoundRestarting;
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
        public override Version Version { get; } = new Version(1, 0, 9);

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

        public void CheckUpdates()
        {
            NextUpdateCheck = DateTime.Now.AddSeconds(120);

            if (CachedHashes == null)
            {
                CachedHashes = new Dictionary<string, string>();
                foreach (var file in Directory.GetFiles(Paths.Plugins, "*.dll", SearchOption.AllDirectories))
                {
                    var hash = CalculateMD5(file);

                    if (!CachedHashes.ContainsKey(hash))
                        CachedHashes.Add(hash, file);
                }
            }


            Log.Info("Get updates from ApiEndpoint...");
            var updates = CheckUpdates(Config.ApiEndpoint, new CheckUpdatesModel()
            {
                ExiledVersion = Loader.Version.ToString(3),
                SLVersion = GameCore.Version.VersionString,
                Hashes = CachedHashes.Keys.ToList()
            });

            if (updates == null)
                return;

            int updated = 0;

            foreach (var plugin in updates.filesToUpdate)
            {
                var file = CachedHashes[plugin.Key];

                var name = Path.GetFileNameWithoutExtension(file);

                if (Config.PluginBlacklist.Contains(name))
                {
                    Log.Info($"Skip update for file \"{name}\".");
                    continue;
                }

                Log.Info($"Updating file \"{name}\" from version {plugin.Value.oldVersion} to {plugin.Value.newVersion}.");

                File.Delete(CachedHashes[plugin.Key]);
                using (var client = new WebClient())
                {
                    client.DownloadFile(plugin.Value.downloadURL, file);
                }
                Log.Info($"{name} was updated successfully!");
                updated++;
            }

            if (updated != 0)
            {
                switch (Config.UpdateAction)
                {
                    case UpdateAction.Nothing:
                        Log.Info($"Downloaded {updated} updates.");
                        break;
                    case UpdateAction.RestartNextRound:
                        Log.Info($"Downloaded {updated} updates, server will be restarted next round.");
                        ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                        break;
                    case UpdateAction.RestartNow:
                        Log.Info($"Downloaded {updated} updates, restarting server.");
                        ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                        RoundRestart.InitiateRoundRestart();
                        break;
                    case UpdateAction.RestartNowIfEmpty:
                        if (Player.List.Count() == 0 || !Round.IsStarted)
                        {
                            Log.Info($"Downloaded {updated} updates, restarting server.");
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            RoundRestart.InitiateRoundRestart();
                        }
                        else
                        {
                            Log.Info($"Downloaded {updated} updates, server will be restarted next round (Server is not empty).");
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                        }
                        break;
                }
                Updated = true;
            }

            Log.Info("Checking updates ended.");
        }

        public DateTime NextUpdateCheck = DateTime.Now.AddSeconds(5);
        public CoroutineHandle? Handler;
        public Dictionary<string, string> CachedHashes;
        public bool Updated = false;

        public IEnumerator<float> CheckUpdatesHandler()
        {
            while (true)
            {
                try
                {
                    if (NextUpdateCheck < DateTime.Now && !Updated)
                        CheckUpdates();
                }
                catch (Exception ex) 
                {
                    Log.Error(ex.ToString());
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += Server_WaitingForPlayers;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= Server_WaitingForPlayers;
            base.OnDisabled();
        }

        private void Server_WaitingForPlayers()
        {
            if (!Handler.HasValue)
                Handler = Timing.RunCoroutine(CheckUpdatesHandler());
        }
    }
}
