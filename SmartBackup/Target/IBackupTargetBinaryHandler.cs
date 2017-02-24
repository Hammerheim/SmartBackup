using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Progress;
using Vibe.Hammer.SmartBackup.Target;

namespace Vibe.Hammer.SmartBackup
{
  public interface IBinaryHandler
  {
    Task<bool> InsertFile(ContentCatalogueBinaryEntry file, FileInfo sourceFile);
    bool BinaryFileExists { get; }
    Task<FileInfo> ExtractFile(ContentCatalogueBinaryEntry file);
    void CloseStream();
    Task<bool> CreateNewFile(long offset, long length);
    Task<bool> CopyBytesToStreamAsync(FileStream outputStream, long offset, long length);
    bool SwapFiles(FileInfo newFile);
  }
}
