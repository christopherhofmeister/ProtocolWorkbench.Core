using ProtocolWorkbench.Core.Enums;
using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class BinaryParameterEncoding
    {
        public static List<Byte> ParameterToBytesLSBFirst(MessageParameter param)
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
    }
}
