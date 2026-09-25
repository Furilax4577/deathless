using System;
using System.Runtime.InteropServices;

namespace DeathlessLauncher
{
    // Manette Xbox par XInput (P/Invoke, rien à installer) : xinput1_4.dll (Windows 8 et plus), repli sur
    // xinput9_1_0.dll. Sondée par la fenêtre (Lire, environ 30 fois par seconde) ; les manettes absentes ne sont
    // réinterrogées que toutes les 2 secondes, car XInputGetState est lent sur un emplacement vide.
    // Les manettes PlayStation ne passent pas par XInput : elles ne sont pas gérées (clavier et souris restent possibles).
    public sealed class Manette
    {
        public const ushort DpadHaut = 0x0001, DpadBas = 0x0002, DpadGauche = 0x0004, DpadDroite = 0x0008;
        public const ushort Start = 0x0010, Back = 0x0020, LB = 0x0100, RB = 0x0200;
        public const ushort A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        struct XInputGamepad
        {
            public ushort Buttons;
            public byte LeftTrigger, RightTrigger;
            public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct XInputState
        {
            public uint PacketNumber;
            public XInputGamepad Gamepad;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        static extern uint GetState14(uint index, out XInputState state);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        static extern uint GetState910(uint index, out XInputState state);

        const uint Ok = 0;
        int dll;                 // 0 : xinput1_4, 1 : xinput9_1_0, 2 : aucune
        readonly bool[] present = new bool[4];
        readonly XInputState[] precedent = new XInputState[4];
        readonly DateTime[] prochainEssai = new DateTime[4];

        /// Boutons enfoncés à cet instant (toutes manettes réunies).
        public ushort Boutons { get; private set; }
        /// Boutons qui viennent d'être enfoncés depuis la lecture précédente.
        public ushort Appuis { get; private set; }
        /// Stick gauche et droit, vertical, de -1 (bas) à 1 (haut), zone morte retirée.
        public double StickGaucheY { get; private set; }
        public double StickDroitY { get; private set; }
        /// Vrai si la manette a été utilisée pendant cette lecture (bouton, ou stick poussé franchement).
        public bool Activite { get; private set; }
        public bool Disponible => dll < 2;
        public bool Connectee { get { foreach (bool p in present) if (p) return true; return false; } }

        public void Lire()
        {
            ushort boutons = 0, appuis = 0;
            double lg = 0, rd = 0;
            bool activite = false;
            DateTime now = DateTime.UtcNow;
            for (uint i = 0; i < 4 && dll < 2; i++)
            {
                if (!present[i] && now < prochainEssai[i]) continue;
                if (!Etat(i, out XInputState s))
                {
                    present[i] = false;
                    prochainEssai[i] = now.AddSeconds(2);
                    continue;
                }
                XInputState avant = precedent[i];
                bool etaitLa = present[i];
                present[i] = true;
                precedent[i] = s;
                ushort b = s.Gamepad.Buttons;
                boutons |= b;
                if (etaitLa)
                {
                    appuis |= (ushort)(b & ~avant.Gamepad.Buttons);
                    if (b != avant.Gamepad.Buttons) activite = true;
                    // 0,35 comme InputDeviceWatcher du jeu : pas de bascule sur la dérive d'un stick.
                    if (Math.Abs(s.Gamepad.ThumbLY - avant.Gamepad.ThumbLY) > 0.35 * 32767 || Math.Abs(s.Gamepad.ThumbRY - avant.Gamepad.ThumbRY) > 0.35 * 32767
                        || Math.Abs(s.Gamepad.ThumbLX - avant.Gamepad.ThumbLX) > 0.35 * 32767 || Math.Abs(s.Gamepad.ThumbRX - avant.Gamepad.ThumbRX) > 0.35 * 32767)
                        activite = true;
                }
                double y = Axe(s.Gamepad.ThumbLY);
                if (Math.Abs(y) > Math.Abs(lg)) lg = y;
                y = Axe(s.Gamepad.ThumbRY);
                if (Math.Abs(y) > Math.Abs(rd)) rd = y;
            }
            Boutons = boutons;
            Appuis = appuis;
            StickGaucheY = lg;
            StickDroitY = rd;
            Activite = activite;
        }

        /// Oublie les appuis en cours (quand la fenêtre reprend la main, un bouton déjà tenu ne compte pas).
        public void Oublier()
        {
            Appuis = 0;
            for (uint i = 0; i < 4; i++)
                if (present[i] && Etat(i, out XInputState s)) precedent[i] = s;
        }

        static double Axe(short v)
        {
            const double zoneMorte = 7849; // XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE
            double a = Math.Abs((double)v);
            if (a < zoneMorte) return 0;
            return Math.Sign(v) * Math.Min(1, (a - zoneMorte) / (32767 - zoneMorte));
        }

        bool Etat(uint index, out XInputState s)
        {
            s = default;
            while (dll < 2)
            {
                try
                {
                    return (dll == 0 ? GetState14(index, out s) : GetState910(index, out s)) == Ok;
                }
                catch (DllNotFoundException) { dll++; }
                catch (EntryPointNotFoundException) { dll++; }
            }
            return false;
        }
    }
}
