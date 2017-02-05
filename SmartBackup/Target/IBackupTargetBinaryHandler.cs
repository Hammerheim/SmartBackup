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
  public interface IBackupTargetBinaryHandler
  {
    Task<bool> InsertFile(ContentCatalogueBinaryEntry file, FileInfo sourceFile);
    Task<bool> RemoveFile(ContentCatalogueBinaryEntry file);
    Task<bool> Defragment();
    Task WriteContentCatalogue(ContentCatalogue catalogue, bool closeStreams);
    Task<ContentCatalogue> ReadContentCatalogue();
    bool BinaryFileExists { get; }
    Task<FileInfo> ExtractFile(ContentCatalogueBinaryEntry file);
    Task RetainDataIntervals(OffsetAndLengthPair[] intervals, int lowTail, IProgress<ProgressReport> progressCallback);
    Task MoveBytes(long moveFromOffset, long numberOfBytesToMove, long newOffset);
  }
}
