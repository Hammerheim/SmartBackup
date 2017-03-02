using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Xml.Serialization;
using System.Xml;
using Vibe.Hammer.SmartBackup.Compression;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Progress;
using Vibe.Hammer.SmartBackup.Target;

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTarget : IBackupTarget
  {
    private IBinaryHandler binaryHandler;
    private long maxLength;
    private long tail = BackupTargetConstants.DataOffset;
    private string filename;
    private ICompressionHandler compressionHandler;
    private IHasher primaryHasher;
    private IHasher secondaryHasher;
    private bool Initialized = false;
    private int ID;

    public BackupTarget(IHasher primaryHasher, IHasher secondaryHasher, ICompressionHandler compressionHandler)
    {
      this.primaryHasher = primaryHasher;
      this.secondaryHasher = secondaryHasher;
      this.compressionHandler = compressionHandler;
    }
    public long Tail
    {
      get
      {
        EnsureInitialized();
        return tail;
      }
    }

    public int TargetId => ID;

    public async Task<ContentCatalogueBinaryEntry> AddFile(FileInformation file, int version, bool compressIfPossible)
    {
      EnsureInitialized();
      return await InsertFile(file, version, compressIfPossible);
    }

    public bool CanContain(FileInformation file)
    {
      EnsureInitialized();
      return (tail + file.Size < maxLength);
    }

    public virtual void Initialize(int maxLengthInMegaBytes, DirectoryInfo backupDirectory, int id, long tail, string filenamePattern)
    {
      if (string.IsNullOrWhiteSpace(filenamePattern))
        filenamePattern = BackupTargetConstants.DefaultBackupTargetName;

      ID = id;
      this.maxLength = maxLengthInMegaBytes * BackupTargetConstants.MegaByte;
      filename = Path.Combine(backupDirectory.FullName, $"{filenamePattern}.{id}.exe");
      backupDirectory.Refresh();
      if (!backupDirectory.Exists)
        backupDirectory.Create();
      binaryHandler = new BinaryHandler(new FileInfo(filename), compressionHandler);
      this.tail = tail;
      Initialized = true;
    }

    public async Task ExtractFile(ContentCatalogueBinaryEntry file, DirectoryInfo extractionRoot)
    {
      var targetFile = new FileInfo(Path.Combine(extractionRoot.FullName, file.SourceFileInfo.RelativePath, file.SourceFileInfo.FileName));
      await ExtractFile(file, targetFile, file.SourceFileInfo.LastModified);
    }

    public async Task ExtractLinkedFile(ContentCatalogueBinaryEntry binaryFile, ContentCatalogueLinkEntry linkFile, DirectoryInfo extractionRoot)
    {
      var targetFile = new FileInfo(Path.Combine(extractionRoot.FullName, linkFile.SourceFileInfo.RelativePath, linkFile.SourceFileInfo.FileName));
      await ExtractFile(binaryFile, targetFile, linkFile.SourceFileInfo.LastModified);
    }

    private async Task ExtractFile(ContentCatalogueBinaryEntry binaryToExtract, FileInfo targetFile, DateTime originalWriteTime)
    {
      targetFile.Refresh();
      if (targetFile.Exists && targetFile.LastWriteTime >= originalWriteTime)
        return;

      var extractedFile = await binaryHandler.ExtractFile(binaryToExtract);
      if (extractedFile != null)
      {
        targetFile.Directory.Create();
        if (binaryToExtract.Compressed)
        {
          try
          {
            SafeOverwriteFile(extractedFile, targetFile, originalWriteTime, () => compressionHandler.DecompressFile(extractedFile, targetFile));
          }
          finally
          {
            extractedFile.Delete();
          }
        }
        else
        {
          SafeOverwriteFile(extractedFile, targetFile, originalWriteTime, () => File.Move(extractedFile.FullName, targetFile.FullName));
        }
        File.SetLastWriteTime(targetFile.FullName, originalWriteTime);
        GC.WaitForPendingFinalizers();
      }
    }

    private void SafeOverwriteFile(FileInfo sourceFile, FileInfo targetFile, DateTime originalWriteTime, Action performMoveAction)
    {
      targetFile.Refresh();
      if (targetFile.Exists)
      {
        if (targetFile.LastWriteTime < originalWriteTime)
        {
          var lastWriteTime = targetFile.LastWriteTime;
          var movedFile = new FileInfo(targetFile.FullName + ".tmp");
          File.Move(targetFile.FullName, movedFile.FullName);
          try
          {
            GC.WaitForPendingFinalizers();
            performMoveAction();
          }
          catch
          {
            movedFile.MoveTo(targetFile.FullName);
            File.SetLastWriteTime(targetFile.FullName, lastWriteTime);
            return;
          }
          finally
          {
            File.Delete(movedFile.FullName);
          }
        }
      }
      else
        sourceFile.MoveTo(targetFile.FullName);
    }

    public async Task<string> CalculatePrimaryHash(ContentCatalogueBinaryEntry entry)
    {
      var tempFile = await binaryHandler.ExtractFile(entry);

      tempFile.Refresh();
      if (tempFile.Exists)
      {
        try
        {
          return await primaryHasher.GetHashString(tempFile);
        }
        finally
        {
          tempFile.Delete();
        }
      }
      return string.Empty;
    }

    public async Task<string> CalculateSecondaryHash(ContentCatalogueBinaryEntry entry)
    {
      var tempFile = await binaryHandler.ExtractFile(entry);

      if (tempFile.Exists)
      {
        try
        {
          return await secondaryHasher.GetHashString(tempFile);
        }
        finally
        {
          tempFile.Delete();
        }
      }
      return string.Empty;
    }

    public async Task<bool> Defragment(List<ContentCatalogueBinaryEntry> binariesToMove, IProgress<ProgressReport> progressCallback)
    {
      var entries = binariesToMove.OrderBy(x => x.TargetOffset).ToArray();
      var tempFile = new FileInfo(Path.GetTempFileName());
      var tempFilename = tempFile.FullName;

      try
      {
        long currentOffset = BackupTargetConstants.DataOffset;
        using (var outputStream = tempFile.Create())
        {
          var time = DateTime.Now;
          for (int i = 0; i < entries.Length; i++)
          {
            var entry = entries[i];
            if (await binaryHandler.CopyBytesToStreamAsync(outputStream, entry.TargetOffset, entry.TargetLength))
            {
              currentOffset += entry.TargetLength;
            }

            if (DateTime.Now - time > TimeSpan.FromSeconds(5))
            {
              progressCallback.Report(new ProgressReport($"Moving {i} of {entries.Length}", i, entries.Length));
              time = DateTime.Now;
            }
          }

          binaryHandler.CloseStream();
        }
        GC.WaitForPendingFinalizers();
        await Task.Delay(500);
        if (binaryHandler.SwapFiles(tempFile))
        {
          for (int i = 0; i < entries.Length; i++)
          {
            if (i == 0)
              entries[i].TargetOffset = 0;
            else
              entries[i].TargetOffset = entries[i - 1].TargetOffset + entries[i - 1].TargetLength;
          }
          if (tail > currentOffset)
          {
            progressCallback.Report(new ProgressReport($"Defragmentation left {(tail / (1024 * 1024)) - (currentOffset / (1024 * 1024))} MB available in the file"));
          }
          else
            progressCallback.Report(new ProgressReport("No space was reclaimed"));
          tail = currentOffset;
        }
        return true;
      }
      catch (Exception err)
      {
        throw;
      }
      finally
      {
        SafeDeleteTemporaryFile(tempFilename);
      }
    }

    public void CloseStream()
    {
      binaryHandler.CloseStream();
    }

    private async Task<ContentCatalogueBinaryEntry> InsertFile(FileInformation file, int version, bool compressIfPossible)
    {
      var item = new ContentCatalogueBinaryEntry
      {
        SourceFileInfo = file,
        TargetOffset = tail,
        Deleted = false,
        Version = version
      };

      var sourceFile = new FileInfo(file.FullyQualifiedFilename);
      bool insertResult = false;

      var shouldCompress = compressionHandler.ShouldCompress(sourceFile);
      if (shouldCompress && compressIfPossible)
      {
        var tempFile = new FileInfo(Path.GetTempFileName());
        if (!await compressionHandler.CompressFile(sourceFile, tempFile))
          throw new AddFileToArchiveException("Unable to compress file");

        if (tempFile.Length < sourceFile.Length)
        {
          item.Compressed = true;
          item.TargetLength = GetFileSize(tempFile);
          insertResult = await InsertBinary(item, tempFile);
          GC.WaitForPendingFinalizers();
          tempFile.Delete();
        }
        else
        {
          // Note: Compression caused the file to expand. This would be caused by files that cannot be compressed, like very small files or encrypted files.
          item.Compressed = false;
          item.TargetLength = GetFileSize(sourceFile);
          insertResult = await InsertBinary(item, sourceFile);
          tempFile.Delete();
        }
      }
      else
      {
        item.TargetLength = GetFileSize(sourceFile);
        insertResult = await InsertBinary(item, sourceFile);
      }
      return insertResult ? item : null;
    }

    private async Task<bool> InsertBinary(ContentCatalogueBinaryEntry item, FileInfo sourceFile)
    {
      var success = await binaryHandler.InsertFile(item, sourceFile);
      if (success)
        tail = item.TargetOffset + item.TargetLength;
      return success;
    }

    private long GetFileSize(FileInfo file)
    {
      return file.Length;
    }

    private void EnsureInitialized()
    {
      if (!Initialized)
        throw new BackupTargetNotInitializedException();
    }

    private static void SafeDeleteTemporaryFile(string tempFilename)
    {
      if (File.Exists(tempFilename))
      {
        try
        {
          File.Delete(tempFilename);
        }
        catch
        {

        }
      }
    }
  }
}
