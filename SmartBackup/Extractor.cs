using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup
{
  public class Extractor : IExtractor
  {
    private readonly IContentCatalogue catalogue;
    public Extractor(IContentCatalogue contentCatalogue)
    {
      this.catalogue = contentCatalogue;
    }

    public async Task ExtractAll(DirectoryInfo extractionRoot, IProgress<ProgressReport> progressCallback)
    {
      var keys = catalogue.GetUniqueFileKeys();
      var numberOfFiles = keys.Count;
      var lastReport = DateTime.Now;
      int fileNumber = 0;

      foreach (var key in keys)
      {
        await ExtractFile(key, extractionRoot);
        if (DateTime.Now - lastReport > TimeSpan.FromSeconds(5))
        {
          var file = catalogue.GetNewestVersion(key);
          progressCallback.Report(new ProgressReport(file.SourceFileInfo.FileName, fileNumber, numberOfFiles));
          lastReport = DateTime.Now;
        }

        fileNumber++;
      }
    }

    private async Task ExtractFile(string key, DirectoryInfo extractionRoot)
    {
      var item = catalogue.GetNewestVersion(key);
      if (item.Deleted)
        return;

      var linkItem = item as ContentCatalogueLinkEntry;
      if (linkItem != null)
      {
        await ExtractLinkedFile(linkItem, extractionRoot);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = catalogue.GetBackupTargetContainingFile(item.SourceFileInfo);
      if (backupTarget == null)
        throw new FileNotFoundException();

      await backupTarget.ExtractFile(binaryContentItem, extractionRoot);
    }

    private async Task ExtractFile(string key, int version, DirectoryInfo extractionRoot)
    {
      var item = catalogue.GetSpecificVersion(key, version);
      if (item.Deleted)
        return;

      var linkItem = item as ContentCatalogueLinkEntry;
      if (linkItem != null)
      {
        await ExtractLinkedFile(linkItem, extractionRoot);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = catalogue.GetBackupTargetContainingFile(item.SourceFileInfo);
      if (backupTarget == null)
        throw new FileNotFoundException();
      await backupTarget.ExtractFile(binaryContentItem, extractionRoot);
    }

    private async Task ExtractLinkedFile(ContentCatalogueLinkEntry link, DirectoryInfo extractionRoot)
    {
      var item = catalogue.GetSpecificVersion(link.ContentCatalogueEntryKey, link.ContentCatalogueEntryVersion);

      var newLinkItem = item as ContentCatalogueLinkEntry;
      if (newLinkItem != null)
      {
        await ExtractLinkedFile(newLinkItem, extractionRoot);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = catalogue.GetBackupTargetContainingFile(item.SourceFileInfo);
      if (backupTarget == null)
        throw new FileNotFoundException();
      await backupTarget.ExtractLinkedFile(binaryContentItem, link, extractionRoot);
    }
  }
}
