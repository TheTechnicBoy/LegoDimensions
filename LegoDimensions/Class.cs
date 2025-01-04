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
        public FlashProperties() { }

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
        public FadeProperties() { }
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
        public byte Red { get; private set; }
        public byte Green { get; private set; }
        public byte Blue { get; private set; }

        public Color(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }
        //To DO Enum Colors

        public String toString()
        {
            return "Red: " + Red + " Green: " + Green + " Blue: " + Blue;
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

        public byte[] UUID { get; internal set; }
    }
}
