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
    private bool Initialized = false;
    private int ID;

    public BackupTarget(IHasher primaryHasher, ICompressionHandler compressionHandler)
    {
      this.primaryHasher = primaryHasher;
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

    public async Task<string> CalculatePrimaryHash(ContentCatalogueBinaryEntry entry) => await CalculatePrimaryHash(entry, binaryHandler);

    private async Task<string> CalculatePrimaryHash(ContentCatalogueBinaryEntry entry, IBinaryHandler handler)
    {
      var tempFile = await handler.ExtractFile(entry);

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

    public async Task<bool> VerifyContent(ContentCatalogueBinaryEntry entry)
    {
      entry.PrimaryContentHash = await CalculatePrimaryHash(entry);

      var originalFile = new FileInfo(entry.SourceFileInfo.FullyQualifiedFilename);
      originalFile.Refresh();
      if (originalFile.LastWriteTime == entry.SourceFileInfo.LastModified)
      {
        var originalHash = await primaryHasher.GetHashString(originalFile);
        if (entry.PrimaryContentHash == originalHash)
          return true;
      }
      return false;
    }

    public async Task<bool> Defragment(List<ContentCatalogueEntry> content, IProgress<ProgressReport> progressCallback)
    {
      var entries = content.OfType<ContentCatalogueBinaryEntry>().OrderBy(x => x.TargetOffset).ToArray();
      if (entries.Length == content.Count)
      {
        progressCallback.Report(new ProgressReport($"{ID}: No defragmentation required"));
        return true;
      }
      var tempFile = new FileInfo(Path.GetTempFileName());
      var tempFilename = tempFile.FullName;

      try
      {
        progressCallback.Report(new ProgressReport("Defragmenting file"));
        long newTail = await MoveBinariesToTempFile(entries, tempFile, progressCallback);
        if (newTail < 0)
        {
          progressCallback.Report(new ProgressReport("Failed to defragment file."));
          return false;
        }

        if (newTail == tail)
        {
          progressCallback.Report(new ProgressReport($"{ID}: No defragmentation required"));
          return true;
        }

        await WaitForFileRelease();

        progressCallback.Report(new ProgressReport("Validating defragmented file..."));

        if (!await ValidateDefragmentedFile(tempFile, entries, progressCallback))
          return false;

        await WaitForFileRelease();

        if (!binaryHandler.SwapFiles(tempFile))
          return false;

        ReportDefragmentationResult(progressCallback, newTail);

        tail = newTail;
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

    private async Task<bool> ValidateDefragmentedFile(FileInfo tempFile, ContentCatalogueBinaryEntry[] entries, IProgress<ProgressReport> progressCallback)
    {
      var currentFile = 0;
      var time = DateTime.Now;
      var numberOfFiles = entries.Length;
      var tempBinaryHandler = new BinaryHandler(tempFile, new CompressionHandler());

      try
      {
        for (var i = 0; i < entries.Length; i++)
        {
          try
          {
            var hash = await CalculatePrimaryHash(entries[i], tempBinaryHandler);
            if (hash != entries[i].PrimaryContentHash)
            {
              progressCallback.Report(new ProgressReport("File verification failed", i, numberOfFiles));
              return false;
            }
            currentFile++;
            if (DateTime.Now - time > TimeSpan.FromSeconds(5))
            {
              progressCallback.Report(new ProgressReport(entries[i].SourceFileInfo.FileName, i, numberOfFiles));
              time = DateTime.Now;
            }
          }
          catch
          {
            progressCallback.Report(new ProgressReport("Catastrofic error in file validation. Dropping it like it's hot"));
            return false;
          }
        }
      }
      finally
      {
        if (tempBinaryHandler != null)
        {
          tempBinaryHandler.CloseStream();
          tempBinaryHandler = null;
          GC.WaitForPendingFinalizers();
        }
      }
      return true;
    }

    private void ReportDefragmentationResult(IProgress<ProgressReport> progressCallback, long newTail)
    {
      if (tail > newTail)
      {
        progressCallback.Report(new ProgressReport($"{ID}: Defragmentation released {(tail - newTail) / (1024 * 1024)} MB in the file"));
      }
      else
        progressCallback.Report(new ProgressReport($"{ID}: No space was reclaimed"));
    }

    private async Task WaitForFileRelease()
    {
      GC.WaitForPendingFinalizers();
      await Task.Delay(500);
    }

    private async Task<long> MoveBinariesToTempFile(ContentCatalogueBinaryEntry[] entries, FileInfo tempFile, IProgress<ProgressReport> progressCallback)
    {
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
              entry.TargetOffset = currentOffset;
              currentOffset += entry.TargetLength;
            }
            else
            {
              return -1;
            }

            if (DateTime.Now - time > TimeSpan.FromSeconds(5))
            {
              progressCallback.Report(new ProgressReport($"{ID}: Moving {i} of {entries.Length} entries", i, entries.Length));
              time = DateTime.Now;
            }
          }
          binaryHandler.CloseStream();
          return currentOffset;
        }
      }
      catch (Exception err)
      {
        progressCallback.Report(new ProgressReport(err.Message));
        return -1;
      }
      finally
      {
        
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
