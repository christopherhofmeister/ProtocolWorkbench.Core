using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.Binary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolWorkBench.Core.Protocols.Binary
{
    public static class BinaryService
    {
        public static List<Byte> CreateBinaryBMessageCRC(int id, List<MessageParameter> msgParams)
        {
            BinaryProtocolB binary = new BinaryProtocolB();
            binary.StartOfFrame = 1;
            binary.EndOfFrame = 4;
            binary.MesssageType = (byte)id;
            foreach (MessageParameter msgParam in msgParams)
            {
                binary.Payload.AddRange(ParamaterToListByteLSBFirst(msgParam));
            }
            UInt16HbLb length = new UInt16HbLb();
            length.U16Value = (ushort)(binary.Payload.Count + 7);
            binary.Length.Lb = length.Lb;
            binary.Length.Hb = length.Hb;

            List<Byte> msg = BinaryProtocolBToListByte(binary);

            UInt16HbLb crc = CRCService.CalculateCRCCitt16(msg, 3);
            binary.CRC.Lb = crc.Lb;
            binary.CRC.Hb = crc.Hb;

            return BinaryProtocolBToListByte(binary);
        }

        private static List<Byte> ParamaterToListByteLSBFirst(MessageParameter param)
        {
            List<Byte> formattedPayload = new List<byte>();

            if (param.CType == CTypes.BOOL)
            {
                if (param.Value.ToLower() == "true")
                {
                    formattedPayload.Add(0x01);
                }
                else
                {
                    formattedPayload.Add(0x00);
                }

            }
            else if (param.CType == CTypes.UINT8)
            {
                formattedPayload.Add(Convert.ToByte(param.Value));
            }
            else if (param.CType == CTypes.UINT16)
            {
                UInt16 u16 = Convert.ToUInt16(param.Value);
                formattedPayload.Add((byte)u16);
                formattedPayload.Add((byte)(u16 >> 8));
            }
            else if (param.CType == CTypes.UINT32)
            {
                UInt32 u32 = Convert.ToUInt32(param.Value);
                formattedPayload.Add((byte)u32);
                formattedPayload.Add((byte)(u32 >> 8));
                formattedPayload.Add((byte)(u32 >> 16));
                formattedPayload.Add((byte)(u32 >> 24));
            }
            else if (param.CType == CTypes.UINT64)
            {
                UInt64 u64 = Convert.ToUInt64(param.Value);
                formattedPayload.Add((byte)u64);
                formattedPayload.Add((byte)(u64 >> 8));
                formattedPayload.Add((byte)(u64 >> 16));
                formattedPayload.Add((byte)(u64 >> 24));
                formattedPayload.Add((byte)(u64 >> 32));
                formattedPayload.Add((byte)(u64 >> 40));
                formattedPayload.Add((byte)(u64 >> 48));
                formattedPayload.Add((byte)(u64 >> 56));
            }
            else if (param.CType == CTypes.INT8)
            {
                formattedPayload.Add(Convert.ToByte(param.Value));
            }
            else if (param.CType == CTypes.INT16)
            {
                Int16 i16 = Convert.ToInt16(param.Value);
                formattedPayload.Add((byte)i16);
                formattedPayload.Add((byte)(i16 >> 8));
            }
            else if (param.CType == CTypes.INT32)
            {
                Int32 i32 = Convert.ToInt32(param.Value);
                formattedPayload.Add((byte)i32);
                formattedPayload.Add((byte)(i32 >> 8));
                formattedPayload.Add((byte)(i32 >> 16));
                formattedPayload.Add((byte)(i32 >> 24));
            }
            else if (param.CType == CTypes.INT64)
            {
                Int64 i64 = Convert.ToInt64(param.Value);
                formattedPayload.Add((byte)i64);
                formattedPayload.Add((byte)(i64 >> 8));
                formattedPayload.Add((byte)(i64 >> 16));
                formattedPayload.Add((byte)(i64 >> 24));
                formattedPayload.Add((byte)(i64 >> 32));
                formattedPayload.Add((byte)(i64 >> 40));
                formattedPayload.Add((byte)(i64 >> 48));
                formattedPayload.Add((byte)(i64 >> 56));
            }
            else if (param.CType == CTypes.STRING)
            {
                string[] strArray = param.Value.Split(',');
                foreach (string s in strArray)
                {
                    byte b = Convert.ToByte(s);
                    formattedPayload.Add(b);
                }

                /* send lsb first for consistancy */
                formattedPayload.Reverse();
            }
            else if (param.CType == CTypes.BYTE_ARRAY)
            {
                string[] strArray = null;
                if (param.Value.Contains(','))
                {
                    strArray = param.Value.Split(',');
                }
                else if (param.Value.Contains(' '))
                {
                    strArray = param.Value.Split(' ');
                }
                else
                {
                    byte b = 0;
                    b = Convert.ToByte(param.Value);
                    formattedPayload.Add(b);
                }
                if (null != strArray)
                {
                    foreach (string s in strArray)
                    {
                        byte b = 0;
                        string strTrim = s.Trim();
                        if ((strTrim.StartsWith("0x") || (strTrim.StartsWith("Ox"))))
                        {
                            /* convert hex string to byte */
                            string num = strTrim.Substring(2, strTrim.Length - 2);
                            int intNum = Int32.Parse(num, System.Globalization.NumberStyles.HexNumber);
                            b = (byte)intNum;
                        }
                        else
                        {
                            b = Convert.ToByte(s);
                        }
                        formattedPayload.Add(b);
                    }
                    /* send lsb first */
                    formattedPayload.Reverse();
                }
            }

            return formattedPayload;
        }

        public static List<Byte> BinaryProtocolBToListByte(BinaryProtocolB binaryProtocolB)
        {
            List<Byte> value = new List<byte>();

            value.Add(binaryProtocolB.StartOfFrame);
            value.Add(binaryProtocolB.Length.Lb);
            value.Add(binaryProtocolB.Length.Hb);
            value.Add(binaryProtocolB.MesssageType);
            value.AddRange(binaryProtocolB.Payload);
            value.Add(binaryProtocolB.CRC.Lb);
            value.Add(binaryProtocolB.CRC.Hb);
            value.Add(binaryProtocolB.EndOfFrame);

            return value;
        }
    }
}
