using System;
using System.Collections.Generic;
using System.Text;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public static class DnsUtil {
        public static void EncodeQuestionName(string questionName, byte[] buffer, int index) {
            string[] parts = questionName.Split('.');
            foreach (string part in parts) {
                buffer[index++] = (byte) part.Length;
                Encoding.ASCII.GetBytes(part).CopyTo(buffer, index);
                index += part.Length;
            }
            buffer[index] = 0;
        }

        static IEnumerable<string> DecodeQuestionNameParts(byte[] buffer, ref int index) {
            List<string> parts = new List<string>();
            while (buffer[index] != 0) {
                if ((buffer[index] & 0b11000000) == 0b11000000) {
                    int ptr = DnsUtil.ToUInt16(buffer, ref index);
                    ptr ^= 0b1100000000000000;
                    parts.AddRange(DecodeQuestionNameParts(buffer, ref ptr));
                    return parts;
                } else {
                    parts.Add(Encoding.ASCII.GetString(buffer, index + 1, buffer[index]));
                    index += buffer[index] + 1;
                }
            }
            ++index;
            return parts;
        }

        public static string DecodeQuestionName(byte[] buffer, ref int index) {
            return string.Join(".", DecodeQuestionNameParts(buffer, ref index));
        }

        public static void FixEndianness(byte[] buffer, int index, int length) {
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(buffer, index, length);
            }
        }

        public static ushort ToUInt16(byte[] buffer, ref int index) {
            FixEndianness(buffer, index, 2);
            ushort val = BitConverter.ToUInt16(buffer, index);
            index += 2;
            return val;
        }

        public static uint ToUInt32(byte[] buffer, ref int index) {
            FixEndianness(buffer, index, 4);
            uint val = BitConverter.ToUInt32(buffer, index);
            index += 4;
            return val;
        }

        public static void GetBytes(byte[] buffer, ref int index, ushort val) {
            BitConverter.GetBytes(val).CopyTo(buffer, index);
            FixEndianness(buffer, index, 2);
            index += 2;
        }

        public static void GetBytes(byte[] buffer, ref int index, uint val) {
            BitConverter.GetBytes(val).CopyTo(buffer, index);
            FixEndianness(buffer, index, 4);
            index += 4;
        }
    }
}
