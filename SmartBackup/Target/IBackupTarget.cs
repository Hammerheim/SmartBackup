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
  public interface IBackupTarget
  {
    void Initialize(int maxLengthInMegaBytes, DirectoryInfo backupDirectory, int id, long tail);
    int TargetId { get; }
    long Tail { get; }
    bool CanContain(FileInformation file);
    Task<ContentCatalogueBinaryEntry> AddFile(FileInformation file, int version);
    void CloseStream();
    Task ExtractFile(ContentCatalogueBinaryEntry file, DirectoryInfo extractionRoot);
    Task<HashPair> CalculateHashes(ContentCatalogueBinaryEntry entry);
    Task<bool> ReclaimSpace(List<ContentCatalogueBinaryEntry> binariesToMove, IProgress<ProgressReport> progressCallback);
  }
}
