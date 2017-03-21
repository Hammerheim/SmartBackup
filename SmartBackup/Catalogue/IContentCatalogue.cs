using System;
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

    int AddBackupTarget();
    (bool Found, int TargetId) TryFindBackupTargetWithRoom(long requiredSpace);

    IEnumerable<List<ContentCatalogueBinaryEntry>> GetAllPossibleDublicates(IProgress<ProgressReport> progressCallback);

    void ConvertAllUnclaimedLinksToClaimedLinks(int contentTargetId);
    int CountUnclaimedLinks();
    void ReplaceBinaryEntryWithLink(ContentCatalogueBinaryEntry binary, ContentCatalogueLinkEntry link);

    // Content
    int MaxSizeOfFiles { get; }
    int Version { get; }
    string FilenamePattern { get; }
    string BackupDirectory { get; }
    List<TargetContentCatalogue> Targets { get; }
    void WriteCatalogue();
    void Close(IBackupTargetHandler targetHandler);
    void AddItem(int targetId, ContentCatalogueBinaryEntry catalogueItem);
    void RemoveItem(ContentCatalogueBinaryEntry catalogueItem);
    int CountTotalEntries();
  }
}
