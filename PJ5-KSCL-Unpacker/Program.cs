using System;
using System.IO;

namespace PJ5_KSCL_Unpacker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "PZ5 KSCL Unpacker by LeHieu - VietHoaGame";
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    string ext = Path.GetExtension(arg).ToLower();
                    FileAttributes attr = File.GetAttributes(arg);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        byte[] result = KSCL.Repack(arg, $"{arg}.kscl");
                        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(arg), $"{arg}-new.kscl"), result);
                        Console.WriteLine($"Imported: {Path.GetFileName(Path.Combine(Path.GetDirectoryName(arg), $"{arg}-new.kscl"))}");
                    }
                    else if (ext == ".kscl")
                    {
                        try
                        {
                            KSCL.Unpack(arg);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                    }
                }
            }
            else
            {
                Console.WriteLine("Please drag and drop files/folder into this tool to unpack/repack.");
            }
            Console.ReadKey();
        }
    }
}
