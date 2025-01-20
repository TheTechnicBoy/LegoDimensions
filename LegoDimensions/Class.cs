using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LegoDimensions
{
    public enum Pad
    {
        All = 0,
        Center = 1,
        Left = 2,
        Right = 3,
    }

    public class FlashProperties
    {
        public byte OnLen { get; set; }
        public byte OffLen { get; set; }
        public byte PulseCnt { get; set; }
        public Color Color { get; set; }

        public FlashProperties(Color color, byte onLen, byte offLen, byte pulseCnt)
        {
            Color = color;
            OnLen = onLen;
            OffLen = offLen;
            PulseCnt = pulseCnt;
        }
        public FlashProperties() {
            Color = new Color(0, 0, 0);
         }

    }

    public class FadeProperties
    {
        public byte FadeLen { get; set; }
        public byte PulseCnt { get; set; }
        public Color Color { get; set; }

        public FadeProperties(Color color, byte fadeLen, byte pulseCnt)
        {
            Color = color;
            FadeLen = fadeLen;
            PulseCnt = pulseCnt;
        }
        public FadeProperties() { 
            Color = new Color(0, 0, 0);
        }
    }

    public class RandomFadeProperties
    {
        public byte FadeLen { get; set; }
        public byte PulseCnt { get; set; }

        public RandomFadeProperties(byte fadeLen, byte pulseCnt)
        {
            FadeLen = fadeLen;
            PulseCnt = pulseCnt;
        }
        public RandomFadeProperties() { }
    }

    public class Color
    {
        public byte red { get; private set; }
        public byte green { get; private set; }
        public byte blue { get; private set; }

        public Color(byte red, byte green, byte blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }
        //To DO Enum Colors

        public override String ToString()
        {
            return "Red: " + red + " Green: " + green + " Blue: " + blue;
        }

        public static readonly Color Red = Color.fromHex("#FF0000");
        public static readonly Color Green = Color.fromHex("#00FF00");
        public static readonly Color Blue = Color.fromHex("#0000FF");

        public static Color fromHex(String hex){
            if(hex.StartsWith("#")){
                hex = hex.Substring(1);
            }
            if(hex.Length != 6){
                throw new System.ArgumentException("Hex color must be 6 characters long", "hex");
            }
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 4), 16);
            byte b = Convert.ToByte(hex.Substring(4, 6), 16);
            return new Color(r, g, b);
        }

        public static Color FromConsoleColor(ConsoleColor consoleColor)
        {
            switch (consoleColor)
            {
                case ConsoleColor.Black: return new Color(0, 0, 0);
                case ConsoleColor.DarkBlue: return new Color(0, 0, 139);
                case ConsoleColor.DarkGreen: return new Color(0, 100, 0);
                case ConsoleColor.DarkCyan: return new Color(0, 139, 139);
                case ConsoleColor.DarkRed: return new Color(139, 0, 0);
                case ConsoleColor.DarkMagenta: return new Color(139, 0, 139);
                case ConsoleColor.DarkYellow: return new Color(139, 139, 0);
                case ConsoleColor.Gray: return new Color(169, 169, 169);
                case ConsoleColor.DarkGray: return new Color(105, 105, 105);
                case ConsoleColor.Blue: return new Color(0, 0, 255);
                case ConsoleColor.Green: return new Color(0, 255, 0);
                case ConsoleColor.Cyan: return new Color(0, 255, 255);
                case ConsoleColor.Red: return new Color(255, 0, 0);
                case ConsoleColor.Magenta: return new Color(255, 0, 255);
                case ConsoleColor.Yellow: return new Color(255, 255, 0);
                case ConsoleColor.White: return new Color(255, 255, 255);
                default: throw new ArgumentOutOfRangeException(nameof(consoleColor), consoleColor, null);
            }
        }

        
    }

    internal enum MessageCommand
    {
        /// <summary>No command, this is used when receiveing a message from the portal.</summary>
        None = 0x00,


        // B = general
        /// <summary>Wake up command.</summary>
        Wake = 0xB0,

        /// <summary>Generate a seed.</summary>
        Seed = 0xB1,

        /// <summary>Challenge.</summary>
        Challenge = 0xB3,

        /// <summary>Unknown command.</summary>
        Unkonwn0xB4 = 0xB4,


        // C = colors
        /// <summary>Change color immediatly.</summary>
        Color = 0xC0,

        /// <summary>Get a color.</summary>
        GetColor = 0xC1,

        /// <summary>Fade colors.</summary>
        Fade = 0xC2,

        /// <summary>Flash colors.</summary>
        Flash = 0xC3,

        /// <summary>Fade to a random color.</summary>
        FadeRandom = 0xC4,

        /// <summary>Fade unkwon?</summary>
        FadeUnknown = 0xC5,

        /// <summary>Fade all.</summary>
        FadeAll = 0xC6,

        /// <summary>Flash all.</summary>
        FlashAll = 0xC7,

        /// <summary>Color all pads.</summary>
        ColorAll = 0xC8,


        // D = tags
        /// <summary>List tags</summary>
        TagList = 0xD0,

        /// <summary>Read 16 bytes on a specific page.</summary>
        Read = 0xD2,

        /// <summary>Write 16 bytes on a specific page.</summary>
        Write = 0xD3,

        /// <summary></summary>
        Model = 0xD4,


        // E = configuration
        /// <summary>Set password behavior.</summary>
        ConfigPassword = 0xE1,

        /// <summary>Active or deactivate the NFC module.</summary>
        ConfigActive = 0xE5,
    }

    public class PortalTagEventArgs
    {
        public Pad Pad { get; internal set; }
        public bool Placed { get; internal set; }

        /// <summary>
        /// The ID of the Tag on the Portal, the first has 0, second 2, [...]
        /// </summary>
        public int ID { get; internal set; }

        public byte[] UUID { get; internal set; } = {0x00};
    }

    /// <summary>
    /// The password type for the portal NFC card authentication.
    /// </summary>
    public enum PortalPassword
    {
        /// <summary>
        /// Disabled.
        /// </summary>
        Disable = 0,

        /// <summary>
        /// Automatic, this is the default.
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// Custom password.
        /// </summary>
        Custom = 2,
    }
}
