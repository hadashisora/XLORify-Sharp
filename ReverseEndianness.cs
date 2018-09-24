using System;

namespace XLORify
{
    //Flips endianness the crappiest way possible and allows me to use BinaryReader/Writer on Big Endian files without directly modifying or rewriting it
    public class ReverseEndianness
    {
        public static UInt16 UInt16_ReverseEndianness(UInt16 input)
        {
            var thing = BitConverter.GetBytes(input);
            Array.Reverse(thing);
            var thing2 = BitConverter.ToUInt16(thing, 0);
            return thing2;
        }

        public static UInt32 UInt32_ReverseEndianness(UInt32 input)
        {
            var thing = BitConverter.GetBytes(input);
            Array.Reverse(thing);
            var thing2 = BitConverter.ToUInt32(thing, 0);
            return thing2;
        }

        public static UInt64 UInt64_ReverseEndianness(UInt64 input)
        {
            var thing = BitConverter.GetBytes(input);
            Array.Reverse(thing);
            var thing2 = BitConverter.ToUInt64(thing, 0);
            return thing2;
        }

        public static Int16 Int16_ReverseEndianness(Int16 input)
        {
            var thing = BitConverter.GetBytes(input);
            Array.Reverse(thing);
            var thing2 = BitConverter.ToInt16(thing, 0);
            return thing2;
        }

        public static Int32 Int32_ReverseEndianness(Int32 input)
        {
            var thing = BitConverter.GetBytes(input);
            Array.Reverse(thing);
            var thing2 = BitConverter.ToInt32(thing, 0);
            return thing2;
        }

        public static Int64 Int64_ReverseEndianness(Int64 input)
        {
            var thing = BitConverter.GetBytes(input);
            Array.Reverse(thing);
            var thing2 = BitConverter.ToInt64(thing, 0);
            return thing2;
        }
    }
}
