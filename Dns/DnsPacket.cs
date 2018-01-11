using System;
using System.Linq;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsPacket {
        public ushort Identification;
        public bool QueryResponse;
        public DnsOpcode Opcode;
        public bool AuthoritativeAnswer;
        public bool Truncated;
        public bool RecursionDesired;
        public bool RecursionAvailable;
        public bool AuthenticatedData;
        public bool CheckingDisabled;
        public DnsReturnCode ReturnCode;
        public ushort TotalQuestions {
            get {
                return (ushort) Questions.Length;
            }
            set {
                Questions = new DnsQuestion[value];
            }
        }
        public ushort TotalAnswers {
            get {
                return (ushort) Answers.Length;
            }
            set {
                Answers = new DnsResourceRecord[value];
            }
        }
        public ushort TotalAuthorities {
            get {
                return (ushort) Authorities.Length;
            }
            set {
                Authorities = new DnsResourceRecord[value];
            }
        }
        public ushort TotalAdditionalResources {
            get {
                return (ushort) AdditionalResources.Length;
            }
            set {
                AdditionalResources = new DnsResourceRecord[value];
            }
        }
        public DnsQuestion[] Questions;
        public DnsResourceRecord[] Answers;
        public DnsResourceRecord[] Authorities;
        public DnsResourceRecord[] AdditionalResources;
        static Random Random = new Random();

        public byte[] ToByteArray() {
            byte[] header = new byte[12];
            int index = 0;
            DnsUtil.GetBytes(header, ref index, Identification);
            header[index++] = (byte) ((QueryResponse ? 1 << 7 : 0)
                                    | (((int) Opcode) << 3)
                                    | (AuthoritativeAnswer ? 1 << 2 : 0)
                                    | (Truncated ? 1 << 1 : 0)
                                    | (RecursionDesired ? 1 : 0));
            header[index++] = (byte) ((RecursionAvailable ? 1 << 7 : 0)
                                    | (AuthenticatedData ? 1 << 5 : 0)
                                    | (CheckingDisabled ? 1 << 4 : 0)
                                    | ((int) ReturnCode));
            DnsUtil.GetBytes(header, ref index, TotalQuestions);
            DnsUtil.GetBytes(header, ref index, TotalAnswers);
            DnsUtil.GetBytes(header, ref index, TotalAuthorities);
            DnsUtil.GetBytes(header, ref index, TotalAdditionalResources);
            return header.Concat(Questions          .SelectMany(o => o.ToByteArray()))
                         .Concat(Answers            .SelectMany(o => o.ToByteArray()))
                         .Concat(Authorities        .SelectMany(o => o.ToByteArray()))
                         .Concat(AdditionalResources.SelectMany(o => o.ToByteArray()))
                         .ToArray();
        }

        public DnsPacket() {
            Identification = (ushort) Random.Next();
            Questions = new DnsQuestion[0];
            Answers = new DnsResourceRecord[0];
            Authorities = new DnsResourceRecord[0];
            AdditionalResources = new DnsResourceRecord[0];
        }

        public DnsPacket(byte[] buffer) {
            int index = 0;
            Identification = DnsUtil.ToUInt16(buffer, ref index);
            QueryResponse = (buffer[index] & (1 << 7)) != 0;
            Opcode = (DnsOpcode) ((buffer[index] >> 3) & 0b1111);
            AuthoritativeAnswer = (buffer[index] & (1 << 2)) != 0;
            Truncated = (buffer[index] & (1 << 1)) != 0;
            RecursionDesired = (buffer[index++] & 1) != 0;
            RecursionAvailable = (buffer[index] & (1 << 7)) != 0;
            AuthenticatedData = (buffer[index] & (1 << 5)) != 0;
            CheckingDisabled = (buffer[index] & (1 << 4)) != 0;
            ReturnCode = (DnsReturnCode) (buffer[index++] & 0b1111);
            TotalQuestions = DnsUtil.ToUInt16(buffer, ref index);
            TotalAnswers = DnsUtil.ToUInt16(buffer, ref index);
            TotalAuthorities = DnsUtil.ToUInt16(buffer, ref index);
            TotalAdditionalResources = DnsUtil.ToUInt16(buffer, ref index);
            for (int i = 0; i < TotalQuestions; ++i) {
                Questions[i] = new DnsQuestion(buffer, ref index);
            }
            for (int i = 0; i < TotalAnswers; ++i) {
                Answers[i] = new DnsResourceRecord(buffer, ref index);
            }
            for (int i = 0; i < TotalAuthorities; ++i) {
                Authorities[i] = new DnsResourceRecord(buffer, ref index);
            }
            for (int i = 0; i < TotalAdditionalResources; ++i) {
                AdditionalResources[i] = new DnsResourceRecord(buffer, ref index);
            }
        }
    }
}
