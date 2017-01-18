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

namespace Vibe.Hammer.SmartBackup
{
  public class BackupTarget : IBackupTarget
  {
    //private LinkedList<BackupTargetItem> content = new LinkedList<BackupTargetItem>();
    //private Dictionary<string, BackupTargetItem> keySearchContent = new Dictionary<string, BackupTargetItem>();
    private ContentCatalogue catalogue;
    private BackupTargetBinaryHandler binaryHandler;
    private long maxLength;
    private long tail = 1256 * 1024;
    private string filename;
    private CompressionHandler compressionHandler;

    public BackupTarget(long maxLengthInMegaBytes, DirectoryInfo backupDirectory, int id)
    {
      this.maxLength = maxLengthInMegaBytes * 1024 * 1024;
      filename = Path.Combine(backupDirectory.FullName, $"BackupTarget.{id}.exe");
      if (!backupDirectory.Exists)
        backupDirectory.Create();
      var binaryInfo = new FileInfo(filename);
      binaryHandler = new BackupTargetBinaryHandler(binaryInfo);
      if (binaryInfo.Exists)
        catalogue = binaryHandler.ReadContentCatalogue();
      else
        catalogue = new ContentCatalogue();
      compressionHandler = new CompressionHandler();
    }

    public long Tail => tail;

    public int TargetId { get; set; }

    public async Task AddFile(FileInformation file)
    {
      using (FileCopyExecutor fileCopyExecutor = new FileCopyExecutor())
      {
        var item = new BackupTargetItem
        {
          File = file,
          TargetOffset = tail
        };

        var filename = await fileCopyExecutor.CopyFile(file.FullyQualifiedFilename);
        if (string.IsNullOrEmpty(filename))
          return;

        item.Compressed = await compressionHandler.CompressFile(filename);
        
        // Find location
        item.TargetOffset = tail;
        item.TargetLength = GetFileSize(filename);
        
        // Insert file
        var success = await binaryHandler.InsertFile(item, new FileInfo(filename));
        if (success)
        {
          tail = item.TargetOffset + item.TargetLength;
          // update log
          catalogue.Add(item);
        }
      }
    }

    private long GetFileSize(string filename)
    {
      var info = new FileInfo(filename);
      return info.Length;
    }

    public bool CanContain(FileInformation file)
    {
      return (tail + file.Size < maxLength);
    }

    public bool Contains(string key)
    {
      return (catalogue.KeySearchContent.ContainsKey(key));
    }

    public BackupTargetItem GetItem(string key)
    {
      if (Contains(key))
        return catalogue.KeySearchContent[key];
      return null;
    }

    public void ReadCatalogue()
    {
      throw new NotImplementedException();
    }

    public void WriteCatalogue()
    {
      binaryHandler.WriteContentCatalogue(catalogue);
    }
  }
}
