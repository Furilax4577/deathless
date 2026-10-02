using UnityEditor;
using UnityEngine;
using Deathless.Jeu.Dev;

/// Entrées de menu de l'écoute des bruits de pas par matière (02/10/2026) : en Play, dans une partie lancée, le héros
/// local marche successivement sur l'herbe, la terre, le sable, la pierre, le bois, l'eau et le métal (ScenariosPas).
public static class PasMatiereMenu
{
    [MenuItem("Deathless/Jeu/Pas par matière (Play) : parcours de 3 s par matière")]
    static void Parcours() { Debug.Log("[Pas] " + ScenariosPas.Lancer(3f)); }

    [MenuItem("Deathless/Jeu/Pas par matière (Play) : écoute lente, 6 s par matière")]
    static void ParcoursLent() { Debug.Log("[Pas] " + ScenariosPas.Lancer(6f)); }

    [MenuItem("Deathless/Jeu/Pas par matière (Play) : parcours de 3 s par matière", true)]
    static bool ParcoursOk() { return Application.isPlaying; }

    [MenuItem("Deathless/Jeu/Pas par matière (Play) : écoute lente, 6 s par matière", true)]
    static bool ParcoursLentOk() { return Application.isPlaying; }
}
