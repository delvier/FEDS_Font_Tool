﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEDS_Font_Tool
{
    class Program
    {
        public static void FromWeirdFont(string path)
        {
            string filename = Path.GetFileName(path);
            string dirname = Path.GetDirectoryName(path);
            byte[] filedata = File.ReadAllBytes(path);
            uint skip = 0x20;
            uint[] lowByteAddr = new uint[192];
            for (int i = 0; i < 192; i++)
            {
                lowByteAddr[i] = BitConverter.ToUInt32(filedata.Skip((int)(skip + 4 * i)).Take(4).ToArray());
            }
            byte[] export;
            using (MemoryStream ex = new())
            {
                using (BinaryWriter bw = new(ex))
                {
                    for (int i = 0; i < 192; i++)
                    {
                        if (lowByteAddr[i] == 0)
                        {
                            continue;
                        }
                        int j = 0;
                        while (true)
                        {
                            ushort hexCode = BitConverter.ToUInt16(filedata.Skip((int)(skip + lowByteAddr[i] + 8 * j)).Take(2).ToArray());
                            if (hexCode == 0)
                            {
                                break;
                            }
                            ushort width = BitConverter.ToUInt16(filedata.Skip((int)(skip + lowByteAddr[i] + 8 * j + 2)).Take(2).ToArray());
                            uint header = (uint)(width * 65536 + hexCode);
                            uint pos = BitConverter.ToUInt32(filedata.Skip((int)(skip + lowByteAddr[i] + 8 * j + 4)).Take(4).ToArray());
                            uint[] output = WeirdGlyphDecipher(filedata, + pos);
                            bw.Write(BitConverter.GetBytes(header));
                            for (int k = 0; k < 32; k++)
                            {
                                bw.Write(BitConverter.GetBytes(output[k]));
                            }
                            j++;
                        }
                    }
                }
                export = ex.ToArray();
            }
            File.WriteAllBytes($"{dirname}{Path.DirectorySeparatorChar}{filename}.dec", export);
        }
        public static void ToWeirdFont(string path)
        {
            string filename = Path.GetFileName(path).Replace(".dec","");
            string dirname = Path.GetDirectoryName(path);
            byte[] filedata = File.ReadAllBytes(path);
            uint glyph_size = 0x84;
            List<byte[]>[] glyph_info = new List<byte[]>[0x100 - 0x40];
            List<byte[]>[] glyphs = new List<byte[]>[0x100 - 0x40];
            for (int i = 0; i < 0x100 - 0x40; i++)
            {
                glyph_info[i] = new List<byte[]>();
                glyphs[i] = new List<byte[]>();
            }
            for (int i = 0; i < filedata.Length / glyph_size; i++)
            {
                byte[] each_glyph = filedata.Skip((int)(i * glyph_size)).Take((int)glyph_size).ToArray();
                byte low_byte = each_glyph[0];
                glyph_info[low_byte - 0x40].Add(each_glyph.Take(4).ToArray());
                glyphs[low_byte - 0x40].Add(WeirdGlyphRecipher(each_glyph.Skip(4).ToArray()));
            }

            byte[] part0 = new byte[0x20];
            byte[] part1 = new byte[0x300]; //pointers by lower bytes
            List<byte> part2_tmp = new List<byte>(); //data for glyphs
            List<byte> part3_tmp = new List<byte>(); //glyphs
            List<byte> part4_tmp = new List<byte>(); //pointers to pointers
            for (int i = 0; i < glyph_info.Length; i++)
            {
                if (glyph_info[i].Count != 0)
                {
                    Array.Copy(BitConverter.GetBytes(0x300 + part2_tmp.Count()), 0, part1, i * 4, 4);
                    part4_tmp.AddRange(BitConverter.GetBytes(i * 4));
                    for (int ii = 0; ii < glyph_info[i].Count; ii++)
                    {
                        part2_tmp.AddRange(glyph_info[i][ii]);
                        part2_tmp.AddRange(new byte[] { 0, 0, 0, 0 });
                    }
                    part2_tmp.AddRange(new byte[] { 0, 0, 0, 0 }); // separator
                }
            }
            byte[] part2 = part2_tmp.ToArray();
            for (int i = 0; i < glyphs.Length; i++)
            {
                for (int ii = 0; ii < glyph_info[i].Count; ii++)
                {
                    int pointer = BitConverter.ToInt32(part1.Skip(i * 4).Take(4).ToArray()) + 8 * ii + 4;
                    int pos = 0x300 + part2.Count() + part3_tmp.Count();
                    part4_tmp.AddRange(BitConverter.GetBytes(pointer));
                    Array.Copy(BitConverter.GetBytes(pos), 0, part2, pointer - 0x300, 4);
                    part3_tmp.AddRange(glyphs[i][ii]);
                }
            }
            while (part3_tmp.Count % 4 != 0)
            {
                part3_tmp.Add((byte)0);
            }
            byte[] part3 = part3_tmp.ToArray();
            byte[] part4 = part4_tmp.ToArray();
            int length = 0x320 + part2.Length + part3.Length + part4.Length;
            int part4_ptr = 0x300 + part2.Length + part3.Length;
            int ptr_no = part4.Length / 4;
            Array.Copy(BitConverter.GetBytes(length), 0, part0, 0, 4);
            Array.Copy(BitConverter.GetBytes(part4_ptr), 0, part0, 4, 4);
            Array.Copy(BitConverter.GetBytes(ptr_no), 0, part0, 8, 4);
            List<byte> complete = new List<byte>();
            complete.AddRange(part0);
            complete.AddRange(part1);
            complete.AddRange(part2);
            complete.AddRange(part3);
            complete.AddRange(part4);
            File.WriteAllBytes($"{dirname}{Path.DirectorySeparatorChar}{filename}.enc", complete.ToArray());
        }
        public static uint[] WeirdGlyphDecipher(byte[] filedata, uint pos)
        {
            uint skip = 0x20;
            uint[] glyph = new uint[32];
            int num = 0;
            int i = 0;
            while (num < 256)
            {
                byte[] block = filedata.Skip((int)(skip + pos + 5 * i)).Take(5).ToArray();
                byte isTransparent = block[0];
                for (int j = 0; j < 8; j++)
                {
                    int co; // counter or colour
                    if (j % 2 == 0)
                    {
                        co = block[j / 2 + 1] / 16;
                    } else
                    {
                        co = block[j / 2 + 1] % 16;
                    }
                    if (isTransparent % 2 == 0)
                    {
                        glyph[num / 8] = (uint)((glyph[num / 8] << 4) + co);
                        num++;
                    } else
                    {
                        for (int count = 0; count < co + 1; count++)
                        {
                            if (num >= 256)
                            {
                                break;
                            }
                            glyph[num / 8] <<= 4;
                            num++;
                        }
                    }
                    if (num >= 256)
                    {
                        break;
                    }
                    isTransparent /= 2;
                }
                i++;
            }
            return glyph;
        }
        public static byte[] WeirdGlyphRecipher(byte[] glyph)
        {
            //something wrong
            int counter = 0;
            List<bool> transBits = new List<bool>();
            List<int> ciphered = new List<int>();
            List<byte> combined = new List<byte>();
            byte[] fourbits = new byte[256];

            // converting 4-bytes to 4-bits
            for (int i = 0; i < glyph.Length / 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    fourbits[j + 8 * i] = (byte)((BitConverter.ToUInt32(glyph.Skip(4 * i).Take(4).ToArray()) >> (28 - 4 * j)) % 16);
                }
            }
            for (int i = 0; i < 256; i++)
            {
                if (fourbits[i] == 0) // transparent case
                {
                    counter++;
                    if (counter >= 16 || i >= 255) // fully counted
                    {
                        transBits.Add(true);
                        ciphered.Add(counter - 1);
                        counter = 0;
                    }
                } else
                {
                    if (i > 0) //not on initial position...
                    {
                        if (fourbits[i - 1] == 0 && counter > 0) //... and the preceding pixel is transparent then
                        {
                            transBits.Add(true);
                            ciphered.Add(counter - 1);
                            counter = 0;
                        }
                    }
                    transBits.Add(false);
                    ciphered.Add(fourbits[i]);
                }
            }

            while(transBits.Count % 8 != 0)
            {
                transBits.Add(false);
            }

            while (true)
            {
                if (combined.Count % 5 == 0)
                {
                    int i = combined.Count / 5;
                    if (i * 8 >= transBits.Count())
                    {
                        break;
                    }
                    int bite = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        bite += (transBits[8 * i + j] ? 1 : 0) << j;
                    }
                    combined.Add((byte)bite);
                } else
                {
                    int i = (combined.Count % 5 - 1) + (combined.Count / 5 * 4);
                    if (2 * i >= ciphered.Count)
                    {
                        break;
                    } else if (2 * i == ciphered.Count - 1)
                    {
                        int bite = ciphered[2 * i] * 16;
                        combined.Add((byte)bite);
                    } else
                    {
                        int bite = ciphered[2 * i] * 16 + ciphered[2 * i + 1];
                        combined.Add((byte)bite);
                    }
                }
            }
            return combined.ToArray();
        }

        public static void From2bppFont(string path, int width)
        {
            Console.WriteLine("TBA");
        }

        public static void To2bppFont(string path, int width)
        {
            Console.WriteLine("TBA");
        }
        public static void Interactive()
        {
            while (true)
            {
                Console.WriteLine("What do you want? (Ctrl+C to exit)");
                Console.WriteLine("d: Decipher a 4bpp font file (talk, alpha)");
                Console.WriteLine("r: Recipher a 4bpp font file");
                Console.WriteLine("x: Extract from a 2bpp font file (sys_agb, sys_wars) -- TBA"); // Code is only written in my local computer; agb is 12x16, wars is 8x16
                Console.WriteLine("b: Build a 2bpp font file -- TBA");
                string func = Console.ReadLine();
                string path;
                int w;
                switch (func)
                {
                    case "d":
                        Console.WriteLine("Write down file name or path:");
                        path = Console.ReadLine().Trim('"');
                        FromWeirdFont(path);
                        return;
                    case "r":
                        Console.WriteLine("Write down file name or path:");
                        path = Console.ReadLine().Trim('"');
                        ToWeirdFont(path);
                        return;
                    case "x":
                        Console.WriteLine("Write down file name or path:");
                        path = Console.ReadLine().Trim('"');
                        if (path == "sys_agb")
                        {
                            w = 12;
                        } else if (path == "sys_wars")
                        {
                            w = 8;
                        } else
                        {
                            Console.WriteLine("What would be the width of each glyph? (sys_agb is 12, sys_wars is 8)");
                            w = int.Parse(Console.ReadLine());
                        }
                        From2bppFont(path, w);
                        return;
                    case "b":
                        Console.WriteLine("Write down file name or path:");
                        path = Console.ReadLine().Trim('"');
                        if (path.Replace(".enc","") == "sys_agb")
                        {
                            w = 12;
                        }
                        else if (path.Replace(".enc", "") == "sys_wars")
                        {
                            w = 8;
                        }
                        else
                        {
                            Console.WriteLine("What is the width of each glyph? (sys_agb is 12, sys_wars is 8)");
                            w = int.Parse(Console.ReadLine());
                        }
                        From2bppFont(path, w);
                        return;
                    default:
                        Console.WriteLine("Wrong input received. Try again.");
                        break;
                }
            }
        }
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Interactive();
            }
            else
            {
                try
                {
                    switch (args[0])
                    {
                        case "-d":
                            FromWeirdFont(args[1]);
                            break;
                        case "-r":
                            ToWeirdFont(args[1]);
                            break;
                        case "-x":
                            From2bppFont(args[1], int.Parse(args[2]));
                            break;
                        case "-b":
                            To2bppFont(args[1], int.Parse(args[2]));
                            break;
                        default:
                            Console.WriteLine("Unrecognizable arguments received. Trying to be interactive.");
                            Interactive();
                            break;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Insufficient arguments received. Trying to be interactive.");
                    Interactive();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
