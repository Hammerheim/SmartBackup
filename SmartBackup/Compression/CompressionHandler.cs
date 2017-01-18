using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Compression
{
  internal class CompressionHandler : ICompressionHandler
  {
    public async Task<bool> CompressFile(string fullyQualifiedFilename)
    {
      var sourceFile = new FileInfo(fullyQualifiedFilename);
      var archiveFilename = fullyQualifiedFilename + ".zip";

      if (CompressionTypes.IsCompressed(sourceFile.Extension))
        return false;

      await Task.Run(() =>
      {
        using (FileStream fs = new FileStream(archiveFilename, FileMode.Create))
        {
          using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
          {
            archive.CreateEntryFromFile(sourceFile.FullName, sourceFile.Name);
          }
        }
      });
      GC.WaitForPendingFinalizers();
      File.Delete(sourceFile.FullName);
      GC.WaitForPendingFinalizers();
      File.Move(archiveFilename, fullyQualifiedFilename);

      return true;
    }
  }
}
