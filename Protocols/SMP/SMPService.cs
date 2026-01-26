using PeterO.Cbor;
using ProtocolWorkBench.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using ProtocolWorkBench.Core.Protocols.SMP.Models;

namespace ProtocolWorkBench.Core.Protocols.SMP
{
    public static class SMPService
    {
        public const int SMP_HEADER_LENGTH = 8;

        private static byte SmpSequenceNumber = 1;

        public static SmpHeader GetSmpHeaderFromDataLittleEndian(List<byte> data)
        {
            SmpHeader smpHeader = new SmpHeader();
            smpHeader.MgmtOp = data[0];
            smpHeader.Flags = data[1];
            smpHeader.PayloadLength.Lb = data[2];
            smpHeader.PayloadLength.Hb = data[3];
            smpHeader.GroupId.Lb = data[4];
            smpHeader.GroupId.Hb = data[5];
            smpHeader.SequenceNumber = data[6];
            smpHeader.MessageId = data[7];

            return smpHeader;
        }

        public static SmpHeader GetSmpHeaderFromDataBigEndian(List<byte> data)
        {
            SmpHeader smpHeader = new SmpHeader();
            smpHeader.MgmtOp = data[0];
            smpHeader.Flags = data[1];
            smpHeader.PayloadLength.Lb = data[3];
            smpHeader.PayloadLength.Hb = data[2];
            smpHeader.GroupId.Lb = data[5];
            smpHeader.GroupId.Hb = data[4];
            smpHeader.SequenceNumber = data[6];
            smpHeader.MessageId = data[7];

            return smpHeader;
        }

        public static List<byte> SmpHeaderToByteListBigEndian(SmpHeader header)
        {
            List<byte> headerLB = new List<byte>();
            headerLB.Add(header.MgmtOp);
            headerLB.Add(header.Flags);
            headerLB.Add(header.PayloadLength.Hb);
            headerLB.Add(header.PayloadLength.Lb);
            headerLB.Add(header.GroupId.Hb);
            headerLB.Add(header.GroupId.Lb);
            headerLB.Add(header.SequenceNumber);
            headerLB.Add(header.MessageId);
            return headerLB;
        }

        public static void SetSequenceNumber(byte seqNum)
        {
            SmpSequenceNumber = seqNum;
        }

        private static byte GetSequenceNumber()
        {
            byte seqNum = SmpSequenceNumber;
            SmpSequenceNumber++;
            if (SmpSequenceNumber == 0)
            {
                SmpSequenceNumber = 1;
            }

            return seqNum;
        }

        public static SmpHeader FormatSmpHeader(CBORObject cBORObject, byte mgmtOp, byte commandId, UInt16HbLb groupId)
        {
            SmpHeader smpHeader = new SmpHeader();
            smpHeader.MgmtOp = mgmtOp;
            smpHeader.MessageId = commandId;
            smpHeader.SequenceNumber = GetSequenceNumber();
            smpHeader.GroupId.U16Value = groupId.U16Value;
            if (cBORObject != null)
            {
                UInt16 cboreLength = (UInt16)cBORObject.CalcEncodedSize();
                smpHeader.PayloadLength.U16Value += (UInt16)cboreLength;
            }
            return smpHeader;
        }

        /// <summary>
        /// Create a SMP Message from CBOR and supplied SMP Header
        /// </summary>
        /// <param name="cborObj"></param>
        /// <param name="smpHeader"></param>
        /// <returns></returns>
        public static List<byte> CreateSmpMessage(CBORObject cborObj, SmpHeader smpHeader)
        {
            List<byte> smpHeaderLB = SmpHeaderToByteListBigEndian(smpHeader);
            List<byte> cborMsg = cborObj.EncodeToBytes().ToList();
            cborMsg.InsertRange(0, smpHeaderLB);
            return cborMsg;
        }

        public static SmpMessage ConvertRawBytesToSmpMessage(List<byte> message)
        {
            SmpMessage smpMessage = new SmpMessage();

            // get the header
            smpMessage.Header = SMPService.GetSmpHeaderFromDataLittleEndian(message);

            // add cbor part
            smpMessage.CBorMessage.AddRange(message.GetRange(SMPService.SMP_HEADER_LENGTH, message.Count - SMPService.SMP_HEADER_LENGTH));

            return smpMessage;
        }

        private static void AddKvP(MessageParameter inMessageParameter, CBORObject outMap)
        {
            switch (inMessageParameter.CType)
            {
                case (CTypes.UINT8):
                    outMap.Add(inMessageParameter.Name, byte.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.UINT16):
                    outMap.Add(inMessageParameter.Name, UInt16.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.UINT32):
                    outMap.Add(inMessageParameter.Name, UInt32.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.UINT64):
                    outMap.Add(inMessageParameter.Name, UInt64.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.INT8):
                    outMap.Add(inMessageParameter.Name, Int16.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.INT16):
                    outMap.Add(inMessageParameter.Name, Int16.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.INT32):
                    outMap.Add(inMessageParameter.Name, Int32.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.INT64):
                    outMap.Add(inMessageParameter.Name, Int64.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.FLOAT):
                    float outFloat = float.Parse(inMessageParameter.Value, CultureInfo.InvariantCulture.NumberFormat);
                    outFloat = (float)Math.Round(outFloat * 100.0f) / 100.0f;
                    outMap.Add(inMessageParameter.Name, outFloat);
                    break;
                case (CTypes.BOOL):
                    outMap.Add(inMessageParameter.Name, bool.Parse(inMessageParameter.Value));
                    break;
                case (CTypes.VOID):
                    throw new IndexOutOfRangeException();
                case (CTypes.STRING):
                    outMap.Add(inMessageParameter.Name, inMessageParameter.Value);
                    break;
                case (CTypes.BYTE_ARRAY):
                    byte[] byteArray = Encoding.ASCII.GetBytes(inMessageParameter.Value);
                    outMap.Add(inMessageParameter.Name, byteArray);
                    break;
            }
        }

    }
}
