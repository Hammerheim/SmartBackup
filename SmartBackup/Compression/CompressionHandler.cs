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

    //public async Task<bool> CompressStream(Stream source, Stream result)
    //{
    //  try
    //  {
    //    using (DeflateStream compressionStream = new DeflateStream(result, CompressionMode.Compress))
    //    {
    //      await source.CopyToAsync(compressionStream);
    //    }
    //    return true;
    //  }
    //  catch (Exception err)
    //  {
    //    return false;
    //  }
    //}

    public async Task<bool> CompressStream(Stream source, Stream result)
    {
      try
      {
        using (DeflateStream compressionStream = new DeflateStream(result, CompressionMode.Compress))
        {
          byte[] buffer = new byte[100000];
          
          bool done = false;
          do
          {
            var read = await source.ReadAsync(buffer, 0, 100000);
            if (read < 100000)
              done = true;
            if (read > 0)
              await compressionStream.WriteAsync(buffer, 0, read);
          } while (!done);
        }
        return true;
      }
      catch (Exception err)
      {
        return false;
      }
    }

    public Task<bool> DecompressStream(Stream source, Stream result)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DecompressStream(Stream source, Stream result, long offset, long length)
    {
      throw new NotImplementedException();
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
