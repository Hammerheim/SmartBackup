using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup
{
  /// <summary>
  /// This class does the job for testing. Must be rewritten
  /// </summary>
  class Program
  {
    // -backup -source:"C:\Test" -target:"E:\Test"
    // -extract -target:"L:\test2"
    // -maintenance -target:e:\test
    private static bool shallowScanComplete;
    static void Main(string[] args)
    {

      DirectoryInfo source;
      DirectoryInfo target;

      if (args.Length < 2)
      {
        Console.WriteLine("You must specify an action: -extract or -backup");
        Console.WriteLine("Use: -extract -target:path");
        Console.WriteLine("Use: -backup -source:path -target:path");
        return;
      }

      if (args[0].ToLower().StartsWith("-backup"))
      {
        if (args[1].ToLower().StartsWith("-source:"))
        {
          var value = args[1].Substring(8);
          value = value.Replace('"', ' ');
          value = value.Trim();
          source = new DirectoryInfo(value);
        }
        else
        {
          Console.WriteLine("You must specify an action: -extract or -backup");
          Console.WriteLine("Use: -extract -target:path");
          Console.WriteLine("Use: -backup -source:path");
          return;
        }
        if (args[2].ToLower().StartsWith("-target:"))
        {
          var value = args[2].Substring(8);
          value = value.Replace('"', ' ');
          value = value.Trim();
          target = new DirectoryInfo(value);
        }
        else
        {
          Console.WriteLine("You must specify an action: -extract or -backup");
          Console.WriteLine("Use: -extract -target:path");
          Console.WriteLine("Use: -backup -source:path");
          return;
        }
        MainBackupAsync(source, target).Wait();
        Console.WriteLine("Done");
        return;
      }
      else if (args[0].ToLower().StartsWith("-extract"))
      {
        if (args[1].ToLower().StartsWith("-target:"))
        {
          var value = args[1].Substring(8);
          value = value.Replace('"', ' ');
          value = value.Trim();
          target = new DirectoryInfo(value);
          MainExtractorAsync(target).Wait();
          Console.WriteLine("Done");
          return;
        }
      }
      else if (args[0].ToLower().StartsWith("-maintenance"))
      {
        if (args[1].ToLower().StartsWith("-target:"))
        {
          var value = args[1].Substring(8);
          value = value.Replace('"', ' ');
          value = value.Trim();
          target = new DirectoryInfo(value);
          MainMaintenanceAsync(target).Wait();
          Console.WriteLine("Done");
          return;
        }
      }
    }

    private static async Task MainExtractorAsync(DirectoryInfo target)
    {
      var catalogue = new ContentCatalogue();
      var assemblyFile = new FileInfo(ConvertUriStylePathToNative(Assembly.GetEntryAssembly().CodeBase));
      await catalogue.BuildFromExistingBackups(assemblyFile.Directory, 1024);

      Console.WriteLine($"Extracting files from {assemblyFile.Directory} to {target.FullName}");
      var callbackObject = new Callback();
      await catalogue.ExtractAll(target, new Progress<ProgressReport>(callbackObject.ProgressCallback));
    }

    private static async Task MainMaintenanceAsync(DirectoryInfo target)
    {
      var callbackObject = new Callback();

      Console.WriteLine("Starting maintenance run...");
      var runner = new Runner(target);
      await runner.CalculateMissingHashes(target, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      await runner.ReplaceDublicatesWithLinks(target, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      await runner.DefragmentBinaries(target, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      Console.WriteLine("Done");
    }
    private static string ConvertUriStylePathToNative(string path)
    {
      path = path.Substring(8);
      path = WebUtility.UrlDecode(path.ToString());
      path = path.Replace('/', '\\');
      return path;
    }
    private static async Task MainBackupAsync(DirectoryInfo source, DirectoryInfo target)
    {
      await ShallowScan(source, target);
    }

    private static async Task ShallowScan(DirectoryInfo source, DirectoryInfo target)
    {
      var callbackObject = new Callback();

      Console.WriteLine("Building source dictionary using shallow scan...");
      var runner = new Runner(target);
      shallowScanComplete = false;
      var result = await runner.Scan(source, target, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      if (result != null)
        await runner.Backup(result, target, new Progress<ProgressReport>(callbackObject.ProgressCallback));

      Console.WriteLine("Done");
    }

    private class Callback
    {
      public void ProgressCallback(ProgressReport progress)
      {
        if (progress.ExpectedNumberOfActions > 0)
          Console.WriteLine($"{Math.Round(progress.CurrentActionNumber / (double)progress.ExpectedNumberOfActions * 100)}% {progress.AdditionalInfo}");
        else
          Console.WriteLine(progress.AdditionalInfo);
      }
    }
  }
}
