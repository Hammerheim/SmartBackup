using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  public interface IContentCatalogue
  {
    ContentCatalogueEntry GetNewestVersion(FileInformation file);

    ContentCatalogueEntry GetNewestVersion(string key);

    ContentCatalogueEntry GetSpecificVersion(string key, int version);

    void Add(TargetContentCatalogue catalogue);

    Task BuildFromExistingBackups(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes, string filenamePattern);

    IBackupTarget GetBackupTargetFor(ContentCatalogueEntry entry);

    bool IsKnownPrimaryHash(string primaryContentHash);

    IEnumerable<ContentCatalogueEntry> EnumerateContent();

    List<string> GetUniqueFileKeys();

    IBackupTarget GetBackupTargetContainingFile(FileInformation file);
  }
}
