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
    public async Task<FileInfo> CompressFile(FileInfo file)
    {
      var archiveFile = new FileInfo(file.FullName + ".zip");

      await Task.Run(() =>
      {
        using (FileStream fs = new FileStream(archiveFile.FullName, FileMode.Create))
        {
          using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
          {
            archive.CreateEntryFromFile(file.FullName, file.Name);
          }
        }
      });
      
      return archiveFile;
    }

    public async Task<FileInfo> DecompressFile(FileInfo file)
    {
      var targetFile = new FileInfo(Path.GetTempFileName());
      if (targetFile.Exists)
        targetFile.Delete();
      var archiveFilename = new FileInfo(file.FullName);

      await Task.Run(() =>
      {
        using (ZipArchive archive = ZipFile.OpenRead(archiveFilename.FullName))
        {
          var entry = archive.Entries.FirstOrDefault();
          if (entry != null)
            entry.ExtractToFile(targetFile.FullName);
        }
      });
      targetFile.IsReadOnly = false;
      GC.WaitForPendingFinalizers();
      return targetFile;
    }
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
      try
      {
        GC.WaitForPendingFinalizers();
        File.Delete(fileToOverwrite.FullName);
        GC.WaitForPendingFinalizers();
      }
      catch (UnauthorizedAccessException err)
      {
        GC.Collect();
        GC.WaitForPendingFinalizers();
      }
      try
      {
        File.Delete(fileToOverwrite.FullName);
        File.Move(fileToMove.FullName, fileToOverwrite.FullName);
      }
      catch (Exception err)
      {
        throw;
      }
    }

    public bool ShouldCompress(FileInfo file)
    {
      return !CompressionTypes.IsCompressed(file.Extension);
    }
  }
}
