using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DeathlessLauncher
{
    // Manifeste publié sur le serveur (version.json) et état installé localement (Game/installed.json). Même format :
    // { "version": "3", "nom": "0.1", "zip": "deathless-v3.zip", "sha256": "…", "size": 123456, "notes": "…" }.
    // « version » est le numéro de publication (il nomme le zip et décide de la mise à jour) ; « nom » (facultatif) est
    // la version du jeu affichée aux joueurs, qui sert aussi à retrouver l'entrée du changelog ; « notes » (facultatif)
    // reste lu pour la compatibilité avec le launcher de Relic, quand changelog.json manque.
    public sealed class Manifest
    {
        public string Version = "";
        public string Nom = "";
        public string Zip = "";
        public string Sha256 = "";
        public long Size;
        public string Notes = "";

        /// Nom affiché : « nom » s'il est donné, sinon le numéro de publication.
        public string Affichage => string.IsNullOrEmpty(Nom) ? Version : Nom;

        public static Manifest Parse(string json)
        {
            Dictionary<string, object> o = Json.Objet(Json.Lire(json));
            if (o == null) throw new InvalidDataException("JSON attendu : un objet { … }.");
            return new Manifest
            {
                Version = Json.Texte(o, "version"),
                Nom = Json.Texte(o, "nom"),
                Zip = Json.Texte(o, "zip"),
                Sha256 = Json.Texte(o, "sha256"),
                Size = long.TryParse(Json.Texte(o, "size"), out long size) ? size : 0,
                Notes = Json.Texte(o, "notes"),
            };
        }

        public string ToJson()
        {
            var o = new Dictionary<string, object>
            {
                ["version"] = Version ?? "",
                ["nom"] = Nom ?? "",
                ["zip"] = Zip ?? "",
                ["sha256"] = Sha256 ?? "",
                ["size"] = Size,
                ["notes"] = Notes ?? "",
            };
            return Json.Ecrire(o) + "\n";
        }
    }

    public sealed class LauncherConfig
    {
        public string BaseUrl = "";
        public string GameExe = "Deathless.exe";

        public static LauncherConfig Load(string path)
        {
            var config = new LauncherConfig();
            if (!File.Exists(path)) return config;
            Dictionary<string, object> o = Json.Objet(Json.Lire(File.ReadAllText(path, Encoding.UTF8)));
            if (o == null) return config;
            config.BaseUrl = Json.Texte(o, "baseUrl").Trim();
            string exe = Json.Texte(o, "gameExe").Trim();
            if (exe.Length > 0) config.GameExe = exe;
            if (config.BaseUrl.Length > 0 && !config.BaseUrl.EndsWith("/")) config.BaseUrl += "/";
            return config;
        }
    }

    public enum Phase { Recherche, Telechargement, Verification, Installation, Termine }

    /// Avancement transmis à l'interface (sur un fil de travail : l'interface le repasse à son Dispatcher).
    public sealed class Avancement
    {
        public Phase Phase;
        public string Version = "";   // nom affiché de la version en cours d'installation
        public long Fait;             // octets téléchargés
        public long Total;            // octets attendus (0 si inconnu)
        public double Debit;          // octets par seconde, lissé
        public bool Reprise;          // téléchargement repris là où il s'était arrêté
    }

    // Le travail du launcher : vérifier la version en ligne, télécharger et installer si besoin, lancer le jeu.
    // Tout est asynchrone ; l'interface reçoit l'avancement par le rappel Rapport.
    public sealed class Updater
    {
        public const string UserAgent = "DeathlessLauncher/1.0";
        public const string TempFolder = "DeathlessLauncher";
        static readonly TimeSpan ManifestTimeout = TimeSpan.FromSeconds(20);

        public Action<Avancement> Rapport = _ => { };

        private readonly LauncherConfig config;
        private readonly string rootDir;      // dossier du launcher
        private readonly string gameDir;      // Game/ à côté du launcher
        private readonly string installedPath;
        private readonly string changelogCache;

        public Updater(LauncherConfig config, string rootDir)
        {
            this.config = config;
            this.rootDir = rootDir;
            gameDir = Path.Combine(rootDir, "Game");
            installedPath = Path.Combine(gameDir, "installed.json");
            changelogCache = Path.Combine(rootDir, "changelog.cache.json");
        }

        public LauncherConfig Config => config;
        public string RootDir => rootDir;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(config.BaseUrl) && !config.BaseUrl.Contains("A-RENSEIGNER");
        public string GameDir => gameDir;
        public string GameExePath => Path.Combine(gameDir, config.GameExe);
        public bool IsInstalled => File.Exists(GameExePath) && File.Exists(installedPath);

        public Manifest Installed
        {
            get
            {
                if (!File.Exists(installedPath)) return null;
                try { return Manifest.Parse(File.ReadAllText(installedPath, Encoding.UTF8)); }
                catch { return null; }
            }
        }

        public static bool IsUpToDate(Manifest installed, Manifest remote, bool gamePresent)
        {
            return gamePresent && installed != null && remote != null && installed.Version == remote.Version
                && string.Equals(installed.Sha256, remote.Sha256, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<Manifest> FetchRemoteAsync(CancellationToken token)
        {
            // Pas de cache : le manifeste doit être relu à chaque lancement.
            string json = await GetStringAsync("version.json", token).ConfigureAwait(false);
            Manifest remote;
            try { remote = Manifest.Parse(json); }
            catch (Exception e) { throw new InvalidDataException("version.json illisible (" + e.Message + ")."); }
            if (string.IsNullOrEmpty(remote.Version) || string.IsNullOrEmpty(remote.Zip))
                throw new InvalidDataException("version.json incomplet (version ou zip manquant).");
            return remote;
        }

        /// changelog.json du serveur, gardé en copie locale pour le mode hors ligne. Jamais d'exception : null si absent
        /// ou illisible (le launcher se rabat alors sur « notes » de version.json).
        public async Task<string> FetchChangelogAsync(CancellationToken token)
        {
            try
            {
                string json = await GetStringAsync("changelog.json", token).ConfigureAwait(false);
                Changelog.Parse(json); // valide avant de remplacer la copie locale
                try { File.WriteAllText(changelogCache, json, new UTF8Encoding(false)); } catch { }
                return json;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch { return null; }
        }

        /// Dernier changelog lu en ligne (null s'il n'y en a jamais eu).
        public string CachedChangelog
        {
            get
            {
                try { return File.Exists(changelogCache) ? File.ReadAllText(changelogCache, Encoding.UTF8) : null; }
                catch { return null; }
            }
        }

        // Téléchargement dans un fichier temporaire (repris s'il a été interrompu), vérification SHA-256, extraction dans
        // Game/ (l'ancienne installation est retirée d'abord), écriture de installed.json.
        public async Task InstallAsync(Manifest remote, CancellationToken token)
        {
            string temp = Path.Combine(Path.GetTempPath(), TempFolder);
            Directory.CreateDirectory(temp);
            string zipPath = Path.Combine(temp, Path.GetFileName(remote.Zip));
            string partPath = zipPath + ".part";

            await DownloadAsync(config.BaseUrl + remote.Zip, partPath, remote, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(remote.Sha256))
            {
                Rapport(new Avancement { Phase = Phase.Verification, Version = remote.Affichage });
                string actual = await Task.Run(() => Sha256Of(partPath), token).ConfigureAwait(false);
                if (!string.Equals(actual, remote.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(partPath);
                    throw new InvalidDataException("Le fichier téléchargé est corrompu (empreinte SHA-256 différente).");
                }
            }
            TryDelete(zipPath);
            File.Move(partPath, zipPath);

            Rapport(new Avancement { Phase = Phase.Installation, Version = remote.Affichage });
            await Task.Run(() =>
            {
                // Extraction à côté (Game.nouveau), puis échange : un zip invalide ou une extraction ratée laisse
                // l'ancienne version intacte et jouable.
                string fresh = gameDir + ".nouveau";
                if (Directory.Exists(fresh)) Directory.Delete(fresh, true);
                Directory.CreateDirectory(fresh);
                try
                {
                    ExtractFlat(zipPath, fresh);
                    if (!File.Exists(Path.Combine(fresh, config.GameExe)))
                        throw new InvalidDataException(config.GameExe + " est absent du zip publié.");
                    File.WriteAllText(Path.Combine(fresh, "installed.json"), remote.ToJson(), new UTF8Encoding(false));
                }
                catch
                {
                    try { Directory.Delete(fresh, true); } catch { }
                    throw;
                }
                if (Directory.Exists(gameDir))
                    Directory.Delete(gameDir, true);
                Directory.Move(fresh, gameDir);
            }, token).ConfigureAwait(false);

            TryDelete(zipPath);
            Rapport(new Avancement { Phase = Phase.Termine, Version = remote.Affichage });
        }

        public void Launch()
        {
            var info = new ProcessStartInfo(GameExePath)
            {
                WorkingDirectory = gameDir,
                UseShellExecute = true,
            };
            Process.Start(info);
        }

        private async Task<string> GetStringAsync(string file, CancellationToken token)
        {
            using (var http = NewClient(ManifestTimeout))
            using (HttpResponseMessage response = await http.GetAsync(config.BaseUrl + file + "?t=" + DateTime.UtcNow.Ticks, token).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException(file + " : le serveur répond " + (int)response.StatusCode + " (" + response.ReasonPhrase + ").");
                byte[] bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                return Encoding.UTF8.GetString(bytes).TrimStart('﻿');
            }
        }

        // Si un .part du même zip existe (téléchargement coupé), on demande la suite (Range). Un serveur qui ne sait pas
        // reprendre renvoie le fichier entier : on repart de zéro. L'empreinte SHA-256 vérifie le résultat dans tous les cas.
        private async Task DownloadAsync(string url, string partPath, Manifest remote, CancellationToken token)
        {
            long existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
            if (remote.Size > 0 && existing > remote.Size) { TryDelete(partPath); existing = 0; }
            if (remote.Size > 0 && existing == remote.Size)
            {
                Rapport(new Avancement { Phase = Phase.Telechargement, Version = remote.Affichage, Fait = existing, Total = existing, Reprise = true });
                return;
            }

            using (var http = NewClient(TimeSpan.FromMinutes(30)))
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (existing > 0) request.Headers.Range = new RangeHeaderValue(existing, null);
                using (HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(remote.Zip + " : le serveur répond " + (int)response.StatusCode + " (" + response.ReasonPhrase + ").");
                    bool resumed = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                    if (!resumed) existing = 0;
                    long length = response.Content.Headers.ContentLength ?? -1;
                    long total = length >= 0 ? existing + length : remote.Size;

                    using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var target = new FileStream(partPath, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, true))
                    {
                        byte[] buffer = new byte[1 << 16];
                        long done = existing;
                        var clock = Stopwatch.StartNew();
                        long lastBytes = done;
                        double lastTime = 0, speed = 0, lastReport = -1;
                        Rapport(new Avancement { Phase = Phase.Telechargement, Version = remote.Affichage, Fait = done, Total = total, Reprise = resumed });
                        int read;
                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                        {
                            await target.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
                            done += read;
                            double now = clock.Elapsed.TotalSeconds;
                            if (now - lastTime >= 0.25)
                            {
                                double instant = (done - lastBytes) / (now - lastTime);
                                speed = speed <= 0 ? instant : speed * 0.7 + instant * 0.3;
                                lastBytes = done;
                                lastTime = now;
                            }
                            if (now - lastReport >= 0.1)
                            {
                                lastReport = now;
                                Rapport(new Avancement { Phase = Phase.Telechargement, Version = remote.Affichage, Fait = done, Total = total, Debit = speed, Reprise = resumed });
                            }
                        }
                        Rapport(new Avancement { Phase = Phase.Telechargement, Version = remote.Affichage, Fait = done, Total = Math.Max(total, done), Debit = speed, Reprise = resumed });
                        if (total > 0 && done < total)
                            throw new IOException("Téléchargement interrompu (" + Format.Mo(done) + " sur " + Format.Mo(total) + " Mo). Réessayer reprendra là où il s'est arrêté.");
                    }
                }
            }
        }

        // Si le zip contient un unique dossier racine (Deathless-v3/…), son contenu est remonté dans Game/.
        public static void ExtractFlat(string zipPath, string destination)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                string root = null;
                bool single = true;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    int slash = entry.FullName.IndexOf('/');
                    string first = slash < 0 ? null : entry.FullName.Substring(0, slash);
                    if (first == null) { single = false; break; }
                    if (root == null) root = first;
                    else if (root != first) { single = false; break; }
                }
                int skip = single && root != null ? root.Length + 1 : 0;

                string full = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string relative = entry.FullName.Length > skip ? entry.FullName.Substring(skip) : "";
                    if (relative.Length == 0) continue;
                    string target = Path.GetFullPath(Path.Combine(destination, relative.Replace('/', Path.DirectorySeparatorChar)));
                    if (!target.StartsWith(full, StringComparison.OrdinalIgnoreCase))
                        continue; // entrée hors du dossier cible : ignorée
                    if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(target); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    entry.ExtractToFile(target, true);
                }
            }
        }

        public static string Sha256Of(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static HttpClient NewClient(TimeSpan timeout)
        {
            // TLS 1.2 explicite : .NET Framework 4.8 le prend par défaut, on force pour les Windows 10 anciens.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var http = new HttpClient { Timeout = timeout };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            return http;
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    /// Lecture et écriture JSON par JavaScriptSerializer (System.Web.Extensions, fourni avec .NET Framework 4.8).
    public static class Json
    {
        public static object Lire(string json) => new JavaScriptSerializer().DeserializeObject(json ?? "");

        public static string Ecrire(object o) => new JavaScriptSerializer().Serialize(o);

        public static Dictionary<string, object> Objet(object o) => o as Dictionary<string, object>;

        public static string Texte(Dictionary<string, object> o, string key)
        {
            if (o == null || !o.TryGetValue(key, out object v) || v == null) return "";
            if (v is string s) return s;
            if (v is IFormattable f) return f.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            return v.ToString();
        }
    }

    /// Mise en forme française des tailles, débits et durées.
    public static class Format
    {
        static readonly System.Globalization.CultureInfo Fr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");

        /// Mégaoctets : une décimale sous 100 Mo, entier au-delà (« 12,4 », « 845 »).
        public static string Mo(long bytes)
        {
            double mo = bytes / 1048576.0;
            return mo < 100 ? mo.ToString("0.0", Fr) : Math.Floor(mo).ToString("0", Fr);
        }

        public static string Debit(double bytesPerSecond)
        {
            if (bytesPerSecond <= 0) return "";
            double mo = bytesPerSecond / 1048576.0;
            if (mo >= 1) return mo.ToString(mo < 10 ? "0.0" : "0", Fr) + " Mo/s";
            return (bytesPerSecond / 1024.0).ToString("0", Fr) + " Ko/s";
        }

        public static string Duree(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) return "";
            int s = (int)Math.Ceiling(seconds);
            if (s < 60) return s + " s";
            int m = s / 60;
            if (m < 60) return m + " min " + (s % 60).ToString("00") + " s";
            return (m / 60) + " h " + (m % 60).ToString("00") + " min";
        }
    }
}
