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
      var archiveFilename = new FileInfo(fullyQualifiedFilename + ".zip");

      if (CompressionTypes.IsCompressed(sourceFile.Extension))
        return false;

      await Task.Run(() =>
      {
        using (FileStream fs = new FileStream(archiveFilename.FullName, FileMode.Create))
        {
          using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
          {
            archive.CreateEntryFromFile(sourceFile.FullName, sourceFile.Name);
          }
        }
      });

      OverwriteFile(sourceFile, archiveFilename);
      return true;
    }

    public bool CompressStream(Stream source, Stream result)
    {
      try
      {
        using (DeflateStream compressionStream = new DeflateStream(result, CompressionMode.Compress))
        {
          source.CopyTo(compressionStream);
        }
        return true;
      }
      catch (Exception err)
      {
        return false;
      }
    }

    private void OverwriteFile(FileInfo fileToOverwrite, FileInfo fileToMove)
    {
      GC.WaitForPendingFinalizers();
      File.Delete(fileToOverwrite.FullName);
      GC.WaitForPendingFinalizers();
      File.Move(fileToMove.FullName, fileToOverwrite.FullName);
    }
  }
}
