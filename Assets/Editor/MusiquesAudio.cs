using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Deathless.Audio;

namespace Deathless.EditorTools
{
    /// Branche les fichiers de Assets/Audio/Deathless/Musique/ (jour_1, jour_2, nuit_1, nuit_2, taverne ; .ogg, .wav ou .mp3) sur
    /// ReglagesAudio (listes lues par LecteurMusique) à chaque import ou suppression, et règle leur import en flux (Streaming,
    /// Vorbis) pour ne pas charger des morceaux de plusieurs minutes en mémoire. Menu Deathless > Audio > Brancher les musiques.
    public class MusiquesAudio : AssetPostprocessor
    {
        const string Dossier = "Assets/Audio/Deathless/Musique";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Dossier + "/") || !Attendu(assetPath)) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            s.preloadAudioData = false;   // Unity 6.3 : réglage déplacé dans AudioImporterSampleSettings
            imp.defaultSampleSettings = s;
            imp.forceToMono = false;
        }

        static void OnPostprocessAllAssets(string[] importes, string[] supprimes, string[] deplaces, string[] deplacesDe)
        {
            if (!Touche(importes) && !Touche(supprimes) && !Touche(deplaces) && !Touche(deplacesDe)) return;
            EditorApplication.delayCall += Brancher;
        }

        static bool Attendu(string c)
        {
            string n = Path.GetFileNameWithoutExtension(c).ToLowerInvariant();
            return n.StartsWith("jour") || n.StartsWith("nuit") || n.StartsWith("taverne");
        }

        static bool Touche(string[] chemins)
        {
            foreach (var c in chemins) if (c.StartsWith(Dossier + "/")) return true;
            return false;
        }

        [MenuItem("Deathless/Audio/Brancher les musiques")]
        public static void Brancher()
        {
            var r = AssetDatabase.LoadAssetAtPath<ReglagesAudio>("Assets/Audio/Resources/DeathlessAudio.asset");
            if (r == null) return;
            var jour = new List<AudioClip>(); var nuit = new List<AudioClip>(); var taverne = new List<AudioClip>();
            if (Directory.Exists(Dossier))
            {
                var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { Dossier });
                var chemins = new List<string>();
                foreach (var g in guids) chemins.Add(AssetDatabase.GUIDToAssetPath(g));
                chemins.Sort(System.StringComparer.OrdinalIgnoreCase);
                foreach (var c in chemins)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(c);
                    string n = Path.GetFileNameWithoutExtension(c).ToLowerInvariant();
                    if (clip == null) continue;
                    if (n.StartsWith("jour")) jour.Add(clip);
                    else if (n.StartsWith("nuit")) nuit.Add(clip);
                    else if (n.StartsWith("taverne")) taverne.Add(clip);
                }
            }
            if (Egal(r.musiquesJour, jour) && Egal(r.musiquesNuit, nuit) && Egal(r.musiquesTaverne, taverne)) return;
            r.musiquesJour = jour.ToArray(); r.musiquesNuit = nuit.ToArray(); r.musiquesTaverne = taverne.ToArray();
            EditorUtility.SetDirty(r);
            AssetDatabase.SaveAssetIfDirty(r);
            Debug.Log("[Musique] branchées : jour " + jour.Count + ", nuit " + nuit.Count + ", taverne " + taverne.Count);
        }

        static bool Egal(AudioClip[] a, List<AudioClip> b)
        {
            if (a == null) return b.Count == 0;
            if (a.Length != b.Count) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
