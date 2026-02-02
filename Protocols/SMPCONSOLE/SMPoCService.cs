using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.McuMgr;
using ProtocolWorkBench.Core.Protocols.SMP;
using ProtocolWorkBench.Core.Protocols.SMP.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtocolWorkBench.Core.Protocols.MCUMGR
{
    // see:  https://github.com/zephyrproject-rtos/mcumgr/blob/master/transport/smp-console.md
    public static class SMPoCService
    {
        // Frame overhead is 2 byte header + 1 byte footer
        public const byte FRAME_OVERHEAD = 3;
        public const byte MAX_PACKET_SIZE = 127;
        public const byte SMP_OVER_CONSOLE_HEADER_LENGTH_FIELD_LENGTH = 2;
        public const byte CRC_LENGTH = 2;
        public static byte EOF = 0x0a;
        private const byte MIN_PACKET_SIZE = SMPService.SMP_HEADER_LENGTH + FRAME_OVERHEAD +
            SMP_OVER_CONSOLE_HEADER_LENGTH_FIELD_LENGTH + CRC_LENGTH;

        private static UInt16HbLb SOF_SOP = new UInt16HbLb(0x0609);
        private static UInt16HbLb SOF_CP = new UInt16HbLb(0x0414);

        #region public

        /// <summary>
        /// This takes a complete SMPoC stream (ie includes all framing bytes/checksum) and will convert to a SMP Message
        /// </summary>
        /// <param name="smpOveroleMsg"></param>
        /// <returns></returns>
        public static SmpMessage SmpOverConsoleDefragmentFromBytes(List<byte> smpOverConsoleMsg)
        {
            SmpMessage smpMessage = new SmpMessage();
            List<byte> base64EncodedParts = new List<byte>();

            // break the message into fragments
            var result = SmpConsoleCompleteMessageIntoFragments(smpOverConsoleMsg);
            if (result.Success)
            {
                var result1 = ValidateThenRemoveSmpOverConsoleFramingBytes(result.Fragments);
                if (result1.Success)
                {
                    List<byte> base64DecodedParts = FromBase64Bytes(result1.SMPoCMessage.ToArray());
                    if (base64DecodedParts.Count > SMPService.SMP_HEADER_LENGTH)
                    {
                        UInt16HbLb length = new UInt16HbLb();
                        length.Hb = base64DecodedParts[0];
                        length.Lb = base64DecodedParts[1];

                        UInt16HbLb crc = new UInt16HbLb();
                        crc.Hb = base64DecodedParts[base64DecodedParts.Count - 2];
                        crc.Lb = base64DecodedParts[base64DecodedParts.Count - 1];
                        // remove length
                        base64DecodedParts.RemoveRange(0, 2);
                        //check the SMPoC length
                        if (length.U16Value == base64DecodedParts.Count)
                        {
                            // remove crc
                            // 0 1 2 3 4 5
                            // count = 6, index of 4 is (6-2)
                            base64DecodedParts.RemoveRange(base64DecodedParts.Count - 2, 2);

                            // calculate crc
                            UInt16HbLb crcCalculated = CRCService.CalculateCRCCitt16(0, base64DecodedParts, 0);
                            if (crcCalculated.U16Value == crc.U16Value)
                            {
                                // get and remove header
                                smpMessage.Header = SMPService.GetSmpHeaderFromDataBigEndian(base64DecodedParts);
                                base64DecodedParts.RemoveRange(0, SMPService.SMP_HEADER_LENGTH);

                                // check the smp header length
                                if (smpMessage.Header.GroupId.U16Value == McuMgrService.FileSystemGroupId.U16Value &&
                                    smpMessage.Header.MessageId == McuMgrService.CommandIdFileSystem &&
                                    smpMessage.Header.MgmtOp == (byte)McuMgr.McuMgrService.MgMtOpTypes.ResponseRead)
                                {
                                    // this is a file download
                                    // the SMPoC frame may be split up and the length check is irrelevant here
                                    // just pass the base64 parts up
                                    smpMessage.CBorMessage = base64DecodedParts;
                                }
                                else
                                {
                                    if (smpMessage.Header.PayloadLength.U16Value == base64DecodedParts.Count)
                                    {
                                        // good crc
                                        // rest of message is cbor part
                                        smpMessage.CBorMessage = base64DecodedParts;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return smpMessage;
        }

        public static List<byte> FileDownloadFragmentToBytes(List<byte> smpOverConsoleMsg)
        {
            List<byte> cbor = new List<byte>();

            // break the message into fragments
            var result = SmpConsoleCompleteMessageIntoFragments(smpOverConsoleMsg);
            if (result.Success)
            {
                var result1 = ValidateThenRemoveSmpOverConsoleFramingBytes(result.Fragments);
                if (result1.Success)
                {
                    List<byte> base64DecodedParts = FromBase64Bytes(result1.SMPoCMessage.ToArray());

                    if (base64DecodedParts.Count > SMPService.SMP_HEADER_LENGTH)
                    {
                        UInt16HbLb length = new UInt16HbLb();
                        length.Hb = base64DecodedParts[0];
                        length.Lb = base64DecodedParts[1];

                        UInt16HbLb crc = new UInt16HbLb();
                        crc.Hb = base64DecodedParts[base64DecodedParts.Count - 2];
                        crc.Lb = base64DecodedParts[base64DecodedParts.Count - 1];
                        // remove length
                        base64DecodedParts.RemoveRange(0, 2);
                        //check the SMPoC length
                        if (length.U16Value == base64DecodedParts.Count)
                        {
                            // remove crc
                            // 0 1 2 3 4 5
                            // count = 6, index of 4 is (6-2)
                            base64DecodedParts.RemoveRange(base64DecodedParts.Count - 2, 2);

                            // calculate crc
                            UInt16HbLb crcCalculated = CRCService.CalculateCRCCitt16(0, base64DecodedParts, 0);
                            if (crcCalculated.U16Value == crc.U16Value)
                            {
                                cbor.AddRange(base64DecodedParts);
                            }
                        }
                    }
                }
            }
            return cbor;
        }

        /// <summary>
        /// Convert base64 encoded message to bytes
        /// </summary>
        /// <param name="base64Bytes"></param>
        /// <returns></returns>
        public static List<byte> FromBase64Bytes(byte[] base64Bytes)
        {
            List<byte> values = new List<byte>();

            string base64String = Encoding.UTF8.GetString(base64Bytes, 0, base64Bytes.Length);

            if (IsBase64String(base64String))
            {
                values = Convert.FromBase64String(base64String).ToList();
            }

            return values;
        }

        /// <summary>
        /// Covert a SMP Message to a SMP-Over-Console Message.
        /// This method will add the length and CRC to the message.
        /// The framing bytes (0609, 1404, oa) will need to be added by the transport.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<Byte> SmpToSmpOverConsole(List<byte> smpMessage)
        {
            List<byte> base64EncodedMessage = new List<byte>();

            // the + 2 is the crc length
            UInt16HbLb length = new UInt16HbLb((ushort)smpMessage.Count);
            length.U16Value += 2;
            UInt16HbLb crc = CRCService.CalculateCRCCitt16(0, smpMessage, 0);

            List<byte> data = new List<byte>();
            data.Add(length.Hb);
            data.Add(length.Lb);
            data.AddRange(smpMessage);
            data.Add(crc.Hb);
            data.Add(crc.Lb);
            base64EncodedMessage = Base64Encode(data);

            return base64EncodedMessage;
        }

        /// <summary>
        /// Break the SMPoC Packet into 127-byte fragments
        /// </summary>
        /// <param name="smpOverConsoleMessage"></param>
        /// <returns></returns>
        public static List<List<byte>> FragmentSMPOverConsoleMessage(List<byte> smpOverConsoleMessage)
        {
            List<List<byte>> messages = new List<List<byte>>();
            int chunkSize = MAX_PACKET_SIZE - FRAME_OVERHEAD;

            while (smpOverConsoleMessage.Count > 0)
            {
                List<byte> msg = new List<byte>();
                if (messages.Count == 0)
                {
                    // first frame
                    msg.Add(SOF_SOP.Hb);
                    msg.Add(SOF_SOP.Lb);
                }
                else
                {
                    // not first frame
                    msg.Add(SOF_CP.Hb);
                    msg.Add(SOF_CP.Lb);
                }

                // take chunk of message, or all of it
                if (smpOverConsoleMessage.Count > chunkSize)
                {
                    msg.AddRange(smpOverConsoleMessage.Take(chunkSize));
                    // remove the chunk we have just taken
                    smpOverConsoleMessage.RemoveRange(0, chunkSize);
                }
                else
                {
                    // Add the entire message since it is less than chunk size.
                    msg.AddRange(smpOverConsoleMessage);
                    // remove the chunk we have just taken
                    smpOverConsoleMessage.Clear();
                }

                msg.Add(EOF);
                messages.Add(msg);
            }

            return messages;
        }

        #endregion

        private static List<Byte> Base64Encode(List<Byte> data)
        {
            List<byte> msg = new List<byte>();

            long arrayLength = (long)((4.0d / 3.0d) * (data.Count));
            // If array length is not divisible by 4, go up to the next multiple of 4
            if (arrayLength % 4 != 0)
            {
                arrayLength += 4 - arrayLength % 4;
            }

            char[] outArray = new char[arrayLength];

            int numBytes = Convert.ToBase64CharArray(data.ToArray(), 0, data.Count, outArray, 0);

            msg = Encoding.UTF8.GetBytes(outArray).ToList();

            return msg;
        }

        private static bool IsBase64String(this string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }

        /// <summary>
        /// Find the framing bytes in the message and return the complete message in fragments with
        /// appropriate framing bytes in place.
        /// </summary>
        /// <param name="smpOveroleMsg"></param>
        /// <returns></returns>
        private static (bool Success, List<List<byte>> Fragments) SmpConsoleCompleteMessageIntoFragments(List<byte> smpOveroleMsg)
        {
            List<List<byte>> smpOverConsoleChunks = new List<List<byte>>();
            bool eom = true;
            int index = 0;
            bool success = true;
            while (eom)
            {
                int markerIndex = smpOveroleMsg.IndexOf(EOF, index);
                int subLength = (markerIndex - index) + 1;
                if (subLength > 0)
                {
                    byte[] sublist = new byte[subLength];
                    if ((index + subLength) <= smpOveroleMsg.Count)
                    {
                        smpOveroleMsg.CopyTo(index, sublist, 0, subLength);
                        smpOverConsoleChunks.Add(sublist.ToList());
                        index = markerIndex + 1;
                        // is this the last eof marker?
                        if (index == smpOveroleMsg.Count)
                        {
                            // yes, stop looping
                            eom = false;
                        }
                    }
                    else
                    {
                        success = false;
                        eom = false;
                        break;
                    }
                }
                else
                {
                    success = false;
                    eom = false;
                    break;
                }
            }

            return (success, smpOverConsoleChunks);
        }

        /// <summary>
        /// Validate the proper framing bytes and remove them.
        /// </summary>
        /// <param name="Fragments"></param>
        /// <returns></returns>
        private static (bool Success, List<byte> SMPoCMessage) ValidateThenRemoveSmpOverConsoleFramingBytes(List<List<byte>> Fragments)
        {
            List<byte> SMPoCMessage = new List<byte>();
            bool success = true;

            for (int i = 0; i < Fragments.Count; i++)
            {
                int maxIndex = Fragments[i].Count - 1;
                if (maxIndex > 2)
                {
                    if (i == 0)
                    {
                        if (Fragments[i][0] == SOF_SOP.Hb && Fragments[i][1] == SOF_SOP.Lb && Fragments[i][maxIndex] == EOF)
                        {
                            SMPoCMessage.AddRange(RemoveFramingBytes(Fragments[i]));
                        }
                        else
                        {
                            success = false;
                            break;
                        }
                    }
                    else
                    {
                        if (Fragments[i][0] == SOF_CP.Hb && Fragments[i][1] == SOF_CP.Lb && Fragments[i][maxIndex] == EOF)
                        {
                            SMPoCMessage.AddRange(RemoveFramingBytes(Fragments[i]));
                        }
                        else
                        {
                            success = false;
                            break;
                        }
                    }
                }
            }
            return (success, SMPoCMessage);
        }

        private static List<byte> RemoveFramingBytes(List<byte> msgFragment)
        {
            // remove start-of-frame
            msgFragment.RemoveRange(0, 2);
            // remove end-of-frame
            msgFragment.RemoveAt(msgFragment.Count - 1);
            return msgFragment;
        }
    }
}
