﻿using System;
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
    void Add(TargetContentCatalogue catalogue);
    Task BuildFromExistingBackups(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes);

    Task ExtractAll(DirectoryInfo extractionRoot, IProgress<ProgressReport> progressCallback);
    IEnumerable<ContentCatalogueBinaryEntry> GetAllContentEntriesWithoutHashes(IProgress<ProgressReport> progressCallback);
    BackupTarget GetBackupTargetFor(ContentCatalogueEntry entry);
  }
}
