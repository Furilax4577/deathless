using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DeathlessLauncher
{
    // changelog.json, publié à côté de version.json :
    // {
    //   "versions": [
    //     { "version": "0.1", "date": "2026-09-25", "titre": "…", "notes": [ "…", "…" ] }
    //   ]
    // }
    // « version » est le nom affiché (le « nom » de version.json, ou son numéro s'il n'a pas de nom) ; « date » au format
    // AAAA-MM-JJ ; « titre » est facultatif ; « notes » est une liste de lignes (une puce chacune), ou un seul texte dont
    // chaque ligne devient une puce. Le launcher trie du plus récent au plus ancien (date, puis numéro de version).
    public sealed class EntreeChangelog
    {
        public string Version = "";
        public string Date = "";      // AAAA-MM-JJ, ou vide
        public string Titre = "";
        public List<string> Notes = new List<string>();

        public string DateAffichee
        {
            get
            {
                if (DateTime.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                    return d.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR"));
                return Date;
            }
        }
    }

    public static class Changelog
    {
        public static List<EntreeChangelog> Parse(string json)
        {
            object root = Json.Lire(json);
            IEnumerable<object> versions;
            if (Json.Objet(root) is Dictionary<string, object> o && o.TryGetValue("versions", out object v) && v is object[] arr)
                versions = arr;
            else if (root is object[] direct)
                versions = direct; // tolérance : un tableau d'entrées sans enveloppe
            else
                throw new InvalidDataException("changelog.json : liste « versions » introuvable.");

            var list = new List<EntreeChangelog>();
            foreach (object item in versions)
            {
                Dictionary<string, object> e = Json.Objet(item);
                if (e == null) continue;
                var entree = new EntreeChangelog
                {
                    Version = Json.Texte(e, "version").Trim(),
                    Date = Json.Texte(e, "date").Trim(),
                    Titre = Json.Texte(e, "titre").Trim(),
                };
                if (e.TryGetValue("notes", out object notes))
                {
                    if (notes is object[] lignes)
                        foreach (object l in lignes) AjouterLignes(entree.Notes, l as string);
                    else
                        AjouterLignes(entree.Notes, notes as string);
                }
                if (entree.Version.Length > 0) list.Add(entree);
            }
            return Trier(list);
        }

        /// Liste affichée : le changelog s'il existe, complété par les notes de version.json quand la version publiée
        /// n'y figure pas (compatibilité avec le launcher de Relic, qui ne connaissait que « notes »).
        public static List<EntreeChangelog> Construire(string changelogJson, Manifest remote)
        {
            List<EntreeChangelog> list;
            try { list = string.IsNullOrWhiteSpace(changelogJson) ? new List<EntreeChangelog>() : Parse(changelogJson); }
            catch { list = new List<EntreeChangelog>(); }

            if (remote != null && !list.Any(e => MemeVersion(e.Version, remote.Affichage)))
            {
                var entree = new EntreeChangelog { Version = remote.Affichage };
                AjouterLignes(entree.Notes, remote.Notes);
                list.Insert(0, entree); // même sans note : la pastille « Nouvelle » annonce la mise à jour
            }
            return list;
        }

        public static bool MemeVersion(string a, string b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

        static void AjouterLignes(List<string> notes, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• ")) line = line.Substring(2).Trim();
                if (line.Length > 0) notes.Add(line);
            }
        }

        // Du plus récent au plus ancien : date décroissante (sans date : après les datées), puis numéro de version
        // décroissant ; à égalité, l'ordre du fichier.
        static List<EntreeChangelog> Trier(List<EntreeChangelog> list)
        {
            return list
                .Select((e, i) => new { e, i })
                .OrderByDescending(x => x.e.Date.Length > 0)
                .ThenByDescending(x => x.e.Date, StringComparer.Ordinal)
                .ThenByDescending(x => x.e.Version, new ComparateurVersions())
                .ThenBy(x => x.i)
                .Select(x => x.e)
                .ToList();
        }

        /// Compare « 0.10 » et « 0.9 » segment par segment, numériquement quand c'est possible.
        public sealed class ComparateurVersions : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                string[] pa = (a ?? "").Split('.', '-', ' '), pb = (b ?? "").Split('.', '-', ' ');
                for (int i = 0; i < Math.Max(pa.Length, pb.Length); i++)
                {
                    string x = i < pa.Length ? pa[i] : "0", y = i < pb.Length ? pb[i] : "0";
                    int c = long.TryParse(x, out long nx) && long.TryParse(y, out long ny) ? nx.CompareTo(ny) : string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
                    if (c != 0) return c;
                }
                return 0;
            }
        }
    }
}
