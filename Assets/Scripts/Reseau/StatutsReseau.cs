using System;
using System.Collections.Generic;
using Deathless.Jeu;
using Unity.Netcode;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Un statut tel que l'hôte le synchronise (NetworkList de EnnemiReseau et HerosReseau) : 24 octets. La fin est en
    /// temps serveur (NetworkManager.ServerTime) : chaque client en déduit la durée restante sans autre message.
    /// Brûlure en paliers (01/10/2026) : palier et jauge au dernier coup de feu, et début de la redescente (temps
    /// serveur) ; chaque client calcule la redescente lui-même (Brulure.Etat), rien n'est envoyé tant que le feu cesse.
    public struct StatutReseau : INetworkSerializable, IEquatable<StatutReseau>
    {
        public byte type, origine, source, palier;
        public float intensite, duree, fin, jauge, descente;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref type);
            s.SerializeValue(ref origine);
            s.SerializeValue(ref source);
            s.SerializeValue(ref intensite);
            s.SerializeValue(ref duree);
            s.SerializeValue(ref fin);
            s.SerializeValue(ref palier);
            s.SerializeValue(ref jauge);
            s.SerializeValue(ref descente);
        }

        public bool Equals(StatutReseau o) => type == o.type && origine == o.origine && source == o.source
            && intensite == o.intensite && duree == o.duree && fin == o.fin
            && palier == o.palier && jauge == o.jauge && descente == o.descente;
    }

    /// Synchronisation des statuts (Docs/reseau.md, « Statuts ») : l'hôte fait foi. Il écrit la liste d'un personnage
    /// dans sa NetworkList seulement quand elle change (ajout, retrait, fin, ou fin décalée de plus de `Tolerance` : un
    /// tic du cône n'envoie que le palier et la jauge de brûlure de l'ennemi touché, la redescente n'envoie rien) ; les éléments sont appariés par
    /// type et NGO n'envoie que ceux qui ont changé. Les clients relisent la liste à chaque changement reçu, et font
    /// défiler les durées eux-mêmes.
    public static class StatutsReseau
    {
        /// Décalage de fin (s) en deçà duquel un rafraîchissement n'est pas envoyé.
        public const float Tolerance = 0.5f;
        /// Durée maximale acceptée d'un statut demandé par un client (s).
        public const float DureeMax = 30f;

        static readonly List<Statut> s_Tampon = new List<Statut>();

        static float Maintenant(NetworkManager nm) => nm != null && nm.IsListening ? nm.ServerTime.TimeAsFloat : Time.time;

        /// Hôte : recopie les statuts (sauf ceux de zone, sans durée, que chaque poste calcule) dans la NetworkList.
        /// Appariement par type (un seul statut à durée par type : Statuts.Appliquer) et non par position : retirer un
        /// statut n'envoie que son retrait (les suivants ne sont pas réécrits), un nouveau statut s'ajoute en fin de
        /// liste, un statut modifié n'envoie que lui. L'ordre reste celui de l'hôte (retraits sur place, ajouts en fin).
        public static void Ecrire(NetworkList<StatutReseau> liste, Statuts st, NetworkManager nm)
        {
            if (liste == null || st == null) return;
            float maintenant = Maintenant(nm), t = Time.time;
            var l = st.Liste;
            // Retraits : types qui ne sont plus sur le personnage (en partant de la fin, les indices restent valables).
            for (int k = liste.Count - 1; k >= 0; k--)
                if (IndexType(l, liste[k].type) < 0) liste.RemoveAt(k);
            // Modifications et ajouts.
            for (int i = 0; i < l.Count; i++)
            {
                var s = l[i];
                if (s.Permanent) continue;
                var r = new StatutReseau
                {
                    type = (byte)s.type, origine = (byte)s.origine, source = (byte)Mathf.Clamp(s.sourceId, 0, 255),
                    intensite = s.intensite, duree = s.duree, fin = maintenant + (s.fin - t),
                    palier = s.palier, jauge = s.jauge, descente = maintenant + (s.descente - t),
                };
                int k = IndexType(liste, r.type);
                if (k >= 0)
                {
                    var a = liste[k];
                    if (a.origine != r.origine || a.source != r.source || Mathf.Abs(a.intensite - r.intensite) > 0.001f
                        || Mathf.Abs(a.fin - r.fin) > Tolerance || Mathf.Abs(a.duree - r.duree) > Tolerance
                        // Brûlure : un coup de feu change palier ou jauge (la redescente, elle, se calcule chez le client).
                        || a.palier != r.palier || Mathf.Abs(a.jauge - r.jauge) > 0.01f || Mathf.Abs(a.descente - r.descente) > 0.05f)
                        liste[k] = r;
                }
                else liste.Add(r);
            }
        }

        /// Index du statut à durée de ce type dans la liste du personnage (-1 s'il n'y est pas).
        static int IndexType(IReadOnlyList<Statut> l, byte type)
        {
            for (int i = 0; i < l.Count; i++) if (!l[i].Permanent && (byte)l[i].type == type) return i;
            return -1;
        }

        /// Index du statut de ce type dans la NetworkList (-1 s'il n'y est pas).
        static int IndexType(NetworkList<StatutReseau> liste, byte type)
        {
            for (int i = 0; i < liste.Count; i++) if (liste[i].type == type) return i;
            return -1;
        }

        /// Client : la liste de l'hôte devient celle du personnage (fins converties en Time.time de ce poste).
        public static void Lire(NetworkList<StatutReseau> liste, Statuts st, NetworkManager nm)
        {
            if (liste == null || st == null) return;
            float maintenant = Maintenant(nm), t = Time.time;
            s_Tampon.Clear();
            for (int i = 0; i < liste.Count; i++)
            {
                var r = liste[i];
                s_Tampon.Add(new Statut
                {
                    type = (TypeStatut)r.type, origine = (OrigineStatut)r.origine, sourceId = r.source,
                    intensite = r.intensite, duree = r.duree, fin = t + (r.fin - maintenant),
                    palier = r.palier, jauge = r.jauge, descente = t + (r.descente - maintenant),
                });
            }
            st.Recevoir(s_Tampon);
        }

        /// Hôte : demande d'un client, vérifiée. Sur un ennemi : brûlure (valeurs de l'hôte, créditée au joueur qui la
        /// demande) ou ralenti ; l'étourdissement et la provocation ont leurs propres relais. Sur son propre héros : ralenti
        /// (chute, eau), étourdi (garde brisée), ivresse (taverne). Refus : type Aucun.
        public static Statut Valider(TypeStatut type, float duree, float intensite, OrigineStatut origine, int joueurId, bool surSonHeros)
        {
            var b = GameBalance.Courant;
            var s = new Statut { type = TypeStatut.Aucun };
            duree = Mathf.Clamp(duree, 0f, DureeMax);
            if (duree <= 0f) return s;
            if (surSonHeros)
            {
                switch (type)
                {
                    case TypeStatut.Ralenti: intensite = Mathf.Clamp(intensite, 0f, 0.9f); break;
                    case TypeStatut.Etourdi: case TypeStatut.Ivresse: case TypeStatut.Renverse: intensite = 1f; break;
                    // Peau de fer (27/09/2026) : rugissement du viking, valeurs de l'hôte (réduction et durée de GameBalance).
                    case TypeStatut.PeauDeFer: intensite = b.peauDeFerReduction; duree = b.peauDeFerDuree; break;
                    default: return s;
                }
                return new Statut { type = type, duree = duree, intensite = intensite, origine = origine, sourceId = type == TypeStatut.PeauDeFer ? joueurId : 0 };
            }
            switch (type)
            {
                // Brûlure en paliers (01/10/2026) : `intensite` porte le remplissage cumulé par le client entre deux demandes
                // (Statuts.AttiserBrulure) ; plafonné à ce qu'un mage peut remplir en un intervalle de demande, avec marge.
                // Brulure.MarqueurPalier (01/10/2026) : un palier d'un coup (grande boule de feu, mur de flammes).
                case TypeStatut.Brulure: duree = b.brulureDuree; intensite = intensite >= Brulure.MarqueurPalier ? Brulure.MarqueurPalier : Mathf.Clamp(intensite, 0f, 1.5f); break;
                case TypeStatut.Ralenti: intensite = Mathf.Clamp(intensite, 0f, 0.9f); break;
                default: return s;
            }
            return new Statut { type = type, duree = duree, intensite = intensite, origine = OrigineStatut.Joueur, sourceId = joueurId };
        }
    }
}
