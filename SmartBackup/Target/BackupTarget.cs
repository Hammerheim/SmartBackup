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

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTarget : IBackupTarget
  {
    private ContentCatalogue catalogue;
    private BackupTargetBinaryHandler binaryHandler;
    private long maxLength;
    private long tail = 1256 * 1024;
    private string filename;
    private CompressionHandler compressionHandler;
    private bool Initialized = false;
    private int ID;

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
      var item = new BackupTargetItem
      {
        File = file,
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

    private async Task InsertBinary(BackupTargetItem item, FileInfo sourceFile)
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
      compressionHandler = new CompressionHandler();
      binaryHandler = new BackupTargetBinaryHandler(new FileInfo(filename), compressionHandler);
      await EnsureContentCatalogue(catalogue);
      CalculateTail();
      Initialized = true;
    }

    private void CalculateTail()
    {
      if (!catalogue.SearchTargets.ContainsKey(ID))
      {
        tail = 1256 * 1024;
        return;
      }
      long calculatedTail = catalogue.Targets[ID].Content.Max(item => item.TargetOffset + item.TargetLength);
      tail = Math.Max(calculatedTail, 1256 * 1024);
    }

    private void EnsureInitialized()
    {
      if (!Initialized)
        throw new BackupTargetNotInitializedException();
    }

    public async Task ExtractFile(BackupTargetItem file, DirectoryInfo extractionRoot)
    {
      var tempFile = await binaryHandler.ExtractFile(file);
      if (tempFile != null)
      {
        var targetFile = new FileInfo(Path.Combine(extractionRoot.FullName, file.File.RelativePath, file.File.FileName));
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
        GC.WaitForPendingFinalizers();
      }
    }
  }
}
