using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeathlessLauncher
{
    /// Un fichier du build publié (v<N>/fichiers.json) : chemin relatif, taille et SHA-256 du fichier installé ; s'il est
    /// servi compressé (<chemin>.gz), la taille compressée (c'est elle qui est téléchargée).
    public sealed class FichierPublie
    {
        public string Chemin = "";
        public long Taille;
        public string Sha256 = "";
        public long TailleGz;
        public bool Gz => TailleGz > 0;
        public long ATelecharger => Gz ? TailleGz : Taille;
    }

    public sealed class ManifesteFichiers
    {
        public string Version = "";
        public List<FichierPublie> Fichiers = new List<FichierPublie>();

        public static ManifesteFichiers Parse(string json)
        {
            Dictionary<string, object> o = Json.Objet(Json.Lire(json)) ?? throw new InvalidDataException("fichiers.json : objet attendu.");
            var m = new ManifesteFichiers { Version = Json.Texte(o, "version") };
            if (!(o.TryGetValue("fichiers", out object liste) && liste is object[] tableau)) throw new InvalidDataException("fichiers.json : liste « fichiers » introuvable.");
            foreach (object element in tableau)
            {
                Dictionary<string, object> e = Json.Objet(element);
                if (e == null) continue;
                var f = new FichierPublie
                {
                    Chemin = Json.Texte(e, "chemin"),
                    Taille = long.TryParse(Json.Texte(e, "taille"), out long t) ? t : -1,
                    Sha256 = Json.Texte(e, "sha256").ToLowerInvariant(),
                    TailleGz = long.TryParse(Json.Texte(e, "gz"), out long g) ? g : 0,
                };
                // Chemin relatif sûr : pas de retour en arrière ni de chemin absolu.
                if (f.Chemin.Length == 0 || f.Taille < 0 || f.Sha256.Length != 64 || f.Chemin.StartsWith("/") || f.Chemin.Contains("..") || f.Chemin.Contains(":"))
                    throw new InvalidDataException("fichiers.json : entrée invalide (" + f.Chemin + ").");
                m.Fichiers.Add(f);
            }
            if (m.Fichiers.Count == 0) throw new InvalidDataException("fichiers.json : aucun fichier.");
            return m;
        }
    }

    /// Débit réaliste : moyenne glissante sur les 5 dernières secondes (et non un pic instantané).
    public sealed class MesureDebit
    {
        readonly Queue<(double t, long octets)> points = new Queue<(double, long)>();
        readonly Stopwatch horloge = Stopwatch.StartNew();
        readonly object verrou = new object();

        public double Ajouter(long total)
        {
            lock (verrou)
            {
                double t = horloge.Elapsed.TotalSeconds;
                points.Enqueue((t, total));
                while (points.Count > 2 && t - points.Peek().t > 5) points.Dequeue();
                var premier = points.Peek();
                double duree = t - premier.t;
                return duree >= 0.5 ? Math.Max(0, (total - premier.octets) / duree) : 0;
            }
        }
    }

    /// Téléchargement tolérant aux connexions lentes et instables : reprise du fichier partiel (Range), nouvelles
    /// tentatives avec attente croissante, détection d'un transfert bloqué, aucun délai global (un gros fichier sur une
    /// connexion lente peut durer longtemps).
    public static class Telechargement
    {
        /// Nombre d'échecs de suite tolérés sans aucun progrès (le compte repart à zéro dès qu'un octet arrive).
        public static int Tentatives = 8;
        /// Attente avant la n-ième nouvelle tentative : Base × 2^(n-1), au plus 60 s.
        public static TimeSpan AttenteBase = TimeSpan.FromSeconds(2);
        /// Sans le moindre octet reçu pendant ce délai, la connexion est jugée bloquée et reprise.
        public static TimeSpan Blocage = TimeSpan.FromSeconds(60);

        /// Erreur qui ne se corrige pas en réessayant (fichier absent, accès refusé).
        public sealed class ErreurDefinitive : Exception
        {
            public ErreurDefinitive(string message) : base(message) { }
        }

        /// Télécharge url dans partPath (repris s'il existe) jusqu'à attendu octets (0 : taille inconnue).
        /// surOctets reçoit chaque bloc reçu (négatif si le serveur refuse la reprise et que le partiel est jeté).
        public static async Task TelechargerAsync(string url, string partPath, long attendu, Action<long> surOctets, CancellationToken token)
        {
            int echecs = 0;
            Directory.CreateDirectory(Path.GetDirectoryName(partPath));
            while (true)
            {
                long existant = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
                if (attendu > 0 && existant > attendu) { File.Delete(partPath); surOctets(-existant); existant = 0; }
                if (attendu > 0 && existant == attendu) return;
                long recus = 0;
                try
                {
                    bool complet = await UneTentativeAsync(url, partPath, existant, attendu, n => { recus += n; surOctets(n); }, token).ConfigureAwait(false);
                    if (complet) return;
                    throw new IOException("transfert coupé");
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (ErreurDefinitive) { throw; }
                catch (Exception e) when (e is IOException || e is HttpRequestException || e is WebException || e is OperationCanceledException || e is ObjectDisposedException)
                {
                    echecs = recus > 0 ? 1 : echecs + 1; // un transfert qui a avancé remet le compte à un
                    if (echecs >= Tentatives)
                        throw new IOException("Téléchargement impossible après " + Tentatives + " tentatives (" + Path.GetFileName(url) + " : " + e.Message + "). Réessayer reprendra là où il s'est arrêté.", e);
                    double s = Math.Min(60, AttenteBase.TotalSeconds * Math.Pow(2, echecs - 1));
                    await Task.Delay(TimeSpan.FromSeconds(s), token).ConfigureAwait(false);
                }
            }
        }

        static async Task<bool> UneTentativeAsync(string url, string partPath, long existant, long attendu, Action<long> surOctets, CancellationToken token)
        {
            using (var garde = CancellationTokenSource.CreateLinkedTokenSource(token))
            using (HttpClient http = Updater.NouveauClient(Timeout.InfiniteTimeSpan))
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                garde.CancelAfter(Blocage);
                if (existant > 0) request.Headers.Range = new RangeHeaderValue(existant, null);
                using (HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, garde.Token).ConfigureAwait(false))
                using (garde.Token.Register(() => { try { response.Dispose(); } catch { } }))
                {
                    int code = (int)response.StatusCode;
                    if (code == 404 || code == 403 || code == 410)
                        throw new ErreurDefinitive(Path.GetFileName(url) + " : le serveur répond " + code + " (" + response.ReasonPhrase + ").");
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(Path.GetFileName(url) + " : le serveur répond " + code + " (" + response.ReasonPhrase + ").");
                    bool reprise = existant > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                    if (existant > 0 && !reprise) { surOctets(-existant); existant = 0; }
                    long longueur = response.Content.Headers.ContentLength ?? -1;
                    long total = longueur >= 0 ? existant + longueur : attendu;
                    using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var cible = new FileStream(partPath, reprise ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, true))
                    {
                        byte[] tampon = new byte[1 << 16];
                        long fait = existant;
                        int lu;
                        while ((lu = await source.ReadAsync(tampon, 0, tampon.Length, garde.Token).ConfigureAwait(false)) > 0)
                        {
                            garde.CancelAfter(Blocage);
                            await cible.WriteAsync(tampon, 0, lu, garde.Token).ConfigureAwait(false);
                            fait += lu;
                            surOctets(lu);
                        }
                        return total <= 0 || fait >= total;
                    }
                }
            }
        }
    }

    public sealed partial class Updater
    {
        /// Nombre de téléchargements simultanés en mise à jour incrémentielle (utile pour les petits fichiers).
        public static int Paralleles = 3;

        long octetsRecus;
        /// Octets réellement reçus du réseau pendant cette session (jauge, bancs de test).
        public long OctetsRecus => Interlocked.Read(ref octetsRecus);
        /// « incrémentielle » ou « zip » : comment la dernière installation s'est faite.
        public string ModeInstallation { get; private set; } = "";
        /// Pourquoi la voie incrémentielle n'a pas été prise ou a échoué (vide si elle a servi).
        public string RaisonZip { get; private set; } = "";
        /// Fichiers téléchargés / réutilisés lors de la dernière mise à jour incrémentielle.
        public int FichiersTelecharges { get; private set; }
        public int FichiersReutilises { get; private set; }

        string DossierTemporaire => Path.Combine(Path.GetTempPath(), TempFolder);
        string DossierNouveau => gameDir + ".nouveau";
        string DossierAncien => gameDir + ".ancien";

        /// Au démarrage : restes d'une session interrompue. Une installation coupée pendant l'échange des dossiers est
        /// remise en place (Game.ancien redevient Game) ; Game.nouveau est supprimé.
        public void NettoyerAuDemarrage()
        {
            try
            {
                if (!Directory.Exists(gameDir) && Directory.Exists(DossierAncien)) Directory.Move(DossierAncien, gameDir);
                SupprimerDossier(DossierAncien);
                SupprimerDossier(DossierNouveau);
            }
            catch { }
        }

        /// Plus rien à télécharger (jeu à jour) : le dossier temporaire est vidé.
        public void NettoyerTemporaires() => SupprimerDossier(DossierTemporaire);

        /// Avant une installation : tout ce qui ne sert pas à la version visée part (autres versions, zips d'avant).
        /// Les fichiers partiels de CETTE version restent, pour reprendre un téléchargement coupé (même après un
        /// redémarrage du launcher, ce qui compte sur une connexion lente).
        void NettoyerAvantInstallation(Manifest remote)
        {
            SupprimerDossier(DossierNouveau);
            SupprimerDossier(DossierAncien);
            try
            {
                if (!Directory.Exists(DossierTemporaire)) return;
                string garderDossier = "v" + remote.Version, garderZip = Path.GetFileName(remote.Zip) + ".part";
                foreach (string d in Directory.GetDirectories(DossierTemporaire))
                    if (!string.Equals(Path.GetFileName(d), garderDossier, StringComparison.OrdinalIgnoreCase)) SupprimerDossier(d);
                foreach (string f in Directory.GetFiles(DossierTemporaire))
                    if (!string.Equals(Path.GetFileName(f), garderZip, StringComparison.OrdinalIgnoreCase)) TryDelete(f);
            }
            catch { }
        }

        static void SupprimerDossier(string d)
        {
            try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch { }
        }

        /// Échange Game et Game.nouveau (l'ancien passe par Game.ancien, remis en place si l'échange échoue).
        void Echanger(string nouveau)
        {
            SupprimerDossier(DossierAncien);
            if (Directory.Exists(gameDir))
            {
                try { Directory.Move(gameDir, DossierAncien); }
                catch (IOException e) { throw new IOException("Impossible de remplacer l'installation (le jeu est-il ouvert ? " + e.Message + ")", e); }
            }
            try { Directory.Move(nouveau, gameDir); }
            catch
            {
                if (!Directory.Exists(gameDir) && Directory.Exists(DossierAncien)) Directory.Move(DossierAncien, gameDir);
                throw;
            }
            SupprimerDossier(DossierAncien);
        }

        static string EncoderChemin(string chemin) => string.Join("/", chemin.Split('/').Select(Uri.EscapeDataString));

        /// Mise à jour fichier par fichier. Renvoie faux si la voie ne convient pas (le zip est alors pris), lève une
        /// exception si elle a échoué (le zip est aussi tenté ensuite). L'installation jouable n'est jamais touchée
        /// avant l'échange final.
        async Task<bool> InstallIncrementielAsync(Manifest remote, CancellationToken token)
        {
            string baseV = config.BaseUrl + remote.Dossier.TrimEnd('/') + "/";
            ManifesteFichiers m = ManifesteFichiers.Parse(await GetStringAsync(remote.Dossier.TrimEnd('/') + "/fichiers.json", token).ConfigureAwait(false));
            if (m.Version != remote.Version) throw new InvalidDataException("fichiers.json annonce la version " + m.Version + " au lieu de " + remote.Version + ".");

            // Comparaison : un fichier installé est repris s'il a la bonne taille et la bonne empreinte (recalculée :
            // un fichier abîmé est ainsi retéléchargé).
            Rapport(new Avancement { Phase = Phase.Comparaison, Version = remote.Affichage });
            var reprendre = new List<FichierPublie>();
            var telecharger = new List<FichierPublie>();
            bool installe = IsInstalled;
            await Task.Run(() =>
            {
                foreach (FichierPublie f in m.Fichiers)
                {
                    token.ThrowIfCancellationRequested();
                    string local = Path.Combine(gameDir, f.Chemin.Replace('/', Path.DirectorySeparatorChar));
                    bool bon = installe && File.Exists(local) && new FileInfo(local).Length == f.Taille
                        && string.Equals(Sha256Of(local), f.Sha256, StringComparison.OrdinalIgnoreCase);
                    (bon ? reprendre : telecharger).Add(f);
                }
            }, token).ConfigureAwait(false);
            long total = telecharger.Sum(f => f.ATelecharger);
            // Presque tout à télécharger (première installation, tout a changé) : le zip, en un seul fichier, fait
            // aussi bien avec bien moins de requêtes.
            if (remote.Size > 0 && total >= remote.Size * 0.9)
            {
                RaisonZip = installe ? "presque tout a changé (" + Format.Mo(total) + " Mo sur " + Format.Mo(remote.Size) + " Mo)" : "première installation";
                return false;
            }

            string nouveau = DossierNouveau;
            string tempV = Path.Combine(DossierTemporaire, "v" + remote.Version);
            SupprimerDossier(nouveau);
            Directory.CreateDirectory(nouveau);
            try
            {
                // Fichiers inchangés : copiés depuis l'installation actuelle.
                await Task.Run(() =>
                {
                    foreach (FichierPublie f in reprendre)
                    {
                        token.ThrowIfCancellationRequested();
                        string rel = f.Chemin.Replace('/', Path.DirectorySeparatorChar);
                        string cible = Path.Combine(nouveau, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(cible));
                        File.Copy(Path.Combine(gameDir, rel), cible, true);
                    }
                }, token).ConfigureAwait(false);

                // Fichiers nouveaux ou modifiés : téléchargés (Paralleles à la fois), vérifiés un par un.
                var mesure = new MesureDebit();
                long fait = 0;
                int finis = 0;
                double dernier = -1;
                var horloge = Stopwatch.StartNew();
                foreach (FichierPublie f in telecharger)
                {
                    string part = Path.Combine(tempV, f.Chemin.Replace('/', Path.DirectorySeparatorChar) + (f.Gz ? ".gz" : "") + ".part");
                    if (File.Exists(part)) fait += Math.Min(new FileInfo(part).Length, f.ATelecharger); // reprise
                }
                object verrou = new object();
                Action<bool> rapporter = force =>
                {
                    lock (verrou)
                    {
                        double t = horloge.Elapsed.TotalSeconds;
                        long f = Interlocked.Read(ref fait);
                        double debit = mesure.Ajouter(f);
                        if (!force && t - dernier < 0.1) return;
                        dernier = t;
                        Rapport(new Avancement
                        {
                            Phase = Phase.Telechargement, Version = remote.Affichage, Fait = f, Total = total, Debit = debit,
                            Incrementiel = true, FichiersFaits = finis, FichiersTotal = telecharger.Count,
                        });
                    }
                };
                rapporter(true);
                using (var places = new SemaphoreSlim(Math.Max(1, Paralleles)))
                {
                    var taches = telecharger.Select(async f =>
                    {
                        await places.WaitAsync(token).ConfigureAwait(false);
                        try
                        {
                            await FichierAsync(f, baseV, tempV, nouveau, n => { Interlocked.Add(ref fait, n); if (n > 0) Interlocked.Add(ref octetsRecus, n); rapporter(false); }, token).ConfigureAwait(false);
                            Interlocked.Increment(ref finis);
                            rapporter(true);
                        }
                        finally { places.Release(); }
                    }).ToList();
                    await Task.WhenAll(taches).ConfigureAwait(false);
                }

                if (!File.Exists(Path.Combine(nouveau, config.GameExe)))
                    throw new InvalidDataException(config.GameExe + " est absent de la version publiée.");
                Rapport(new Avancement { Phase = Phase.Installation, Version = remote.Affichage });
                File.WriteAllText(Path.Combine(nouveau, "installed.json"), remote.ToJson(), new UTF8Encoding(false));
                await Task.Run(() => Echanger(nouveau), token).ConfigureAwait(false);
                FichiersTelecharges = telecharger.Count;
                FichiersReutilises = reprendre.Count;
                return true;
            }
            finally
            {
                SupprimerDossier(nouveau); // réussite : déjà échangé ; échec : préparation abandonnée
            }
        }

        /// Un fichier : téléchargé (repris si coupé), décompressé si besoin, vérifié par son SHA-256 ; une empreinte
        /// fausse le fait retélécharger (trois fois au plus).
        async Task FichierAsync(FichierPublie f, string baseV, string tempV, string nouveau, Action<long> surOctets, CancellationToken token)
        {
            string url = baseV + EncoderChemin(f.Chemin) + (f.Gz ? ".gz" : "");
            string part = Path.Combine(tempV, f.Chemin.Replace('/', Path.DirectorySeparatorChar) + (f.Gz ? ".gz" : "") + ".part");
            string cible = Path.Combine(nouveau, f.Chemin.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(cible));
            for (int essai = 1; ; essai++)
            {
                await Telechargement.TelechargerAsync(url, part, f.ATelecharger, surOctets, token).ConfigureAwait(false);
                string empreinte = await Task.Run(() =>
                {
                    try
                    {
                        if (f.Gz)
                        {
                            using (var entree = File.OpenRead(part))
                            using (var gz = new GZipStream(entree, CompressionMode.Decompress))
                            using (var sortie = File.Create(cible))
                                gz.CopyTo(sortie);
                        }
                        else
                            File.Copy(part, cible, true);
                        return Sha256Of(cible);
                    }
                    catch (InvalidDataException) { return ""; } // gzip abîmé : comme une empreinte fausse
                }, token).ConfigureAwait(false);
                TryDelete(part);
                if (string.Equals(empreinte, f.Sha256, StringComparison.OrdinalIgnoreCase)) return;
                TryDelete(cible);
                surOctets(-f.ATelecharger);
                if (essai >= 3) throw new InvalidDataException(f.Chemin + " : empreinte SHA-256 différente après " + essai + " téléchargements.");
            }
        }
    }
}
