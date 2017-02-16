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

    public int TargetId { get; set; }

    public async Task<ContentCatalogueBinaryEntry> AddFile(FileInformation file, int version)
    {
      EnsureInitialized();
      return await InsertFile(file, version);
    }

    private async Task<ContentCatalogueBinaryEntry> InsertFile(FileInformation file, int version)
    {
      var item = new ContentCatalogueBinaryEntry
      {
        SourceFileInfo = file,
        TargetOffset = tail,
        Deleted = false,
        Version = version
      };

      var sourceFile = new FileInfo(file.FullyQualifiedFilename);

      item.Compressed = compressionHandler.ShouldCompress(sourceFile);
      if (item.Compressed)
      {
        var tempFile = new FileInfo(Path.GetTempFileName());
        if (!await compressionHandler.CompressFile(sourceFile, tempFile, CompressionModes.Stream))
          throw new AddFileToArchiveException("Unable to compress file");

        item.TargetLength = GetFileSize(tempFile);
        await InsertBinary(item, tempFile);
        GC.WaitForPendingFinalizers();
        tempFile.Delete();

      }
      else
      {
        item.TargetLength = GetFileSize(sourceFile);
        await InsertBinary(item, sourceFile);
      }
      return item;
    }

    private async Task InsertBinary(ContentCatalogueBinaryEntry item, FileInfo sourceFile)
    {
      var success = await binaryHandler.InsertFile(item, sourceFile);
      if (success)
      {
        tail = item.TargetOffset + item.TargetLength;
      }
    }

    private long GetFileSize(FileInfo file)
    {
      return file.Length;
    }

    public bool CanContain(FileInformation file)
    {
      EnsureInitialized();
      return (tail + file.Size < maxLength);
    }

    public virtual void Initialize(int maxLengthInMegaBytes, DirectoryInfo backupDirectory, int id, long tail)
    {
      ID = id;
      this.maxLength = maxLengthInMegaBytes * BackupTargetConstants.MegaByte;
      filename = Path.Combine(backupDirectory.FullName, $"BackupTarget.{id}.exe");
      if (!backupDirectory.Exists)
        backupDirectory.Create();
      binaryHandler = new BinaryHandler(new FileInfo(filename), compressionHandler);
      this.tail = tail;
      Initialized = true;
    }

    private void EnsureInitialized()
    {
      if (!Initialized)
        throw new BackupTargetNotInitializedException();
    }

    public async Task ExtractFile(ContentCatalogueBinaryEntry file, DirectoryInfo extractionRoot)
    {
      var tempFile = await binaryHandler.ExtractFile(file);
      if (tempFile != null)
      {
        var targetFile = new FileInfo(Path.Combine(extractionRoot.FullName, file.SourceFileInfo.RelativePath, file.SourceFileInfo.FileName));
        targetFile.Directory.Create();
        if (file.Compressed)
        {
          try
          {
            await compressionHandler.DecompressFile(tempFile, targetFile, CompressionModes.Stream);
          }
          finally
          {
            tempFile.Delete();
          }
        }
        else
          tempFile.MoveTo(targetFile.FullName);
        File.SetLastWriteTime(targetFile.FullName, file.SourceFileInfo.LastModified);
        GC.WaitForPendingFinalizers();
      }
    }

    public async Task ExtractLinkedFile(ContentCatalogueBinaryEntry binaryFile, ContentCatalogueLinkEntry linkFile, DirectoryInfo extractionRoot)
    {
      var tempFile = await binaryHandler.ExtractFile(binaryFile);
      if (tempFile != null)
      {
        var targetFile = new FileInfo(Path.Combine(extractionRoot.FullName, linkFile.SourceFileInfo.RelativePath, linkFile.SourceFileInfo.FileName));
        targetFile.Directory.Create();
        if (binaryFile.Compressed)
        {
          try
          {
            await compressionHandler.DecompressFile(tempFile, targetFile, CompressionModes.Stream);
          }
          finally
          {
            tempFile.Delete();
          }
        }
        else
          tempFile.MoveTo(targetFile.FullName);
        File.SetLastWriteTime(targetFile.FullName, linkFile.SourceFileInfo.LastModified);
        GC.WaitForPendingFinalizers();
      }
    }
    public async Task<HashPair> CalculateHashes(ContentCatalogueBinaryEntry entry)
    {
      var tempFile = await binaryHandler.ExtractFile(entry);
      
      if (tempFile.Exists)
      {
        try
        {
          return new HashPair
          {
            PrimaryHash = await primaryHasher.GetHashString(tempFile),
            SecondaryHash = await secondaryHasher.GetHashString(tempFile)
          };
        }
        finally
        {
          tempFile.Delete();
        }
      }
      return null;
    }

    public async Task<bool> ReclaimSpace(List<ContentCatalogueBinaryEntry> binariesToMove, IProgress<ProgressReport> progressCallback)
    {
      var entries = binariesToMove.OrderBy(x => x.TargetOffset).ToArray();

      long currentOffset = BackupTargetConstants.DataOffset;
      var time = DateTime.Now;
      for (int i = 0; i < entries.Length; i++)
      {
        var entry = entries[i];
        if (entry.TargetOffset > currentOffset)
        {
          await binaryHandler.MoveBytes(entry.TargetOffset, entry.TargetLength, currentOffset);
          entry.TargetOffset = currentOffset;
        }
        currentOffset += entry.TargetLength;

        if (DateTime.Now - time > TimeSpan.FromSeconds(5))
        {
          progressCallback.Report(new ProgressReport($"Moving {i} of {entries.Length}", i, entries.Length));
          time = DateTime.Now;
        }
      }
      if (tail > currentOffset)
        progressCallback.Report(new ProgressReport($"Reclaimed {(tail / (1024 * 1024)) - (currentOffset / (1024 * 1024))} MB"));
      else
        progressCallback.Report(new ProgressReport("No space was reclaimed"));
      tail = currentOffset;
      return true;
    }

    private long RecalculateOffsets(ContentCatalogueBinaryEntry[] binaryEntries)
    {
      long temporaryTail = BackupTargetConstants.DataOffset;
      foreach (var entry in binaryEntries)
      {
        entry.TargetOffset = temporaryTail;
        temporaryTail += entry.TargetLength;
      }

      return temporaryTail;
    }

    public void CloseStream()
    {
      binaryHandler.CloseStream();
    }
  }
}
