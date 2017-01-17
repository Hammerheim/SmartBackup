using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public interface IBackupTargetBinaryHandler
  {
    Task<bool> InsertFile(BackupTargetItem file, FileInfo sourceFile);
    Task<bool> RemoveFile(BackupTargetItem file);
    Task<bool> Defragment();
    void WriteContentCatalogue(ContentCatalogue catalogue);
    ContentCatalogue ReadContentCatalogue();
  }
}
