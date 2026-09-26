using System;
using System.Collections.Generic;
using Deathless.Jeu;
using Unity.Netcode;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Un statut tel que l'hôte le synchronise (NetworkList de EnnemiReseau et HerosReseau) : 15 octets. La fin est en
    /// temps serveur (NetworkManager.ServerTime) : chaque client en déduit la durée restante sans autre message.
    public struct StatutReseau : INetworkSerializable, IEquatable<StatutReseau>
    {
        public byte type, origine, source;
        public float intensite, duree, fin;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref type);
            s.SerializeValue(ref origine);
            s.SerializeValue(ref source);
            s.SerializeValue(ref intensite);
            s.SerializeValue(ref duree);
            s.SerializeValue(ref fin);
        }

        public bool Equals(StatutReseau o) => type == o.type && origine == o.origine && source == o.source
            && intensite == o.intensite && duree == o.duree && fin == o.fin;
    }

    /// Synchronisation des statuts (Docs/reseau.md, « Statuts ») : l'hôte fait foi. Il écrit la liste d'un personnage
    /// dans sa NetworkList seulement quand elle change (ajout, retrait, fin, ou fin décalée de plus de `Tolerance` : un
    /// rafraîchissement de brûlure à chaque tic du cône n'envoie donc presque rien) ; NGO n'envoie que les éléments
    /// modifiés. Les clients relisent la liste à chaque changement reçu, et font défiler les durées eux-mêmes.
    public static class StatutsReseau
    {
        /// Décalage de fin (s) en deçà duquel un rafraîchissement n'est pas envoyé.
        public const float Tolerance = 0.5f;
        /// Durée maximale acceptée d'un statut demandé par un client (s).
        public const float DureeMax = 30f;

        static readonly List<Statut> s_Tampon = new List<Statut>();

        static float Maintenant(NetworkManager nm) => nm != null && nm.IsListening ? nm.ServerTime.TimeAsFloat : Time.time;

        /// Hôte : recopie les statuts (sauf ceux de zone, sans durée, que chaque poste calcule) dans la NetworkList.
        public static void Ecrire(NetworkList<StatutReseau> liste, Statuts st, NetworkManager nm)
        {
            if (liste == null || st == null) return;
            float maintenant = Maintenant(nm), t = Time.time;
            int n = 0;
            var l = st.Liste;
            for (int i = 0; i < l.Count; i++)
            {
                var s = l[i];
                if (s.Permanent) continue;
                var r = new StatutReseau
                {
                    type = (byte)s.type, origine = (byte)s.origine, source = (byte)Mathf.Clamp(s.sourceId, 0, 255),
                    intensite = s.intensite, duree = s.duree, fin = maintenant + (s.fin - t),
                };
                if (n < liste.Count)
                {
                    var a = liste[n];
                    if (a.type != r.type || a.origine != r.origine || a.source != r.source || Mathf.Abs(a.intensite - r.intensite) > 0.001f
                        || Mathf.Abs(a.fin - r.fin) > Tolerance || Mathf.Abs(a.duree - r.duree) > Tolerance)
                        liste[n] = r;
                }
                else liste.Add(r);
                n++;
            }
            while (liste.Count > n) liste.RemoveAt(liste.Count - 1);
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
                    default: return s;
                }
                return new Statut { type = type, duree = duree, intensite = intensite, origine = origine, sourceId = 0 };
            }
            switch (type)
            {
                case TypeStatut.Brulure: duree = b.brulureDuree; intensite = b.brulureDegats; break;
                case TypeStatut.Ralenti: intensite = Mathf.Clamp(intensite, 0f, 0.9f); break;
                default: return s;
            }
            return new Statut { type = type, duree = duree, intensite = intensite, origine = OrigineStatut.Joueur, sourceId = joueurId };
        }
    }
}
