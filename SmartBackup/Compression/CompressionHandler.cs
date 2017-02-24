using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup.Compression
{
  internal class CompressionHandler : ICompressionHandler
  {
    public virtual async Task<bool> CompressFile(FileInfo sourceFile, FileInfo targetFile)
    {
      return await CompressFileAsStream(sourceFile, targetFile);
    }

    private async Task<bool> CompressFileAsStream(FileInfo sourceFile, FileInfo targetFile)
    {
      try
      {
        using (var targetStream = targetFile.OpenWrite())
        {
          using (var sourceStream = sourceFile.OpenRead())
          {
            using (var compressionStream = new DeflateStream(targetStream, CompressionMode.Compress))
            {
              await sourceStream.CopyToAsync(compressionStream);
            }
          }
        }
        return true;
      }
      catch (Exception err)
      {
        throw new FileCompressionAsStramFailedException(sourceFile.FullName, err);
      }
    }

    private async Task<bool> CompressFileAsArchive(FileInfo sourceFile, FileInfo targetFile)
    {
      try
      {
        await Task.Run(() =>
        {
          using (FileStream fs = new FileStream(targetFile.FullName, FileMode.Create))
          {
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
              archive.CreateEntryFromFile(sourceFile.FullName, sourceFile.Name);
            }
          }
        });
        return true;
      }
      catch (Exception err)
      {
        throw new FileCompressionAsArchiveFailedException(sourceFile.FullName, err);
      }
    }

    public virtual async Task<bool> DecompressFile(FileInfo compressedFile, FileInfo sourceFile)
    {
      return await DecompressFileStream(compressedFile, sourceFile);
    }

    private async Task<bool> DecompressFileStream(FileInfo compressedFile, FileInfo targetFile)
    {
      try
      {
        using (var targetStream = targetFile.Create())
        {
          using (var sourceStream = compressedFile.OpenRead())
          {
            using (DeflateStream compressedStream = new DeflateStream(sourceStream, CompressionMode.Decompress))
            {
              await compressedStream.CopyToAsync(targetStream);
            }
          }
        }
        targetFile.IsReadOnly = false;
        GC.WaitForPendingFinalizers();
        return true;
      }
      catch (Exception err)
      {
        throw new DecompressionOfStreamFailedException(targetFile.FullName, err);
      }
    }


    private async Task<bool> DecompressFileArchive(FileInfo compressedFile, FileInfo targetFile)
    {
      try
      {
        await Task.Run(() =>
        {
          using (ZipArchive archive = ZipFile.OpenRead(compressedFile.FullName))
          {
            var entry = archive.Entries.FirstOrDefault();
            if (entry != null)
              entry.ExtractToFile(targetFile.FullName);
          }
        });
        targetFile.IsReadOnly = false;
        GC.WaitForPendingFinalizers();
        return true;
      }
      catch (Exception err)
      {
        throw new DecompressionOfStreamFailedException(targetFile.FullName, err);
      }
    }

    public virtual bool ShouldCompress(FileInfo file)
    {
      return !CompressedFileTypes.IsCompressed(file.Extension);
    }
  }
}
