using Newtonsoft.Json;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProtocolWorkBench.Core.Protocols.CBOR
{
    public static class CBORService
    {
        public static bool OutputDebug { get; set; }

        public static CBORObject CreateCborFromJson(string jsonMsg)
        {
            CBORObject cborObj = null;
            if (!string.IsNullOrEmpty(jsonMsg))
            {
                JSONOptions jo = new JSONOptions("numberconversion=intorfloat");
                JSONOptions.ConversionMode cm = jo.NumberConversion;
                CBOREncodeOptions eo = new CBOREncodeOptions("ctap2Canonical=true");
                cborObj = CBORObject.FromJSONString(jsonMsg, jo);
            }
            return cborObj;
        }

        public static CBORObject CreateCborByteStringForMcuMgrFileUpload (byte[] fileData, string destFilePath, int offSet, UInt32 totalFileSize)
        {
            CBORObject cbor = CBORObject.NewMap();
            cbor.Add("data", fileData);
            cbor.Add("len", totalFileSize);
            cbor.Add("name", destFilePath);
            cbor.Add("off", offSet);
            return cbor;
        }

        public static CBORObject CreateCborByteStringForMcuMgrFileUploadResponse(byte[] fileData)
        {
            CBORObject cbor = CBORObject.NewMap();
            cbor.Add("rc", 0);
            cbor.Add("off", 0);
            cbor.Add("data", fileData);
            cbor.Add("len", fileData.Length);
            return cbor;
        }

        public static CBORObject CreateCborByteStringForMcuMgrSha256Response(int rc, string type, UInt64 offset, UInt64 length, byte[] sha256)
        {
            CBORObject cbor = CBORObject.NewMap();
            cbor.Add("rc", rc);
            cbor.Add("type", type);
            cbor.Add("off", offset);
            cbor.Add("len", length);
            cbor.Add("output", sha256);
            return cbor;
        }

        public static CBORObject CreateCborStringArray(string name, string[] commands)
        {
            CBORObject cbor = CBORObject.NewMap();
            cbor.Add(name, commands);
            return cbor;
        }

        /// <summary>
        /// Convert CBOR Object to a string.
        /// If a CBOR Byte String is present, it will be converted to a byte list.
        /// </summary>
        /// <param name="cBORObj"></param>
        /// <returns></returns>
        public static (List<byte> ByteString, string ByteStringUTF8, string Message) ProcessCborObject(CBORObject cBORObj)
        {
            string debugMsg = null;
            List<byte> file = new List<byte>();
            string strVal = null;
            string json = cBORObj.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(json);
            foreach (var item in cBORObj.Entries)
            {
                debugMsg += item.Key + ": " + item.Value + Environment.NewLine;
                string name = item.Key.AsString();
                if (item.Value.Type == CBORType.ByteString)
                {
                    file = item.Value.GetByteString().ToList();
                    strVal = ConversionService.ConvertByteListToUTF8(file);
                }
            }
            if (OutputDebug)
            {
                DebugService.PrintDebug(debugMsg, null, typeof(CBORService).Name);
            }
            return (file, strVal, debugMsg);
        }
    }
}
