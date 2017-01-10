using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal static class CompressionTypes
  {
    public static bool IsCompressed(string fileExtension)
    {
      switch (fileExtension.ToLower())
      {
        case ".gz":
        case ".7z":
        case ".s7z":
        case ".ace":
        case ".afa":
        case ".alz":
        case ".apk":
        case ".arj":
        case ".b1":
        case ".cab":
        case ".cfs":
        case ".dar":
        case ".dgc":
        case ".dmg":
        case ".gca":
        case ".lzh":
        case ".lha":
        case ".lzx":
        case ".rar":
        case ".sit":
        case ".sitx":
        case ".tgz":
        case ".bz2":
        case ".tbz2":
        case ".tlz":
        case ".zip":
        case ".zipx":
        case ".zoo":
          return true;
        default:
          return false;
      }
    }
  }
}
