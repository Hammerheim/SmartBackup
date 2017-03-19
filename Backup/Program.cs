//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Vibe.Hammer.SmartBackup;

//namespace Backup
//{
//  class Program
//  {
//    private static bool shallowScanComplete;

//    static void Main(string[] args)
//    {
//      Console.WriteLine("Running...");
//      ShallowScan();
//      Console.ReadKey();
//    }

//    private static async Task ShallowScan()
//    {
//      Console.WriteLine("Building source dictionary using shallow scan...");
//      var runner = new Runner();
//      shallowScanComplete = false;
//      var result = await runner.Run(new DirectoryInfo(@"c:\test\"), new DirectoryInfo(@"L:\Test\"), (progress) => Console.WriteLine(progress), false);
//      await result.Save(@"E:\deleteme\x.sbLog");
//      Console.WriteLine("Done");
//    }

//    private static async Task DeepScan()
//    {
//      Console.WriteLine("Building source dictionary using deep scan...");
//      Console.WriteLine("This will take some time...");
//      var runner = new Runner();
//      shallowScanComplete = false;
//      var result = await runner.Run(new DirectoryInfo(@"c:\test\"), new DirectoryInfo(@"L:\Test\"), (progress) => Console.WriteLine(progress), true);
//      await result.Save(@"E:\deleteme\x.sbLog");
//      Console.WriteLine("Done");
//    }
//  }
//}
