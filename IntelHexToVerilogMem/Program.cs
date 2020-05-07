using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IntelHexToVerilogMem
{
    class Program
    {
        static string inputPath = "";
        static string outputPath = "";
        static string outHexGroupLenBytes = "2";
        static UInt32 baseAddress = 0;
        static uint nrOfFilesToSplit = 1;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("    -i <file in>");
                Console.WriteLine("    -o <file out>");
                Console.WriteLine("    -g <nr of bytes on one row> (optional, default 2, accepted 1,2,4)");
                Console.WriteLine("    -b <offset address of the rom> (optional, default 0, not needed for .bin files)");
                Console.WriteLine("    -s <nr of files to split the colons> (optional, default 1, accepted 1,2,4 less or equal -g)");
                Environment.Exit(-1);
            }
            int argsCntInt = 0;
            for (; argsCntInt < args.Length; argsCntInt++)
            {
                if (args[argsCntInt] == "-i")
                {
                    argsCntInt++;
                    inputPath = args[argsCntInt];
                }
                else if (args[argsCntInt] == "-o")
                {
                    argsCntInt++;
                    outputPath = args[argsCntInt];
                }
                else if (args[argsCntInt] == "-g")
                {
                    argsCntInt++;
                    outHexGroupLenBytes = args[argsCntInt];
                }
                else if (args[argsCntInt] == "-b")
                {
                    argsCntInt++;
                    baseAddress = System.Convert.ToUInt32(args[argsCntInt], 16);
                }
                else if (args[argsCntInt] == "-s")
                {
                    argsCntInt++;
                    nrOfFilesToSplit = System.Convert.ToUInt32(args[argsCntInt]);
                }
            }
            if (inputPath.Length == 0 || outputPath.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("    -i <file in>");
                Console.WriteLine("    -o <file out>");
                Console.WriteLine("    -g <nr of bytes on one row> (optional, default 2, accepted 1,2,4)");
                Console.WriteLine("    -b <offset address of the rom> (optional, default 0, not needed for .bin files)");
                Console.WriteLine("    -s <nr of files to split the colons> (optional, default 1, accepted 1,2,4 less or equal -g)");
                Environment.Exit(-1);
            }
            if (Path.GetExtension(inputPath) == ".hex")
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(inputPath);
                }
                catch
                {
                    Console.WriteLine("Invalid file input.");
                    return;
                }
                UInt32 extendedAddress = 0;
                List<string> rom = new List<string>();
                foreach (string line in lines)
                {
                    if (line[0] == ':')
                    {
                        UInt32 lineLenInt = (Convert.ToUInt32(line.Substring(1, 2), 16) * Convert.ToUInt32(2));
                        if (lineLenInt == line.Length - 11)
                        {
                            int cnt = 1;
                            byte chk = (byte)0;
                            for (; cnt < line.Length - 2; cnt += 2)
                            {
                                chk -= Convert.ToByte(line.Substring(cnt, 2), 16);
                            }
                            byte chkCalc = Convert.ToByte(line.Substring((int)lineLenInt + 9, 2), 16);
                            if (chkCalc != (byte)chk)
                            {
                                Console.WriteLine("Invalid checksum.");
                                Environment.Exit(-4);
                            }
                            string lineType = line.Substring(7, 2);
                            if (lineType == "00") // Data line//Contains data and 16-bit address. The format described above.
                            {
                                UInt32 lineAddrLowInt = (extendedAddress - baseAddress) + Convert.ToUInt32(line.Substring(3, 4), 16);
                                string lineData = line.Substring(9, Convert.ToInt32(lineLenInt));
                                if (rom.Count < lineAddrLowInt + (lineLenInt / 2))
                                {
                                    int lineNr = rom.Count;
                                    for (; lineNr < lineAddrLowInt + (lineLenInt / 2); lineNr = rom.Count)
                                    {
                                        rom.Add("FF");
                                    }
                                    lineNr = 0;
                                    for (; lineNr < lineLenInt / 2; lineNr++)
                                    {
                                        rom[(int)lineAddrLowInt + lineNr] = lineData.Substring(lineNr * 2, 2);
                                    }
                                }
                            }
                            else if (lineType == "01") //End of file//A file termination record. No data. Has to be the last line of the file, only one per file permitted. Usually ':00000001FF'. Originally the End Of File record could contain a start address for the program being loaded, e.g. :00AB2F0125 would make a jump to address AB2F. This was convenient when programs were loaded from punched paper tape.
                            {
                                List<string> outRom = new List<string>();
                                int cntOutRomInt = 0;

                                //File.Delete(outputPath);
                                for (; cntOutRomInt < rom.Count; cntOutRomInt += Convert.ToInt32(outHexGroupLenBytes, 10))
                                {
                                    int cntOutRomByteInt = Convert.ToInt32(outHexGroupLenBytes, 10);
                                    string tmp = "";
                                    for (; cntOutRomByteInt > 0; cntOutRomByteInt--)
                                    {
                                        if (cntOutRomInt + cntOutRomByteInt - 1 < rom.Count)
                                        {
                                            tmp += rom[cntOutRomInt + cntOutRomByteInt - 1];
                                        }
                                        else
                                        {
                                            tmp += "00";
                                        }
                                    }
                                    outRom.Add(tmp);
                                    //File.AppendAllText(outputPath, tmp + "\n");
                                }
                                File.WriteAllLines(outputPath, outRom.ToArray());
                                if (nrOfFilesToSplit == 2)
                                {
                                    List<string> outRomL = new List<string>();
                                    List<string> outRomH = new List<string>();
                                    for (cntOutRomInt = 0; cntOutRomInt < outRom.Count; cntOutRomInt++)
                                    {
                                        outRomL.Add(outRom[cntOutRomInt].Substring((outHexGroupLenBytes == "2") ? 2 : 4, (outHexGroupLenBytes == "2") ? 2 : 4));
                                        outRomH.Add(outRom[cntOutRomInt].Substring(0, (outHexGroupLenBytes == "2") ? 2 : 4));
                                    }
                                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_0" + Path.GetExtension(outputPath), outRomL.ToArray());
                                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_1" + Path.GetExtension(outputPath), outRomH.ToArray());
                                }
                                else if (nrOfFilesToSplit == 4)
                                {
                                    List<string> outRom0 = new List<string>();
                                    List<string> outRom1 = new List<string>();
                                    List<string> outRom2 = new List<string>();
                                    List<string> outRom3 = new List<string>();
                                    for (cntOutRomInt = 0; cntOutRomInt < outRom.Count; cntOutRomInt++)
                                    {
                                        outRom0.Add(outRom[cntOutRomInt].Substring(6, 2));
                                        outRom1.Add(outRom[cntOutRomInt].Substring(4, 2));
                                        outRom2.Add(outRom[cntOutRomInt].Substring(2, 2));
                                        outRom3.Add(outRom[cntOutRomInt].Substring(0, 2));
                                    }
                                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_0" + Path.GetExtension(outputPath), outRom0.ToArray());
                                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_1" + Path.GetExtension(outputPath), outRom1.ToArray());
                                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_2" + Path.GetExtension(outputPath), outRom2.ToArray());
                                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_3" + Path.GetExtension(outputPath), outRom3.ToArray());
                                }
                                Environment.Exit(0);
                            }
                            else if (lineType == "02") //Extended segment address//Segment-base address. Used when 16 bits are not enough, identical to 80x86 real mode addressing. The address specified by the 02 record is multiplied by 16 (shifted 4 bits left) and added to the subsequent 00 record addresses. This allows addressing of up to a megabyte of address space. The address field of this record has to be 0000, the byte count is 02 (the segment is 16-bit). The least significant hex digit of the segment address is always 0.
                            {
                                extendedAddress = Convert.ToUInt32(line.Substring(9, 4), 16) << 4;
                            }
                            else if (lineType == "03") //Start segment address//For 80x86 processors, it specifies the initial content of the CS:IP registers. The address field is 0000, the byte count is 04, the first two bytes are the CS value, the latter two are the IP value.
                            {

                            }
                            else if (lineType == "04") //Extended line address//Allowing for fully 32 bit addressing. The address field is 0000, the byte count is 02. The two data bytes represent the upper 16 bits of the 32 bit address, when combined with the address of the 00 type record.
                            {
                                extendedAddress = Convert.ToUInt32(line.Substring(9, 4), 16) << 16;
                            }
                            else if (lineType == "05") //Start line address//The address field is 0000, the byte count is 04. The 4 data bytes represent the 32-bit value loaded into the EIP register of the 80386 and higher CPU.
                            {

                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid hex line lenght.");
                            Environment.Exit(-3);
                        }
                    }
                }
            }
            else if (Path.GetExtension(inputPath) == ".bin")
            {
                byte[] file = File.ReadAllBytes(inputPath);
                List<string> outRom = new List<string>();
                int cntOutRomInt = 0;

                //File.Delete(outputPath);
                for (; cntOutRomInt < file.Length; cntOutRomInt += Convert.ToInt32(outHexGroupLenBytes, 10))
                {
                    int cntOutRomByteInt = Convert.ToInt32(outHexGroupLenBytes, 10);
                    string tmp = "";
                    for (; cntOutRomByteInt > 0; cntOutRomByteInt--)
                    {
                        if (cntOutRomInt + cntOutRomByteInt - 1 < file.Length)
                        {
                            byte tmp_byte = file[cntOutRomInt + cntOutRomByteInt - 1];
                            tmp += Convert.ToString(tmp_byte, 16);
                        }
                        else
                        {
                            tmp += "00";
                        }
                    }
                    outRom.Add(tmp);
                    //File.AppendAllText(outputPath, tmp + "\n");
                }
                File.WriteAllLines(outputPath, outRom.ToArray());
                if (nrOfFilesToSplit == 2)
                {
                    List<string> outRomL = new List<string>();
                    List<string> outRomH = new List<string>();
                    for (cntOutRomInt = 0; cntOutRomInt < outRom.Count; cntOutRomInt++)
                    {
                        outRomL.Add(outRom[cntOutRomInt].Substring((outHexGroupLenBytes == "2") ? 2 : 4, (outHexGroupLenBytes == "2") ? 2 : 4));
                        outRomH.Add(outRom[cntOutRomInt].Substring(0, (outHexGroupLenBytes == "2") ? 2 : 4));
                    }
                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_0" + Path.GetExtension(outputPath), outRomL.ToArray());
                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_1" + Path.GetExtension(outputPath), outRomH.ToArray());
                }
                else if (nrOfFilesToSplit == 4)
                {
                    List<string> outRom0 = new List<string>();
                    List<string> outRom1 = new List<string>();
                    List<string> outRom2 = new List<string>();
                    List<string> outRom3 = new List<string>();
                    for (cntOutRomInt = 0; cntOutRomInt < outRom.Count; cntOutRomInt++)
                    {
                        outRom0.Add(outRom[cntOutRomInt].Substring(6, 2));
                        outRom1.Add(outRom[cntOutRomInt].Substring(4, 2));
                        outRom2.Add(outRom[cntOutRomInt].Substring(2, 2));
                        outRom3.Add(outRom[cntOutRomInt].Substring(0, 2));
                    }
                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_0" + Path.GetExtension(outputPath), outRom0.ToArray());
                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_1" + Path.GetExtension(outputPath), outRom1.ToArray());
                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_2" + Path.GetExtension(outputPath), outRom2.ToArray());
                    File.WriteAllLines(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_3" + Path.GetExtension(outputPath), outRom3.ToArray());
                }
                Environment.Exit(0);
            }
            Environment.Exit(-2);
        }
    }
}
