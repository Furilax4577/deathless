using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deathless.UI;
using Deathless.UI.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Deathless.UI.EditorTools
{
    /// Génère les assets du socle d'interface (menu Deathless > UI). Chaque étape est rejouable : elle met à jour
    /// l'asset existant au lieu d'en créer un nouveau, ce qui garde les GUID.
    public static class DeathlessUISetup
    {
        const string FontsSource = "Assets/Art/Fonts/Fredoka";
        const string FontsFolder = "Assets/UI/Fonts";
        const string ThemeFolder = "Assets/UI/Theme";
        const string PanelPath = "Assets/UI/PanelSettings/DeathlessPanel.asset";
        const string TextSettingsPath = "Assets/UI/Theme/DeathlessTextSettings.asset";
        const string ThemePath = "Assets/UI/Theme/DeathlessTheme.tss";
        const string GlyphsPath = "Assets/UI/Resources/DeathlessInputGlyphs.asset";
        const string ActionsPath = "Assets/Input/DeathlessControls.inputactions";
        const string KenneyRoot = "Assets/Art/UI/KenneyInputPrompts";
        const string DemoScenePath = "Assets/Scenes/UISocle.unity";
        const string DemoUxmlPath = "Assets/UI/Screens/UISocle/UISocle.uxml";

        static readonly string[] s_Weights = { "Regular", "Medium", "SemiBold", "Bold" };

        /// Caractères préchargés dans les atlas (le reste s'ajoute à la volée : atlas dynamiques).
        public const string PreloadCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÀÂÄÆÇÉÈÊËÎÏÔŒÙÛÜŸàâäæçéèêëîïôœùûüÿ" +
            "«»‘’“”–—×·…€°²  ";

        [MenuItem("Deathless/UI/Tout régénérer (polices, panneau, icônes, scène)")]
        public static void All()
        {
            CreateFonts();
            CreatePanelSettings();
            CreateGlyphs();
            CreateDemoScene();
        }

        // ---------------------------------------------------------------- Polices

        [MenuItem("Deathless/UI/1. Polices Fredoka (FontAsset)")]
        public static string CreateFonts()
        {
            EnsureFolder(FontsFolder);
            var log = new System.Text.StringBuilder();
            var assets = new Dictionary<string, FontAsset>();
            foreach (var weight in s_Weights)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontsSource}/Fredoka-{weight}.ttf");
                if (font == null)
                {
                    log.AppendLine($"Police introuvable : Fredoka-{weight}.ttf");
                    continue;
                }
                var path = $"{FontsFolder}/Fredoka-{weight}-SDF.asset";
                var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                if (asset == null)
                {
                    asset = FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                        AtlasPopulationMode.Dynamic, true);
                    asset.name = $"Fredoka-{weight}-SDF";
                    AssetDatabase.CreateAsset(asset, path);
                    var tex = asset.atlasTexture;
                    tex.name = asset.name + " Atlas";
                    AssetDatabase.AddObjectToAsset(tex, asset);
                    asset.material.name = asset.name + " Material";
                    AssetDatabase.AddObjectToAsset(asset.material, asset);
                }

                asset.TryAddCharacters(PreloadCharacters, out var missing);
                // Les pages d'atlas créées par TryAddCharacters doivent être enregistrées dans l'asset.
                foreach (var t in asset.atlasTextures)
                {
                    if (t == null || AssetDatabase.Contains(t)) continue;
                    t.name = asset.name + " Atlas " + System.Array.IndexOf(asset.atlasTextures, t);
                    AssetDatabase.AddObjectToAsset(t, asset);
                }
                EditorUtility.SetDirty(asset);
                assets[weight] = asset;
                log.AppendLine($"{asset.name} : {asset.characterTable.Count} caractères, {asset.atlasTextureCount} atlas"
                               + (string.IsNullOrEmpty(missing) ? "" : $", absents de la police : « {missing} »"));
            }

            // Graisse grasse (-unity-font-style: bold) de Regular = Fredoka Bold, et table de graisses complète.
            if (assets.TryGetValue("Regular", out var regular))
            {
                var table = regular.fontWeightTable;
                if (table != null && table.Length >= 8)
                {
                    table[4].regularTypeface = regular;
                    if (assets.TryGetValue("Medium", out var medium)) table[5].regularTypeface = medium;
                    if (assets.TryGetValue("SemiBold", out var semi)) table[6].regularTypeface = semi;
                    if (assets.TryGetValue("Bold", out var bold)) table[7].regularTypeface = bold;
                    // Le tableau est renvoyé par référence : les affectations ci-dessus suffisent.
                    EditorUtility.SetDirty(regular);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[DeathlessUISetup] Polices\n" + log);
            return log.ToString();
        }

        // ---------------------------------------------------------------- Panneau et texte

        [MenuItem("Deathless/UI/2. PanelSettings et réglages de texte")]
        public static void CreatePanelSettings()
        {
            EnsureFolder(Path.GetDirectoryName(PanelPath).Replace('\\', '/'));
            var regular = AssetDatabase.LoadAssetAtPath<FontAsset>($"{FontsFolder}/Fredoka-Regular-SDF.asset");

            var textSettings = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(TextSettingsPath);
            if (textSettings == null)
            {
                textSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(textSettings, TextSettingsPath);
            }
            textSettings.defaultFontAsset = regular;
            if (textSettings.fallbackFontAssets == null) textSettings.fallbackFontAssets = new List<FontAsset>();
            EditorUtility.SetDirty(textSettings);

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelPath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.scale = 1f;
            panel.clearColor = false;
            var so = new SerializedObject(panel);
            so.FindProperty("textSettings").objectReferenceValue = textSettings;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- Icônes

        static readonly string[,] s_Xbox =
        {
            { "buttonsouth", "xbox_button_color_a" }, { "buttoneast", "xbox_button_color_b" },
            { "buttonwest", "xbox_button_color_x" }, { "buttonnorth", "xbox_button_color_y" },
            { "leftshoulder", "xbox_lb" }, { "rightshoulder", "xbox_rb" },
            { "lefttrigger", "xbox_lt" }, { "righttrigger", "xbox_rt" },
            { "leftstickpress", "xbox_ls" }, { "rightstickpress", "xbox_rs" },
            { "leftstick", "xbox_stick_l" }, { "rightstick", "xbox_stick_r" },
            { "leftstick/up", "xbox_stick_l_up" }, { "leftstick/down", "xbox_stick_l_down" },
            { "leftstick/left", "xbox_stick_l_left" }, { "leftstick/right", "xbox_stick_l_right" },
            { "rightstick/up", "xbox_stick_r_up" }, { "rightstick/down", "xbox_stick_r_down" },
            { "rightstick/left", "xbox_stick_r_left" }, { "rightstick/right", "xbox_stick_r_right" },
            { "dpad", "xbox_dpad" }, { "dpad/up", "xbox_dpad_up" }, { "dpad/down", "xbox_dpad_down" },
            { "dpad/left", "xbox_dpad_left" }, { "dpad/right", "xbox_dpad_right" },
            { "select", "xbox_button_view" }, { "start", "xbox_button_menu" },
        };

        static readonly string[,] s_PlayStation =
        {
            { "buttonsouth", "playstation_button_color_cross" }, { "buttoneast", "playstation_button_color_circle" },
            { "buttonwest", "playstation_button_color_square" }, { "buttonnorth", "playstation_button_color_triangle" },
            { "leftshoulder", "playstation_trigger_l1" }, { "rightshoulder", "playstation_trigger_r1" },
            { "lefttrigger", "playstation_trigger_l2" }, { "righttrigger", "playstation_trigger_r2" },
            { "leftstickpress", "playstation_button_l3" }, { "rightstickpress", "playstation_button_r3" },
            { "leftstick", "playstation_stick_l" }, { "rightstick", "playstation_stick_r" },
            { "leftstick/up", "playstation_stick_l_up" }, { "leftstick/down", "playstation_stick_l_down" },
            { "leftstick/left", "playstation_stick_l_left" }, { "leftstick/right", "playstation_stick_l_right" },
            { "rightstick/up", "playstation_stick_r_up" }, { "rightstick/down", "playstation_stick_r_down" },
            { "rightstick/left", "playstation_stick_r_left" }, { "rightstick/right", "playstation_stick_r_right" },
            { "dpad", "playstation_dpad" }, { "dpad/up", "playstation_dpad_up" }, { "dpad/down", "playstation_dpad_down" },
            { "dpad/left", "playstation_dpad_left" }, { "dpad/right", "playstation_dpad_right" },
            { "select", "playstation5_button_create" }, { "start", "playstation5_button_options" },
            { "touchpadbutton", "playstation5_touchpad_press" },
        };

        // Touches à libellé anglais (ESC, DEL, CTRL, ALT, HOME…) volontairement absentes : l'invite les dessine
        // en français (Échap, Suppr, Ctrl…). On garde les icônes à symbole (Espace, Entrée, Tab, Maj, flèches).
        static readonly string[,] s_KeyboardMouse =
        {
            { "space", "keyboard_space_icon" }, { "enter", "keyboard_return" }, { "numpadenter", "keyboard_return" },
            { "tab", "keyboard_tab_icon" }, { "leftshift", "keyboard_shift_icon" }, { "rightshift", "keyboard_shift_icon" },
            { "leftmeta", "keyboard_win" }, { "rightmeta", "keyboard_win" }, { "backspace", "keyboard_backspace_icon" },
            { "uparrow", "keyboard_arrow_up" }, { "downarrow", "keyboard_arrow_down" },
            { "leftarrow", "keyboard_arrow_left" }, { "rightarrow", "keyboard_arrow_right" }, { "arrows", "keyboard_arrows_all" },
            { "capslock", "keyboard_capslock_icon" },
            { "minus", "keyboard_minus" }, { "equals", "keyboard_equals" }, { "comma", "keyboard_comma" },
            { "period", "keyboard_period" }, { "slash", "keyboard_slash_forward" }, { "backslash", "keyboard_slash_back" },
            { "semicolon", "keyboard_semicolon" }, { "quote", "keyboard_apostrophe" }, { "backquote", "keyboard_tilde" },
            { "leftbracket", "keyboard_bracket_open" }, { "rightbracket", "keyboard_bracket_close" },
            { "numpadplus", "keyboard_numpad_plus" }, { "numpadmultiply", "keyboard_asterisk" },
            { "leftbutton", "mouse_left" }, { "rightbutton", "mouse_right" }, { "middlebutton", "mouse_scroll" },
            { "delta", "mouse_move" }, { "position", "mouse" }, { "scroll", "mouse_scroll_vertical" },
            { "scroll/up", "mouse_scroll_up" }, { "scroll/down", "mouse_scroll_down" },
            { "forwardbutton", "mouse_side_forward" }, { "backbutton", "mouse_side_back" },
        };

        [MenuItem("Deathless/UI/3. Table des icônes (InputGlyphs)")]
        public static string CreateGlyphs()
        {
            EnsureFolder(Path.GetDirectoryName(GlyphsPath).Replace('\\', '/'));
            var glyphs = AssetDatabase.LoadAssetAtPath<InputGlyphs>(GlyphsPath);
            if (glyphs == null)
            {
                glyphs = ScriptableObject.CreateInstance<InputGlyphs>();
                AssetDatabase.CreateAsset(glyphs, GlyphsPath);
            }

            var missing = new List<string>();
            glyphs.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            glyphs.xbox = Table("Xbox Series", s_Xbox, missing);
            glyphs.playStation = Table("PlayStation Series", s_PlayStation, missing);
            var km = Table("Keyboard & Mouse", s_KeyboardMouse, missing);
            // Lettres (clé = lettre affichée par la disposition active) et chiffres, F1-F12 (clé = chemin).
            for (var c = 'a'; c <= 'z'; c++) AddGlyph(km, "Keyboard & Mouse", c.ToString(), "keyboard_" + c, missing);
            for (var d = 0; d <= 9; d++) AddGlyph(km, "Keyboard & Mouse", d.ToString(), "keyboard_" + d, missing);
            for (var f = 1; f <= 12; f++) AddGlyph(km, "Keyboard & Mouse", "f" + f, "keyboard_f" + f, missing);
            glyphs.keyboardMouse = km;

            glyphs.xboxDevice = Icon("Xbox Series", "controller_xboxseries");
            glyphs.playStationDevice = Icon("PlayStation Series", "controller_playstation5");
            glyphs.keyboardMouseDevice = Icon("Keyboard & Mouse", "keyboard");
            EditorUtility.SetDirty(glyphs);
            AssetDatabase.SaveAssets();
            var report = $"InputGlyphs : Xbox {glyphs.xbox.Count}, PlayStation {glyphs.playStation.Count}, clavier-souris {glyphs.keyboardMouse.Count}"
                         + (missing.Count > 0 ? "\nIcônes introuvables : " + string.Join(", ", missing) : "");
            Debug.Log("[DeathlessUISetup] " + report);
            return report;
        }

        static List<InputGlyphs.Glyph> Table(string family, string[,] map, List<string> missing)
        {
            var list = new List<InputGlyphs.Glyph>();
            for (var i = 0; i < map.GetLength(0); i++) AddGlyph(list, family, map[i, 0], map[i, 1], missing);
            return list;
        }

        static void AddGlyph(List<InputGlyphs.Glyph> list, string family, string control, string file, List<string> missing)
        {
            var icon = Icon(family, file);
            if (icon == null)
            {
                missing.Add(family + "/" + file);
                return;
            }
            list.Add(new InputGlyphs.Glyph { control = control, icon = icon });
        }

        static Texture2D Icon(string family, string file) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{KenneyRoot}/{family}/Double/{file}.png");

        // ---------------------------------------------------------------- Scène de démonstration

        [MenuItem("Deathless/UI/4. Scène de démonstration UISocle")]
        public static void CreateDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x16, 0x1a, 0x24, 0xff);
            camera.transform.position = new Vector3(0f, 1f, -10f);

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = eventSystemGo.GetComponent<InputSystemUIInputModule>();
            AssignUIModule(module, actions);

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            var uiGo = new GameObject("UI");
            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DemoUxmlPath);
            var scale = uiGo.AddComponent<UIScale>();
            scale.panels = new[] { panel };
            var demo = uiGo.AddComponent<UISocleDemo>();
            demo.actions = actions;

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, DemoScenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != DemoScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(DemoScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        const string V01ScenePath = "Assets/Scenes/UIv01.unity";

        [MenuItem("Deathless/UI/5. Scène des écrans 0.1 (UIv01)")]
        public static void CreateV01Scene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x16, 0x1a, 0x24, 0xff);

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            AssignUIModule(eventSystemGo.GetComponent<InputSystemUIInputModule>(), actions);

            // Données factices (le vrai jeu les remplace dans main).
            var donnees = new GameObject("Donnees (factices)");
            donnees.AddComponent<Deathless.UI.Dev.EtatFactice>().actions = actions;

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            var uiGo = new GameObject("UI");
            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/UIv01/UIv01.uxml");
            uiGo.AddComponent<UIScale>().panels = new[] { panel };
            var nav = uiGo.AddComponent<Deathless.UI.Ecrans.NavigateurEcrans>();
            nav.actions = actions;
            VisualTreeAsset Uxml(string nom) => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/UI/Screens/{nom}/{nom}.uxml");
            nav.menuPrincipal = Uxml("MenuPrincipal");
            nav.options = Uxml("Options");
            nav.credits = Uxml("Credits");
            nav.hud = Uxml("Hud");
            nav.pause = Uxml("Pause");
            nav.score = Uxml("Score");
            nav.choixClasse = Uxml("ChoixClasse");
            nav.saisie = Uxml("Saisie");
            nav.lobby = Uxml("Lobby");
            nav.achat = Uxml("Achat");
            nav.personnage = Uxml("Personnage");
            nav.roueEmotes = Uxml("RoueEmotes");
            uiGo.AddComponent<Deathless.UI.Dev.DemoV01>();

            EditorSceneManager.SaveScene(scene, V01ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(sc => sc.path != V01ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(V01ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        /// Branche le module d'entrée UI sur la carte UI de DeathlessControls (références sous-assets de l'importeur).
        public static void AssignUIModule(InputSystemUIInputModule module, InputActionAsset actions)
        {
            var refs = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<InputActionReference>().ToList();
            InputActionReference R(string name) =>
                refs.FirstOrDefault(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == name);

            module.actionsAsset = actions;
            module.point = R("Point");
            module.leftClick = R("Click");
            module.rightClick = R("RightClick");
            module.middleClick = R("MiddleClick");
            module.scrollWheel = R("ScrollWheel");
            module.move = R("Navigate");
            module.submit = R("Submit");
            module.cancel = R("Cancel");
            module.trackedDevicePosition = null;
            module.trackedDeviceOrientation = null;
            EditorUtility.SetDirty(module);
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
