using System;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsResourceRecord {
        public string Name;
        public DnsRecordType Type;
        public DnsClass Class;
        public uint TimeToLive;
        public ushort DataLength {
            get {
                return (ushort) Data.Length;
            }
            set {
                Data = new byte[value];
            }
        }
        public byte[] Data;

        public byte[] ToByteArray() {
            int index = Name.Length + 2;
            byte[] data = new byte[index + 10 + DataLength];
            DnsUtil.EncodeQuestionName(Name, data, 0);
            DnsUtil.GetBytes(data, ref index, (ushort) Type);
            DnsUtil.GetBytes(data, ref index, (ushort) Class);
            DnsUtil.GetBytes(data, ref index, TimeToLive);
            DnsUtil.GetBytes(data, ref index, DataLength);
            Data.CopyTo(data, index);
            return data;
        }

        public DnsResourceRecord() {
        }


        public DnsResourceRecord(byte[] buffer, ref int index) {
            Name = DnsUtil.DecodeQuestionName(buffer, ref index);
            Type = (DnsRecordType) DnsUtil.ToUInt16(buffer, ref index);
            Class = (DnsClass) DnsUtil.ToUInt16(buffer, ref index);
            TimeToLive = DnsUtil.ToUInt32(buffer, ref index);
            DataLength = DnsUtil.ToUInt16(buffer, ref index);
            Array.Copy(buffer, index, Data, 0, DataLength);
            index += DataLength;
        }
    }
}
