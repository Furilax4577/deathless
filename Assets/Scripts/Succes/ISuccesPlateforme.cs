using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Deathless.Succes
{
    /// Plateforme qui garde les succès et les statistiques (Wiki/pages/succes.md, « Intégration Steam ») : locale dès
    /// maintenant (SuccesLocal), Steam plus tard (Steamworks.NET : SteamUserStats.SetAchievement, SetStat, GetStat,
    /// GetAchievementAndUnlockTime, StoreStats), sans toucher au reste du jeu. Les identifiants sont ceux du catalogue
    /// (`ACH_…`, `STAT_…`), déclarés à l'identique dans Steamworks.
    public interface ISuccesPlateforme
    {
        /// Nom court (journal) : « local », « steam ».
        string Nom { get; }
        /// Faux tant que la plateforme n'a pas lu son état (Steam : RequestCurrentStats en attente).
        bool Prete { get; }
        /// Débloqué ? Date de déblocage (heure locale) si oui.
        bool EstDebloque(string id, out DateTime date);
        /// Débloque le succès ; vrai s'il ne l'était pas encore (Steam : SetAchievement, puis Stocker).
        bool Debloquer(string id);
        /// Valeur entière d'une statistique (0 si jamais écrite).
        int LireStat(string stat);
        /// Écrit une statistique (Steam : SetStat ; poussée par Stocker, pas à chaque tir).
        void ReglerStat(string stat, int valeur);
        /// Envoie ce qui a changé (disque en local ; Steam : StoreStats). Appelé au déblocage, en fin de nuit, au retour
        /// au menu et à la sortie du jeu.
        void Stocker();
        /// Efface tout (outil de test, jamais appelé par le jeu).
        void ToutEffacer();
    }

    /// Succès et statistiques gardés en local, en JSON dans Application.persistentDataPath : `succes.json`, ou
    /// `succes.<profil>.json` sous un profil de test (`-deathless-profil=client2`, comme les PlayerPrefs du lobby et
    /// l'identité du poste) : deux postes de test sur une machine gardent chacun les leurs. Hors ligne, c'est aussi ce que
    /// la version Steam renverra à la connexion suivante.
    public sealed class SuccesLocal : ISuccesPlateforme
    {
        [Serializable] class Entree { public string id; public long date; }
        [Serializable] class Stat { public string id; public int valeur; }
        [Serializable] class Fichier { public int version = 1; public List<Entree> succes = new List<Entree>(); public List<Stat> stats = new List<Stat>(); }

        Fichier m_Donnees = new Fichier();
        bool m_Sale;
        public string Chemin { get; }
        public string Nom => "local";
        public bool Prete => true;

        public SuccesLocal(string profil)
        {
            Chemin = Path.Combine(Application.persistentDataPath, string.IsNullOrEmpty(profil) ? "succes.json" : "succes." + profil + ".json");
            Lire();
        }

        void Lire()
        {
            try
            {
                if (!File.Exists(Chemin)) return;
                var f = JsonUtility.FromJson<Fichier>(File.ReadAllText(Chemin));
                if (f != null) { m_Donnees = f; m_Donnees.succes ??= new List<Entree>(); m_Donnees.stats ??= new List<Stat>(); }
            }
            catch (Exception e) { Debug.LogWarning("[Succès] lecture impossible de " + Chemin + " : " + e.Message); }
        }

        public bool EstDebloque(string id, out DateTime date)
        {
            foreach (var e in m_Donnees.succes)
                if (e.id == id) { date = DateTimeOffset.FromUnixTimeSeconds(e.date).LocalDateTime; return true; }
            date = default;
            return false;
        }

        public bool Debloquer(string id)
        {
            if (string.IsNullOrEmpty(id) || EstDebloque(id, out _)) return false;
            m_Donnees.succes.Add(new Entree { id = id, date = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
            m_Sale = true;
            return true;
        }

        public int LireStat(string stat)
        {
            foreach (var s in m_Donnees.stats) if (s.id == stat) return s.valeur;
            return 0;
        }

        public void ReglerStat(string stat, int valeur)
        {
            if (string.IsNullOrEmpty(stat)) return;
            foreach (var s in m_Donnees.stats)
                if (s.id == stat) { if (s.valeur != valeur) { s.valeur = valeur; m_Sale = true; } return; }
            m_Donnees.stats.Add(new Stat { id = stat, valeur = valeur });
            m_Sale = true;
        }

        public void Stocker()
        {
            if (!m_Sale) return;
            try
            {
                string tmp = Chemin + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(m_Donnees, true));
                if (File.Exists(Chemin)) File.Delete(Chemin);
                File.Move(tmp, Chemin);
                m_Sale = false;
            }
            catch (Exception e) { Debug.LogWarning("[Succès] écriture impossible de " + Chemin + " : " + e.Message); }
        }

        public void ToutEffacer()
        {
            m_Donnees = new Fichier();
            m_Sale = true;
            Stocker();
        }
    }
}
