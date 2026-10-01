using System;
using System.Collections.Generic;

namespace Deathless.UI.Donnees
{
    /// Un succès tel que l'écran Succès et la bannière le montrent (Wiki/pages/succes.md).
    public interface ISuccesVue
    {
        string Id { get; }
        string Nom { get; }
        string Description { get; }
        /// Rubrique du wiki (Premiers pas, Tenir la nuit…).
        string Categorie { get; }
        /// Caché : montré « ??? » tant qu'il est verrouillé.
        bool Cache { get; }
        bool Debloque { get; }
        /// Date de déblocage (heure locale), null s'il est verrouillé.
        DateTime? Date { get; }
        /// Compteur (succès à statistique) : valeur actuelle et seuil ; Seuil ≤ 1 : pas de progression à montrer.
        int Progression { get; }
        int Seuil { get; }
        /// Identifiant d'icône (table IconesUI).
        string Icone { get; }
    }

    /// Liste des succès du joueur local (posée par le module Deathless.Succes au lancement du jeu, toujours présente).
    public interface IListeSucces
    {
        IReadOnlyList<ISuccesVue> Tous { get; }
        int NombreDebloques { get; }
        /// Un succès vient d'être débloqué sur ce poste (bannière).
        event Action<ISuccesVue> Debloque;
    }
}
