using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal static class ArgumentParser
  {
    public static Arguments Parse(string[] arguments)
    {
      var args = new Arguments();
      if (arguments.Length == 0)
        return args;

      foreach (var paramter in arguments)
      {
        if (IsArgumentPair(paramter))
          HandleArgumentPair(paramter, args);
        else
          HandleSingleArgument(paramter, args);
      }
      return args;
    }

    private static void HandleSingleArgument(string paramter, Arguments args)
    {
      switch (paramter.ToLower())
      {
        case "-backup":
        case "-b":
          args.Actions |= ProgramAction.Backup;
          break;
        case "-maintenance":
        case "-m":
          args.Actions |= ProgramAction.Maintenance;
          break;
        case "-extract":
        case "-e":
          args.Actions |= ProgramAction.Extraction;
          break;
        case "-?":
        case "-help":
          args.PrintHelp = true;
          break;
        case "-debug":
          args.Debug = true;
          break;
        case "-compress":
        case "-c":
          args.Compress = true;
          break;
        case "-validateonextraction":
        case "-ve":
          args.ValidationOnExtraction = true;
          break;
        default:
          throw new ArgumentException(paramter);
      }
    }

    private static void HandleArgumentPair(string parameter, Arguments args)
    {
      var parts = GetParts(parameter);
      switch (parts[0].ToLower())
      {
        case "-target":
        case "-t":
          args.Target = new DirectoryInfo(parts[1]);
          break;
        case "-source":
        case "-s":
          args.Source = new DirectoryInfo(parts[1]);
          break;
        case "-filesize":
        case "-fs":
          args.FileSizeInMB = int.Parse(parts[1]);
          break;
        case "-filenamepattern":
        case "-fp":
          args.FilenamePattern = parts[1];
          break;
        case "-ignoreextensions":
        case "-ie":
          args.IgnoreExtensions = parts[1];
          break;
        default:
          throw new ArgumentException(parameter);
      }
    }

    private static bool IsArgumentPair(string parameter)
    {
      var parts = GetParts(parameter);
      return (parts.Length == 2 && parts.First().Length > 0 && parts.Last().Length > 0);
    }

    private static string[] GetParts(string parameter)
    {
      for (var i = 0; i < parameter.Length; i++)
      {
        if (parameter[i] == ':')
          return new[] { parameter.Substring(0, i), parameter.Substring(i + 1) };
      }
      return new[] { parameter };
    }
  }
}
