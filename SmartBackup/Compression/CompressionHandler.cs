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
    public virtual async Task<bool> CompressFile(FileInfo sourceFile, FileInfo targetFile, CompressionModes mode)
    {
      if (mode == CompressionModes.Archive)
        return await CompressFileAsArchive(sourceFile, targetFile);
      else if (mode == CompressionModes.Stream)
        return await CompressFileAsStream(sourceFile, targetFile);
      return false;
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

    public virtual async Task<bool> DecompressFile(FileInfo compressedFile, FileInfo sourceFile, CompressionModes mode)
    {
      if (mode == CompressionModes.Archive)
        return await DecompressFileArchive(compressedFile, sourceFile);
      else if (mode == CompressionModes.Stream)
        return await DecompressFileStream(compressedFile, sourceFile);
      return false;

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

    public virtual Task<bool> DecompressStream(Stream source, Stream result)
    {
      throw new NotImplementedException();
    }

    public virtual Task<bool> DecompressStream(Stream source, Stream result, long offset, long length)
    {
      throw new NotImplementedException();
    }

    public virtual async Task<bool> CompressStream(Stream source, Stream result)
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

    public virtual bool ShouldCompress(FileInfo file)
    {
      return !CompressedFileTypes.IsCompressed(file.Extension);
    }
  }
}
