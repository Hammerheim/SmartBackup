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
    private ContentCatalogue catalogue;
    private IBackupTargetBinaryHandler binaryHandler;
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

    public async Task AddFile(FileInformation file)
    {
      EnsureInitialized();
      await InsertFile(file);
    }

    private async Task InsertFile(FileInformation file)
    {
      var item = new ContentCatalogueBinaryEntry
      {
        SourceFileInfo = file,
        TargetOffset = tail
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
    }

    private async Task InsertBinary(ContentCatalogueBinaryEntry item, FileInfo sourceFile)
    {
      var success = await binaryHandler.InsertFile(item, sourceFile);
      if (success)
      {
        tail = item.TargetOffset + item.TargetLength;
        // update log
        catalogue.SearchTargets[ID].Add(item);
      }
    }

    private async Task EnsureContentCatalogue(ContentCatalogue catalogue)
    {
      this.catalogue = catalogue;
      if (binaryHandler.BinaryFileExists)
      {
        var tempCatalogue = await binaryHandler.ReadContentCatalogue();
        if (tempCatalogue != null)
        {
          if (this.catalogue != null)
            catalogue.EnsureTargetCatalogueExists(tempCatalogue, ID, this);
          else
            catalogue = tempCatalogue;
        }
        else
          throw new InvalidBinaryTargetException(ID);
      }
      else
      {
        catalogue = new ContentCatalogue();
        catalogue.Add(new TargetContentCatalogue(ID, this));
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

    public bool Contains(string key)
    {
      EnsureInitialized();
      return (catalogue.Targets[ID].KeySearchContent.ContainsKey(key));
    }

    public bool Contains(string key, int version)
    {
      EnsureInitialized();
      if (catalogue.Targets[ID].KeySearchContent.ContainsKey(key))
      {
        return catalogue.Targets[ID].KeySearchContent[key].FirstOrDefault(entry => entry.Key == key && entry.Version == version) != null;
      }
      return false;
    }

    public async Task ReadCatalogue()
    {
      EnsureInitialized();
      throw new NotImplementedException();
    }

    public async Task WriteCatalogue(bool closeStreams)
    {
      EnsureInitialized();
      await binaryHandler.WriteContentCatalogue(catalogue, closeStreams);
    }

    public async Task Initialize(int maxLengthInMegaBytes, DirectoryInfo backupDirectory, int id, ContentCatalogue catalogue)
    {
      ID = id;
      this.maxLength = maxLengthInMegaBytes * 1024 * 1024;
      filename = Path.Combine(backupDirectory.FullName, $"BackupTarget.{id}.exe");
      if (!backupDirectory.Exists)
        backupDirectory.Create();
      binaryHandler = new BackupTargetBinaryHandler(new FileInfo(filename), compressionHandler);
      await EnsureContentCatalogue(catalogue);
      CalculateTail();
      Initialized = true;
    }

    private void CalculateTail()
    {
      if (!catalogue.SearchTargets.ContainsKey(ID))
      {
        tail = BackupTargetConstants.DataOffset;
        return;
      }
      long calculatedTail = catalogue.Targets[ID].Content.OfType<ContentCatalogueBinaryEntry>().Max(item => item.TargetOffset + item.TargetLength);
      tail = Math.Max(calculatedTail, BackupTargetConstants.DataOffset);
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
    public async Task CalculateHashes(ContentCatalogueBinaryEntry entry)
    {
      var tempFile = await binaryHandler.ExtractFile(entry);
      if (tempFile.Exists)
      {
        try
        {
          entry.PrimaryContentHash = await primaryHasher.GetHashString(tempFile);
          entry.SecondaryContentHash = await primaryHasher.GetHashString(tempFile);
          catalogue.AddContentHash(entry.PrimaryContentHash, entry);
        }
        finally
        {
          tempFile.Delete();
        }
      }
    }

    public async Task ReclaimSpace(IProgress<ProgressReport> progressCallback)
    {
      var binaryEntries = catalogue.Targets[ID].Content.OfType<ContentCatalogueBinaryEntry>().OrderBy(x => x.TargetOffset).ToArray();
      var intervals = catalogue.Targets[ID].Content.OfType<ContentCatalogueBinaryEntry>().Select(y => new OffsetAndLengthPair { Offset = y.TargetOffset, Length = y.TargetLength }).OrderBy(x => x.Offset).ToArray();

      await binaryHandler.RetainDataIntervals(intervals, BackupTargetConstants.DataOffset, progressCallback);
      tail = RecalculateOffsets(binaryEntries);

      ConvertAllUnclaimedLinksToClaimedLinks();
    }

    private void ConvertAllUnclaimedLinksToClaimedLinks()
    {
      var unclaimedLinks = catalogue.Targets[ID].Content.OfType<ContentCatalogueUnclaimedLinkEntry>().ToArray();
      foreach (var unclaimedLink in unclaimedLinks)
      {
        catalogue.Targets[ID].ReplaceContent(unclaimedLink, new ContentCatalogueLinkEntry(unclaimedLink));
      }
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
  }
}
