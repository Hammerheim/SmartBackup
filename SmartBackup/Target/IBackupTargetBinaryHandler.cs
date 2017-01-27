using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;

namespace Vibe.Hammer.SmartBackup
{
  public interface IBackupTargetBinaryHandler
  {
    Task<bool> InsertFile(BackupTargetItem file, FileInfo sourceFile);
    Task<bool> RemoveFile(BackupTargetItem file);
    Task<bool> Defragment();
    Task WriteContentCatalogue(ContentCatalogue catalogue, bool closeStreams);
    Task<ContentCatalogue> ReadContentCatalogue();
    bool BinaryFileExists { get; }
    Task<FileInfo> ExtractFile(BackupTargetItem file);
  }
}
