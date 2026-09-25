using System.Collections.Generic;
using UnityEngine;

// Équipe un personnage au squelette KayKit Rig_Medium à partir d'un asset WeaponStyle (même méthode que la scène
// WeaponBench et que ClassGear.AttachTo de Relic) :
// 1. crée les sockets manquants listés dans `requiredSockets` (champ boneName au format "hand.r -> handslot.r" :
//    os parent -> socket, position / rotation / échelle locales du style) ; les rigs Adventurers les ont déjà,
//    Mannequin_Medium.fbx non ;
// 2. instancie chaque pièce de `attachments` sous son os (boneName), à la position / rotation / échelle du style.
// Utilisable en édition (les pièces restent des instances de prefab, sauvegardées dans la scène) comme en jeu.
public static class MannequinEquip
{
    // Retourne les pièces instanciées (dans l'ordre de style.attachments ; null si l'os est introuvable).
    public static List<GameObject> Equiper(GameObject personnage, WeaponStyle style)
    {
        List<GameObject> pieces = new List<GameObject>();
        if (personnage == null || style == null)
            return pieces;
        if (style.requiredSockets != null)
            foreach (WeaponStyle.Attachment s in style.requiredSockets)
                CreerSocket(personnage.transform, s);
        if (style.attachments != null)
            foreach (WeaponStyle.Attachment a in style.attachments)
            {
                Transform os = Trouver(personnage.transform, a.boneName);
                if (os == null || a.prefab == null)
                {
                    Debug.LogWarning("MannequinEquip : os '" + a.boneName + "' introuvable sur " + personnage.name + " (style " + style.name + ")");
                    pieces.Add(null);
                    continue;
                }
                GameObject piece = Instancier(a.prefab, os);
                piece.transform.localPosition = a.localPosition;
                piece.transform.localRotation = Quaternion.Euler(a.localEuler);
                piece.transform.localScale = Vector3.one * (a.localScale > 0f ? a.localScale : 1f);
                pieces.Add(piece);
            }
        return pieces;
    }

    // Socket "parent -> socket" : créé s'il n'existe pas déjà quelque part dans la hiérarchie.
    public static Transform CreerSocket(Transform racine, WeaponStyle.Attachment socket)
    {
        string[] parts = socket.boneName.Split(new[] { "->" }, System.StringSplitOptions.None);
        if (parts.Length != 2)
            return null;
        string parent = parts[0].Trim(), nom = parts[1].Trim();
        Transform existant = Trouver(racine, nom);
        if (existant != null)
            return existant;
        Transform os = Trouver(racine, parent);
        if (os == null)
            return null;
        Transform t = new GameObject(nom).transform;
        t.SetParent(os, false);
        t.localPosition = socket.localPosition;
        t.localRotation = Quaternion.Euler(socket.localEuler);
        t.localScale = Vector3.one * (socket.localScale > 0f ? socket.localScale : 1f);
        return t;
    }

    // Pose alternative (visée de l'arc, arbalète en main...) : chaque pièce qui en a une (hasAlternate) est rangée sous
    // son os alternatif (ou ramenée à sa pose de base) ; les pièces sont retrouvées par le nom de leur modèle.
    public static void PoseAlternative(GameObject personnage, WeaponStyle style, bool alternative)
    {
        if (personnage == null || style == null || style.attachments == null) return;
        foreach (WeaponStyle.Attachment a in style.attachments)
        {
            if (a.prefab == null || !a.hasAlternate) continue;
            Transform piece = Trouver(personnage.transform, a.prefab.name);
            Transform os = Trouver(personnage.transform, alternative && !string.IsNullOrEmpty(a.alternateBone) ? a.alternateBone : a.boneName);
            if (piece == null || os == null) continue;
            piece.SetParent(os, false);
            piece.localPosition = alternative ? a.alternatePosition : a.localPosition;
            piece.localRotation = Quaternion.Euler(alternative ? a.alternateEuler : a.localEuler);
        }
    }

    public static Transform Trouver(Transform racine, string nom)
    {
        foreach (Transform t in racine.GetComponentsInChildren<Transform>(true))
            if (t.name == nom)
                return t;
        return null;
    }

    private static GameObject Instancier(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
        return Object.Instantiate(prefab, parent);
    }
}
