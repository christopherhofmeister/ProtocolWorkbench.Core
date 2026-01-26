using ProtocolWorkBench.Core.Models;
using Newtonsoft.Json.Linq;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtocolWorkBench.Core
{
    public static class ConversionService
    {
        public static string ConvertByteListToASCII(List<byte> listToConvert)
        {
            string value = null;
            ASCIIEncoding encoding = new ASCIIEncoding();
            value = encoding.GetString(listToConvert.ToArray());
            return value;
        }

        public static string ConvertByteListToUTF8(List<byte> listToConvert)
        {
            return System.Text.Encoding.UTF8.GetString(listToConvert.ToArray());
        }

        public static string ConvertByteArrayToHexString(List<byte> listToConvert, bool addSpaces)
        {
            string value = null;
            if (listToConvert.Count > 0)
            {
                for (int i = 0; i < listToConvert.Count; i++)
                {
                    if (addSpaces)
                    {
                        value += string.Format("{0:X2} ", listToConvert[i]);
                    }
                    else
                    {
                        value += string.Format("{0:X2}", listToConvert[i]);
                    }
                }
                value = value.TrimEnd();
            }

            /* remove trailing space */
            return value;
        }

        public static byte[] ConvertStringToByteArray(string stringData)
        {
            return Encoding.ASCII.GetBytes(stringData);
        }

        public static List<byte> ConvertHexByteStringWithSpacesToByteArray(string stringHex)
        {
            List<byte> result = new List<byte>();
            if (stringHex.Contains(' '))
            {
                string[] values = stringHex.Split(' ');
                foreach (string hexVal in values)
                {
                    if (!string.IsNullOrEmpty(hexVal))
                    {
                        result.Add(Convert.ToByte(hexVal, 16));
                    }
                }
            }
            return result;
        }

        public static JProperty ConvertMessageParmToJParam(MessageParameter msgParam)
        {
            JObject jObj = new JObject();
            object val = null;

            switch (msgParam.CType)
            {
                case CTypes.BOOL:
                    bool bValue = bool.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, bValue);
                    val = bValue;
                    break;

                case CTypes.STRING:
                    jObj.Add(msgParam.Name, msgParam.Value);
                    val = msgParam.Value;
                    break;

                case CTypes.FLOAT:
                    float fValue = float.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, fValue);
                    val = fValue;
                    break;
                case CTypes.DOUBLE:
                    double dValue = double.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, dValue);
                    val = dValue;
                    break;

                case CTypes.BYTE_ARRAY:
                    CBORObject cbor = CBORObject.NewMap();
                    cbor.Add("bytes", msgParam.ByteArray);
                    byte[] bytes = cbor.EncodeToBytes();
                    /* note by default newtonsoft will convert a byte array into
                     * base 64, so special care was taken not to do this. */
                    JToken t = JToken.FromObject(msgParam.ByteArray.ToList());
                    jObj.Add(msgParam.Name, t);
                    val = msgParam.ByteArray.ToList();
                    break;

                case CTypes.INT8:
                case CTypes.INT16:
                    Int16 i16Value = Int16.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, i16Value);
                    val = i16Value;
                    break;
                case CTypes.INT32:
                    Int32 i32Value = Int32.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, i32Value);
                    val = i32Value;
                    break;
                case CTypes.INT64:
                    Int64 i64Value = Int64.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, i64Value);
                    val = i64Value;
                    break;

                case CTypes.UINT8:
                case CTypes.UINT16:
                    UInt16 u16Value = UInt16.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, u16Value);
                    val = u16Value;
                    break;
                case CTypes.UINT32:
                    UInt32 u32Value = UInt32.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, u32Value);
                    val = u32Value;
                    break;
                case CTypes.UINT64:
                    UInt64 u64Value = UInt64.Parse(msgParam.Value);
                    jObj.Add(msgParam.Name, u64Value);
                    val = u64Value;
                    break;
            }

            JProperty jProperty = new JProperty(msgParam.Name, val);
            return jProperty;
        }
    }
}
