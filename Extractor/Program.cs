using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;

namespace Extractor
{
  class Program
  {
    static void Main(string[] args)
    {
      DirectoryInfo target;

      if (args.Length == 0)
      {
        Console.WriteLine("You must specify either an extraction location or verify that ");
        Console.WriteLine("you want to extract to the original location");
        Console.WriteLine("Use: -target:<path>|-useOriginalTarget");
        return;
      }
      if (args.Length > 1)
      {
        Console.WriteLine("You must specify either an extraction location or verify that ");
        Console.WriteLine("you want to extract to the original location, but not both");
        Console.WriteLine("Use: -target:<path>|-useOriginalTarget");
        return;
      }
      if (args[0].ToLower().StartsWith("-target:"))
      {
        var value = args[0].Substring(8);
        value = value.Replace('\\', ' ');
        value = value.Trim();
        target = new DirectoryInfo(value);
        MainAsync(target).Wait();
        Console.WriteLine("Done");
        return;
      }

      Console.WriteLine("You must specify either an extraction location or verify that ");
      Console.WriteLine("you want to extract to the original location, but not both");
      Console.WriteLine("Use: -target:<path>|-useOriginalTarget");
    }

    private static async Task MainAsync(DirectoryInfo target)
    {
      var catalogue = new ContentCatalogue();
      await catalogue.BuildFromExistingBackups(new DirectoryInfo("L:\\Test\\"), 1024);

      foreach (var key in catalogue.GetUniqueFileKeys())
      {
        var file = catalogue.GetNewestVersion(key);
        await catalogue.ExtractFile(file, target);
      }
    }
  }
}
