using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;

namespace Vibe.Hammer.SmartBackup
{
  public interface IBackupTarget
  {
    Task Initialize(int maxLengthInMegaBytes, DirectoryInfo backupDirectory, int id, ContentCatalogue catalogue);
    int TargetId { get; set; }
    long Tail { get; }
    bool Contains(string key);
    bool Contains(string key, int version);
    bool CanContain(FileInformation file);
    Task AddFile(FileInformation file);
    Task WriteCatalogue(bool closeStreams);
    Task ReadCatalogue();
    Task ExtractFile(ContentCatalogueBinaryEntry file, DirectoryInfo extractionRoot);
    Task CalculateHashes(ContentCatalogueBinaryEntry entry);
  }
}
