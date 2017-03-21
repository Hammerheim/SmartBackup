﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Progress;
using Vibe.Hammer.SmartBackup.Target;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  public interface IContentCatalogue 
  {
    IEnumerable<ContentCatalogueEntry> EnumerateContent();

    (bool Found, int Id) GetBackupTargetFor(ContentCatalogueEntry entry);

    (bool Found, int Id) GetBackupTargetContainingFile(FileInformation file);
    int AddBackupTarget();
    (bool Found, int TargetId) TryFindBackupTargetWithRoom(long requiredSpace);

    IEnumerable<ContentCatalogueUnclaimedLinkEntry> GetUnclaimedLinks();
    IEnumerable<ContentCatalogueUnclaimedLinkEntry> GetUnclaimedLinks(int backupTargetId);
    void ReplaceContent(int backupTargetId, ContentCatalogueEntry toBeReplaced, ContentCatalogueEntry replaceWithThis);

    // Content
    int MaxSizeOfFiles { get; }
    int Version { get; }
    string FilenamePattern { get; }
    string BackupDirectory { get; }
    List<TargetContentCatalogue> Targets { get; }
    void WriteCatalogue();
    void Close();
    void AddItem(int targetId, ContentCatalogueBinaryEntry catalogueItem);
    void RemoveItem(ContentCatalogueBinaryEntry catalogueItem);
    int CountTotalEntries();
    IEnumerable<List<ContentCatalogueBinaryEntry>> GetAllPossibleDublicates(IProgress<ProgressReport> progressCallback);
    void ReplaceBinaryEntryWithLink(ContentCatalogueBinaryEntry binary, ContentCatalogueLinkEntry link);
  }
}
