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
      try
      {
        var arguments = ArgumentParser.Parse(args);
        if (arguments.PrintHelp)
        {
          PrintOptions();
          return;
        }

        if (arguments.ShouldBackup)
        {
          MainBackupAsync(arguments.Source, arguments.Target, arguments.FileSize).Wait();
        }
        if (arguments.ShouldMaintain)
        {
          MainMaintenanceAsync(arguments.Target, arguments.FileSize).Wait();
        }

        if (arguments.ShouldExtract)
        {
          MainExtractorAsync(arguments.Source, arguments.Target, arguments.FileSize).Wait();
        }
        Console.WriteLine("Done");
      }
      catch (ArgumentException err)
      {
        Console.WriteLine($"Invalid parameter: {err.ParamName}");
        PrintOptions();
        return;
      }
    }

    private static void PrintOptions()
    {
      Console.WriteLine(@"SmartBackup can be run either using the stand-alone executable or by executing a file containing a backup.");
      Console.WriteLine(@"The following options are avilable:");
      Console.WriteLine(@"Actions: (At least one must be specified");
      Console.WriteLine(@"-BackUp:      Perform backup");
      Console.WriteLine(@"-Maintenance: Scan the files for dublicates and reclaim space. Note: Files on disk is not shrunk.");
      Console.WriteLine(@"-Extract:     Extract all files");
      Console.WriteLine(@"");
      Console.WriteLine(@"Options");
      Console.WriteLine(@"-source:<Directory> Specify the source directory to backup from");
      Console.WriteLine(@"-target:<Directory> Specify where to store files when backing up or find the files for maintenance");
      Console.WriteLine(@"-FileSize:<integer> Specify the size of backup catalogues in mega bytes. Default: 1024");
      Console.WriteLine(@"");
      Console.WriteLine(@"Examples:");
      Console.WriteLine(@"SmartBackup -b -m -s:c:\test -t:e:\backup -fs:1024");
      Console.WriteLine(@"This command runs a backup using the c:\Test directory as the source and storing the files in e:\backup");
      Console.WriteLine(@"using a catalogue size of 1024 MB (1 GB)");
      Console.WriteLine(@"");
      Console.WriteLine(@"SmartBackup -e -s:e:\backup -t:c:\test");
      Console.WriteLine(@"This command extracts all files found in catalogues in the e:\backup directory and writing the files");
      Console.WriteLine(@"to the c:\test directory");
      Console.WriteLine(@"You must specify an action: -extract or -backup");
      Console.WriteLine(@"Use: -extract -target:path");
      Console.WriteLine(@"Use: -backup -source:path -target:path");
    }

    private static async Task MainExtractorAsync(DirectoryInfo source, DirectoryInfo target, int fileSize)
    {
      if (source.FullName == string.Empty)
      {
        var assemblyFile = new FileInfo(ConvertUriStylePathToNative(Assembly.GetEntryAssembly().CodeBase));
        source = assemblyFile.Directory;
      }
      var catalogue = new ContentCatalogue();
      
      await catalogue.BuildFromExistingBackups(source, fileSize);

      Console.WriteLine($"Extracting files from {source} to {target.FullName}");
      var callbackObject = new Callback();
      await catalogue.ExtractAll(target, new Progress<ProgressReport>(callbackObject.ProgressCallback));
    }

    private static async Task MainMaintenanceAsync(DirectoryInfo target, int fileSize)
    {
      var callbackObject = new Callback();

      Console.WriteLine("Starting maintenance run...");
      var runner = new Runner(target);
      await runner.CalculateMissingHashes(target, fileSize, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      await runner.ReplaceDublicatesWithLinks(target, fileSize, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      await runner.DefragmentBinaries(target, fileSize, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      Console.WriteLine("Done");
    }
    private static string ConvertUriStylePathToNative(string path)
    {
      path = path.Substring(8);
      path = WebUtility.UrlDecode(path.ToString());
      path = path.Replace('/', '\\');
      return path;
    }
    private static async Task MainBackupAsync(DirectoryInfo source, DirectoryInfo target, int fileSize)
    {
      await ShallowScan(source, target, fileSize);
    }

    private static async Task ShallowScan(DirectoryInfo source, DirectoryInfo target, int fileSize)
    {
      var callbackObject = new Callback();

      Console.WriteLine("Building source dictionary using shallow scan...");
      var runner = new Runner(target);
      shallowScanComplete = false;
      var result = await runner.Scan(source, target, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      if (result != null)
      {
        await runner.Backup(result, target, fileSize, new Progress<ProgressReport>(callbackObject.ProgressCallback));
        await runner.IdentifyDeletedFiles(target, fileSize, new Progress<ProgressReport>(callbackObject.ProgressCallback));
      }

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
