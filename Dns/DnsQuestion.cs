using System;
using System.Linq;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsQuestion : IComparable<DnsQuestion>, IEquatable<DnsQuestion> {
        public string QueryName;
        public DnsRecordType Type;
        public DnsClass Class;

        public byte[] ToByteArray() {
            byte[] data = new byte[QueryName.Length + 2 + 4];
            DnsUtil.EncodeQuestionName(QueryName, data, 0);
            int index = QueryName.Length + 2;
            DnsUtil.GetBytes(data, ref index, (ushort) Type);
            DnsUtil.GetBytes(data, ref index, (ushort) Class);
            return data;
        }

        public int CompareTo(DnsQuestion other) {
            int diff = QueryName.CompareTo(other.QueryName);
            if (diff == 0) {
                diff = Type.CompareTo(other.Type);
                if (diff == 0) {
                    diff = Class.CompareTo(other.Class);
                }
            }
            return diff;
        }

        public override int GetHashCode() {
            return new Tuple<string, DnsRecordType, DnsClass>(QueryName, Type, Class).GetHashCode();
        }

        public bool Equals(DnsQuestion other) {
            return QueryName == other.QueryName && Type == other.Type && Class == other.Class;
        }

        public override bool Equals(object obj) {
            if (obj is DnsQuestion) {
                return Equals((DnsQuestion) obj);
            } else {
                return false;
            }
        }

        public DnsQuestion() {
        }

        public DnsQuestion(byte[] buffer, ref int index) {
            QueryName = DnsUtil.DecodeQuestionName(buffer, ref index);
            Type = (DnsRecordType) DnsUtil.ToUInt16(buffer, ref index);
            Class = (DnsClass) DnsUtil.ToUInt16(buffer, ref index);
        }
    }
}
