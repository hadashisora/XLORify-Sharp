using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XLORify
{
    class Program
    {
        static void Main(string[] args)
        {
            //Put everything in a try-catch clause
            try
            {
                //Print the thing and check if the input file exists, otherwise print usage
                Console.WriteLine("XLORify-Sharp ver1.0 by CHEMi6DER");
                if (args.Length != 2 && args.Length != 3)
                {
                    Console.WriteLine("Usage: xlorify.exe {path to BxFNT} {path where to save XLOR} {optional: Internal name of XLOR}");
                    return;
                }
                if (!File.Exists(args[0])) Console.WriteLine("Usage: xlorify.exe {path to BxFNT} {path where to save XLOR} {optional: Internal name of XLOR}");
                //Do the thing
                String FilePath = args[0]; //This is here just for debugging purpouses
                String XlorPath = args[1]; //This is here just for debugging purpouses
                //Open th file
                BinaryReader br = new BinaryReader(File.OpenRead(FilePath), Encoding.ASCII);
                String Signature =  new string(br.ReadChars(4));
                Console.WriteLine("Detected file FourCC: " + Signature);
                List<UInt16> charList = new List<UInt16>();
                //Parse the file
                switch (Signature)
                {
                    case "RFNT":
                        charList = ReadRFNT(br);
                        br.Close();
                        break;
                    case "CFNT":
                        charList = ReadCFNT(br);
                        br.Close();
                        break;
                    case "FFNT":
                        charList = ReadFFNT(br);
                        br.Close();
                        break;
                    case "RTFN":
                        charList = ReadNFTR(br); //Not supported yet, because IDK if NTR SDK has FontCvtr or if it even exists at all
                        br.Close();
                        break;
                    default:
                        throw new FormatException("Unsupported format!!!");
                }
                //Dump all the output into a single string
                String output = "";
                //Note: this XLOR structure was taken from an example xlor provided with the FontCvtr, so IDK what is required and what isn't
                //Some XLOR XML header stuff
                output += "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>\n<!DOCTYPE letter-order SYSTEM \"letter-order.dtd\">\n\n<letter-order version=\"1.1\">\n<head>\n<create user=\"Sora";
                output += "\" date=\"";
                //DateTime stuff, IDK if it works properly or if it's even needed since FontCvtr doesn't look at this data
                output += DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "T" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
                output += "\" />\n<title>";
                //In case the user doesn't input a title just shove the name of the file into there
                if (args.Length == 2) output += FilePath.Split('\\')[FilePath.Split('\\').Length - 1];
                else if (args.Length == 3) output += args[2];
                output += "</title>\n<comment>";
                //Screw this description
                output += "Screw this description";
                //There should be 16 columns
                output += "</comment>\n</head>\n\n<body>\n<area width=\"16\" />\n\n<order>\n";
                int row_pos = 0;
                //Dump character codes into the file
                foreach (UInt16 item in charList)
                {
                    //If-Else statement to alighn everything nicely)
                    if (row_pos != 16)
                    {
                        if (item != 0x20) output += "&#x" + item.ToString("X4") + "; ";
                        else output += "<sp/>    ";
                        row_pos++;
                    }
                    if (row_pos == 16)
                    {
                        output += "\n";
                        row_pos = 0;
                    }
                }
                //Footer
                output += "\n</order>\n</body>\n</letter-order>";

                //Simply dump that enormous text string into a file
                Console.WriteLine("Writing XLOR...");
                if (File.Exists(XlorPath)) File.Delete(XlorPath);
                var outfile = File.CreateText(XlorPath);
                outfile.Write(output);
                Console.WriteLine("Successfully written " + outfile.BaseStream.Length + " bytes to XLOR file!!!");
                outfile.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        static List<UInt16> ReadRFNT(BinaryReader br)
        {
            //Sidenote: only Big Endian variant of this code has a kinda in-depth explanation of how the code works, I was just lazy enogh to copy-paste that into other code sections)
            //Read Byte Order Mark to determine how to further process the file
            UInt16 BOM = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
            //Initialize some variables
            List<UInt16> charList = new List<UInt16>();
            Encoding encoding = Encoding.Default;
            //Do the thing, split by endianess
            if (BOM == 0xFEFF)
            {
                //Big endian
                Console.WriteLine("Parsing Big Endian RFNT...");
                br.BaseStream.Position = 0xC; //Goto position of FINF offset
                br.BaseStream.Position = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()) + 0xf; //Go to character encoding offset
                //Read character encoding
                switch (br.ReadByte())
                {
                    case 0:
                        encoding = Encoding.UTF8;
                        break;
                    case 1:
                        encoding = Encoding.Unicode;
                        break;
                    case 2:
                        encoding = Encoding.GetEncoding(932); //Because Microsoft couldn't add an easier way to pick ShiftJIS
                        break;
                    case 3:
                        encoding = Encoding.GetEncoding(1252); //Again, same as above just for CP1252
                        break;
                    default:
                        throw new FormatException("Wrong encoding!!!");
                }
                br.BaseStream.Position += 0x8; //Go to CMAP pointer position
                br.BaseStream.Position = ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()) - 0x8; //Go to first CMAP section
                //Read first CMAP header
                String MapSignature = new string(br.ReadChars(4));
                if (MapSignature != "CMAP") throw new FormatException("Invalid CMAP section!!!");
                bool HasNext = true;
                UInt32 NextPtr = (uint)br.BaseStream.Position + 0x4;
                List<CMAP> cmap_Header = new List<CMAP>();
                List<CMAP_Item> cmap = new List<CMAP_Item>();
                //Parse all CMAP data
                while (HasNext)
                {
                    br.BaseStream.Position = NextPtr - 0x8; //Go to offset of next CMAP section
                    cmap_Header.Add(new CMAP(ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()), ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()))); //Read the header
                    if (cmap_Header[cmap_Header.Count - 1].Signature != 0x434D4150) throw new FormatException("Invalid CMAP section!!!"); //Validate CMAP by checking it's signaure
                    //And here goes the main CMAP parsing thing which I won't explain, since it should be quite obvious
                    while (br.BaseStream.Position != cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8)
                    {
                        switch (cmap_Header[cmap_Header.Count - 1].MappingMethod)
                        {
                            case 0:
                                //Direct mapping method parser
                                var IndexOffset = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i <= cmap_Header[cmap_Header.Count - 1].CodeEnd; i++)
                                {
                                    cmap.Add(new CMAP_Item(i, IndexOffset));
                                    IndexOffset++;
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 1:
                                //Table mapping method
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i < cmap_Header[cmap_Header.Count - 1].CodeEnd + 1; i++)
                                {
                                    var index = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                    if (index != 0xFFFF) cmap.Add(new CMAP_Item(i, index));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 2:
                                //Scan mapping method
                                var Num = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                for (var i = 0; i < Num; i++)
                                {
                                    cmap.Add(new CMAP_Item(ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16())));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                        }
                    }
                    NextPtr = cmap_Header[cmap_Header.Count - 1].PtrNext; //Save pointer to the next CMAP section
                    if (NextPtr == 0x0) HasNext = false; //If NextPtr == 0x0 then we should stop parsing
                }
                foreach (CMAP_Item item in cmap) charList.Add(BitConverter.ToUInt16(Encoding.Convert(encoding, Encoding.Unicode, BitConverter.GetBytes(item.Code)), 0));
                charList.Sort();
            }
            else if (BOM == 0xFFFE)
            {
                //Little endian
                Console.WriteLine("Parsing Little Endian RFNT...");
                br.BaseStream.Position = 0xC;
                br.BaseStream.Position = br.ReadUInt16() + 0xf;
                switch (br.ReadByte())
                {
                    case 0:
                        encoding = Encoding.UTF8;
                        break;
                    case 1:
                        encoding = Encoding.Unicode;
                        break;
                    case 2:
                        encoding = Encoding.GetEncoding(932); //Because Microsoft couldn't add an easier way to pick ShiftJIS
                        break;
                    case 3:
                        encoding = Encoding.GetEncoding(1252); //Again, same as above just for CP1252
                        break;
                    default:
                        throw new FormatException("Wrong encoding!!!");
                }
                br.BaseStream.Position += 0x8;
                br.BaseStream.Position = br.ReadUInt32() - 0x8;
                String MapSignature = new string(br.ReadChars(4));
                if (MapSignature != "CMAP") throw new FormatException("Invalid CMAP section!!!");
                bool HasNext = true;
                UInt32 NextPtr = (uint)br.BaseStream.Position + 0x4;
                List<CMAP> cmap_Header = new List<CMAP>();
                List<CMAP_Item> cmap = new List<CMAP_Item>();
                while (HasNext)
                {
                    br.BaseStream.Position = NextPtr - 0x8;
                    cmap_Header.Add(new CMAP(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt32()));
                    if (cmap_Header[cmap_Header.Count - 1].Signature != 0x50414D43) throw new FormatException("Invalid CMAP section!!!");
                    while (br.BaseStream.Position != cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8)
                    {
                        switch (cmap_Header[cmap_Header.Count - 1].MappingMethod)
                        {
                            case 0:
                                var IndexOffset = br.ReadUInt16();
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i <= cmap_Header[cmap_Header.Count - 1].CodeEnd; i++)
                                {
                                    cmap.Add(new CMAP_Item(i, IndexOffset));
                                    IndexOffset++;
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 1:
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i < cmap_Header[cmap_Header.Count - 1].CodeEnd + 1; i++)
                                {
                                    var index = br.ReadUInt16();
                                    if (index != 0xFFFF) cmap.Add(new CMAP_Item(i, index));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 2:
                                var Num = br.ReadUInt16();
                                for (var i = 0; i < Num; i++)
                                {
                                    cmap.Add(new CMAP_Item(br.ReadUInt16(), br.ReadUInt16()));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                        }
                    }
                    NextPtr = cmap_Header[cmap_Header.Count - 1].PtrNext;
                    if (NextPtr == 0x0) HasNext = false;
                }
                foreach (CMAP_Item item in cmap) charList.Add(BitConverter.ToUInt16(Encoding.Convert(encoding, Encoding.Unicode, BitConverter.GetBytes(item.Code)), 0));
                charList.Sort();
            }
            else throw new FormatException("Wrong BOM!!!");
            Console.WriteLine("Parsed " + charList.Count + " charcters from RFNT");
            return charList; 
        }
        static List<UInt16> ReadCFNT(BinaryReader br)
        {
            UInt16 BOM = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
            List<UInt16> charList = new List<UInt16>();
            Encoding encoding = Encoding.Default;
            if (BOM == 0xFEFF)
            {
                //Big endian
                Console.WriteLine("Parsing Big Endian CFNT...");
                br.BaseStream.Position = 0x6;
                br.BaseStream.Position = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()) + 0xf;
                switch (br.ReadByte())
                {
                    case 0:
                        encoding = Encoding.UTF8;
                        break;
                    case 1:
                        encoding = Encoding.Unicode;
                        break;
                    case 2:
                        encoding = Encoding.GetEncoding(932); //Because Microsoft couldn't add an easier way to pick ShiftJIS
                        break;
                    case 3:
                        encoding = Encoding.GetEncoding(1252); //Again, same as above just for CP1252
                        break;
                    default:
                        throw new FormatException("Wrong encoding!!!");
                }
                br.BaseStream.Position += 0x8;
                br.BaseStream.Position = ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()) - 0x8;
                String MapSignature = new string(br.ReadChars(4));
                if (MapSignature != "CMAP") throw new FormatException("Invalid CMAP section!!!");
                bool HasNext = true;
                UInt32 NextPtr = (uint)br.BaseStream.Position + 0x4;
                List<CMAP> cmap_Header = new List<CMAP>();
                List<CMAP_Item> cmap = new List<CMAP_Item>();
                while (HasNext)
                {
                    br.BaseStream.Position = NextPtr - 0x8;
                    cmap_Header.Add(new CMAP(ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()), ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32())));
                    if (cmap_Header[cmap_Header.Count - 1].Signature != 0x434D4150) throw new FormatException("Invalid CMAP section!!!");
                    while (br.BaseStream.Position != cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8)
                    {
                        switch (cmap_Header[cmap_Header.Count - 1].MappingMethod) //This is very stupid, but it works
                        {
                            case 0:
                                var IndexOffset = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i <= cmap_Header[cmap_Header.Count - 1].CodeEnd; i++)
                                {
                                    cmap.Add(new CMAP_Item(i, IndexOffset));
                                    IndexOffset++;
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 1:
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i < cmap_Header[cmap_Header.Count - 1].CodeEnd + 1; i++)
                                {
                                    var index = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                    if (index != 0xFFFF) cmap.Add(new CMAP_Item(i, index));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 2:
                                var Num = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                for (var i = 0; i < Num; i++)
                                {
                                    cmap.Add(new CMAP_Item(ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16())));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                        }
                    }
                    NextPtr = cmap_Header[cmap_Header.Count - 1].PtrNext;
                    if (NextPtr == 0x0) HasNext = false;
                }
                foreach (CMAP_Item item in cmap) charList.Add(BitConverter.ToUInt16(Encoding.Convert(encoding, Encoding.Unicode, BitConverter.GetBytes(item.Code)), 0));
                charList.Sort();
            }
            else if (BOM == 0xFFFE)
            {
                //Little endian
                Console.WriteLine("Parsing Little Endian CFNT...");
                br.BaseStream.Position = 0x6;
                br.BaseStream.Position = br.ReadUInt16() + 0xf;
                switch (br.ReadByte())
                {
                    case 0:
                        encoding = Encoding.UTF8;
                        break;
                    case 1:
                        encoding = Encoding.Unicode;
                        break;
                    case 2:
                        encoding = Encoding.GetEncoding(932); //Because Microsoft couldn't add an easier way to pick ShiftJIS
                        break;
                    case 3:
                        encoding = Encoding.GetEncoding(1252); //Again, same as above just for CP1252
                        break;
                    default:
                        throw new FormatException("Wrong encoding!!!");
                }
                br.BaseStream.Position += 0x8;
                br.BaseStream.Position = br.ReadUInt32() - 0x8;
                String MapSignature = new string(br.ReadChars(4));
                if (MapSignature != "CMAP") throw new FormatException("Invalid CMAP section!!!");
                bool HasNext = true;
                UInt32 NextPtr = (uint)br.BaseStream.Position + 0x4;
                List<CMAP> cmap_Header = new List<CMAP>();
                List<CMAP_Item> cmap = new List<CMAP_Item>();
                while (HasNext)
                {
                    br.BaseStream.Position = NextPtr - 0x8;
                    cmap_Header.Add(new CMAP(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt32()));
                    if (cmap_Header[cmap_Header.Count - 1].Signature != 0x50414D43) throw new FormatException("Invalid CMAP section!!!");
                    while (br.BaseStream.Position != cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8)
                    {
                        switch (cmap_Header[cmap_Header.Count - 1].MappingMethod) //This is very stupid, but it works
                        {
                            case 0:
                                var IndexOffset = br.ReadUInt16();
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i <= cmap_Header[cmap_Header.Count - 1].CodeEnd; i++)
                                {
                                    cmap.Add(new CMAP_Item(i, IndexOffset));
                                    IndexOffset++;
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 1:
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i < cmap_Header[cmap_Header.Count - 1].CodeEnd + 1; i++)
                                {
                                    var index = br.ReadUInt16();
                                    if (index != 0xFFFF) cmap.Add(new CMAP_Item(i, index));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 2:
                                var Num = br.ReadUInt16();
                                for (var i = 0; i < Num; i++)
                                {
                                    cmap.Add(new CMAP_Item(br.ReadUInt16(), br.ReadUInt16()));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                        }
                    }
                    NextPtr = cmap_Header[cmap_Header.Count - 1].PtrNext;
                    if (NextPtr == 0x0) HasNext = false;
                }
                foreach (CMAP_Item item in cmap) charList.Add(BitConverter.ToUInt16(Encoding.Convert(encoding, Encoding.Unicode, BitConverter.GetBytes(item.Code)), 0));
                charList.Sort();
            }
            else throw new FormatException("Wrong BOM!!!");
            Console.WriteLine("Parsed " + charList.Count + " charcters from CFNT");
            return charList;
        }
        static List<UInt16> ReadFFNT(BinaryReader br)
        {
            UInt16 BOM = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
            List<UInt16> charList = new List<UInt16>();
            Encoding encoding = Encoding.Default;
            if (BOM == 0xFEFF)
            {
                //Big endian
                Console.WriteLine("Parsing Big Endian FFNT...");
                br.BaseStream.Position = 0x6;
                br.BaseStream.Position = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()) + 0x13;
                switch (br.ReadByte())
                {
                    case 0:
                        encoding = Encoding.UTF8;
                        break;
                    case 1:
                        encoding = Encoding.Unicode;
                        break;
                    case 2:
                        encoding = Encoding.GetEncoding(932); //Because Microsoft couldn't add an easier way to pick ShiftJIS
                        break;
                    case 3:
                        encoding = Encoding.GetEncoding(1252); //Again, same as above just for CP1252
                        break;
                    default:
                        throw new FormatException("Wrong encoding!!!");
                }
                br.BaseStream.Position += 0x9;
                br.BaseStream.Position = ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()) - 0x8;
                String MapSignature = new string(br.ReadChars(4));
                if (MapSignature != "CMAP") throw new FormatException("Invalid CMAP section!!!");
                bool HasNext = true;
                UInt32 NextPtr = (uint)br.BaseStream.Position + 0x4;
                List<CMAP> cmap_Header = new List<CMAP>();
                List<CMAP_Item> cmap = new List<CMAP_Item>();
                while (HasNext)
                {
                    br.BaseStream.Position = NextPtr - 0x8;
                    cmap_Header.Add(new CMAP(ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()), ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt32_ReverseEndianness(br.ReadUInt32())));
                    if (cmap_Header[cmap_Header.Count - 1].Signature != 0x434D4150) throw new FormatException("Invalid CMAP section!!!");
                    while (br.BaseStream.Position != cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8)
                    {
                        switch (cmap_Header[cmap_Header.Count - 1].MappingMethod) //This is very stupid, but it works
                        {
                            case 0:
                                var IndexOffset = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i <= cmap_Header[cmap_Header.Count - 1].CodeEnd; i++)
                                {
                                    cmap.Add(new CMAP_Item(i, IndexOffset));
                                    IndexOffset++;
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 1:
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i < cmap_Header[cmap_Header.Count - 1].CodeEnd + 1; i++)
                                {
                                    var index = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                    if (index != 0xFFFF) cmap.Add(new CMAP_Item(i, index));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 2:
                                var Num = ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16());
                                for (var i = 0; i < Num; i++)
                                {
                                    cmap.Add(new CMAP_Item(ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16()), ReverseEndianness.UInt16_ReverseEndianness(br.ReadUInt16())));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                        }
                    }
                    NextPtr = cmap_Header[cmap_Header.Count - 1].PtrNext;
                    if (NextPtr == 0x0) HasNext = false;
                }
                foreach (CMAP_Item item in cmap) charList.Add(BitConverter.ToUInt16(Encoding.Convert(encoding, Encoding.Unicode, BitConverter.GetBytes(item.Code)), 0));
                charList.Sort();
            }
            else if (BOM == 0xFFFE)
            {
                //Little endian
                Console.WriteLine("Parsing Little Endian FFNT...");
                br.BaseStream.Position = 0x6;
                br.BaseStream.Position = br.ReadUInt16() + 0x13;
                switch (br.ReadByte())
                {
                    case 0:
                        encoding = Encoding.UTF8;
                        break;
                    case 1:
                        encoding = Encoding.Unicode;
                        break;
                    case 2:
                        encoding = Encoding.GetEncoding(932); //Because Microsoft couldn't add an easier way to pick ShiftJIS
                        break;
                    case 3:
                        encoding = Encoding.GetEncoding(1252); //Again, same as above just for CP1252
                        break;
                    default:
                        throw new FormatException("Wrong encoding!!!");
                }
                br.BaseStream.Position += 0x9;
                br.BaseStream.Position = br.ReadUInt32() - 0x8;
                String MapSignature = new string(br.ReadChars(4));
                if (MapSignature != "CMAP") throw new FormatException("Invalid CMAP section!!!");
                bool HasNext = true;
                UInt32 NextPtr = (uint)br.BaseStream.Position + 0x4;
                List<CMAP> cmap_Header = new List<CMAP>();
                List<CMAP_Item> cmap = new List<CMAP_Item>();
                while (HasNext)
                {
                    br.BaseStream.Position = NextPtr - 0x8;
                    cmap_Header.Add(new CMAP(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt32()));
                    if (cmap_Header[cmap_Header.Count - 1].Signature != 0x50414D43) throw new FormatException("Invalid CMAP section!!!");
                    while (br.BaseStream.Position != cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8)
                    {
                        switch (cmap_Header[cmap_Header.Count - 1].MappingMethod) //This is very stupid, but it works
                        {
                            case 0:
                                var IndexOffset = br.ReadUInt16();
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i <= cmap_Header[cmap_Header.Count - 1].CodeEnd; i++)
                                {
                                    cmap.Add(new CMAP_Item(i, IndexOffset));
                                    IndexOffset++;
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 1:
                                for (var i = cmap_Header[cmap_Header.Count - 1].CodeBegin; i < cmap_Header[cmap_Header.Count - 1].CodeEnd + 1; i++)
                                {
                                    var index = br.ReadUInt16();
                                    if (index != 0xFFFF) cmap.Add(new CMAP_Item(i, index));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                            case 2:
                                var Num = br.ReadUInt16();
                                for (var i = 0; i < Num; i++)
                                {
                                    cmap.Add(new CMAP_Item(br.ReadUInt16(), br.ReadUInt16()));
                                }
                                br.BaseStream.Position = cmap_Header[cmap_Header.Count - 1].PtrNext - 0x8;
                                break;
                        }
                    }
                    NextPtr = cmap_Header[cmap_Header.Count - 1].PtrNext;
                    if (NextPtr == 0x0) HasNext = false;
                }
                foreach (CMAP_Item item in cmap) charList.Add(BitConverter.ToUInt16(Encoding.Convert(encoding, Encoding.Unicode, BitConverter.GetBytes(item.Code)), 0));
                charList.Sort();
            }
            else throw new FormatException("Wrong BOM!!!");
            Console.WriteLine("Parsed " + charList.Count + " charcters from FFNT");
            return charList;
        }
        static List<UInt16> ReadNFTR(BinaryReader br)
        {
            //Not supported yet, because IDK if NTR SDK has FontCvtr or if it even exists at all
            throw new FormatException("NFTR not supported yet!!!");
        }
    }

    class CMAP
    {
        //Represents CMAP header
        public UInt32 Signature; //Should always be 0x434D4150, CMAP in ASCII
        public UInt32 Length; //CMAP section length in bytes
        public UInt16 CodeBegin; //First character code
        public UInt16 CodeEnd; //Last character code
        public UInt16 MappingMethod; //Mapping method, 0x0 - Direct, 0x1 - Table, 0x2 - Scan
        public UInt16 Reserved; //Reserved
        public UInt32 PtrNext; //Pointer to the next CMAP(next CMAP offset + 0x8), 0x0 if last

        public CMAP(UInt32 Signature, UInt32 Length, UInt16 CodeBegin, UInt16 CodeEnd, UInt16 MappingMethod, UInt16 Reserved, UInt32 PtrNext)
        {
            //Constructor for CMAP class
            this.Signature = Signature;
            this.Length = Length;
            this.CodeBegin = CodeBegin;
            this.CodeEnd = CodeEnd;
            this.MappingMethod = MappingMethod;
            this.Reserved = Reserved;
            this.PtrNext = PtrNext;
        }
    }

    class CMAP_Item
    {
        //Represents CMAP data item
        public UInt16 Code; //Character code
        public UInt16 Index; //Glyph index

        public CMAP_Item(UInt16 Code, UInt16 Index)
        {
            this.Code = Code;
            this.Index = Index;
        }
    }
}
