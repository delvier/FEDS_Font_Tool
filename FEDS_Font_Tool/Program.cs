using System;
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
            Console.WriteLine("This is currently not implemented. Thank you.");
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
        public static void interactive()
        {
            while (true)
            {
                Console.WriteLine("What do you want? (Ctrl+C to exit)");
                Console.WriteLine("d: Decipher a 4bpp font file (talk, alpha)");
                Console.WriteLine("r: Recipher a 4bpp font file");
                // Console.WriteLine("x: Extract from a 2bpp font file (sys_agb, sys_wars)"); // Code is only written in my local computer; agb is 12x16, wars is 8x16
                // Console.WriteLine("b: Build a 2bpp font file");
                string func = Console.ReadLine();
                string path;
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
                interactive();
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
                        default:
                            Console.WriteLine("Unrecognizable arguments received. Trying to be interactive.");
                            interactive();
                            break;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Insufficient arguments received. Trying to be interactive.");
                    interactive();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
