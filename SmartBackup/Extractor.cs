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
  public class Extractor : IExtractor
  {
    private readonly IContentCatalogue catalogue;
    private readonly IExtractableContentCatalogue extractableCatalogue;
    private readonly IBackupTargetHandler targetHandler;

    public Extractor(IContentCatalogue contentCatalogue, IExtractableContentCatalogue extractableContentCatalogue, IBackupTargetHandler backupTargetHandler)
    {
      catalogue = contentCatalogue;
      extractableCatalogue = extractableContentCatalogue;
      targetHandler = backupTargetHandler;
    }

    public async Task ExtractAll(DirectoryInfo extractionRoot, bool validateOnExtraction, IProgress<ProgressReport> progressCallback)
    {
      var keys = extractableCatalogue.GetUniqueFileKeys();
      var numberOfFiles = keys.Count;
      var lastReport = DateTime.Now;
      int fileNumber = 0;

      foreach (var key in keys)
      {
        await ExtractFile(key, validateOnExtraction, extractionRoot);
        if (DateTime.Now - lastReport > TimeSpan.FromSeconds(5))
        {
          var file = extractableCatalogue.GetNewestVersion(key);
          progressCallback.Report(new ProgressReport(file.SourceFileInfo.FileName, fileNumber, numberOfFiles));
          lastReport = DateTime.Now;
        }

        fileNumber++;
      }
    }

    private async Task ExtractFile(string key, bool validateOnExtraction, DirectoryInfo extractionRoot)
    {
      var item = extractableCatalogue.GetNewestVersion(key);
      if (item.Deleted)
        return;

      var linkItem = item as ContentCatalogueLinkEntry;
      if (linkItem != null)
      {
        await ExtractLinkedFile(linkItem, extractionRoot, validateOnExtraction);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = targetHandler.GetBackupTargetContainingFile(catalogue, item.SourceFileInfo);
      if (backupTarget != null)
        await backupTarget.ExtractFile(binaryContentItem, validateOnExtraction, extractionRoot);
    }

    private async Task ExtractLinkedFile(ContentCatalogueLinkEntry link, DirectoryInfo extractionRoot, bool validateOnExtraction)
    {
      var item = extractableCatalogue.GetSpecificVersion(link.ContentCatalogueEntryKey, link.ContentCatalogueEntryVersion);

      var newLinkItem = item as ContentCatalogueLinkEntry;
      if (newLinkItem != null)
      {
        await ExtractLinkedFile(newLinkItem, extractionRoot, validateOnExtraction);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = targetHandler.GetBackupTargetContainingFile(catalogue, item.SourceFileInfo);
      if (backupTarget != null)
        await backupTarget.ExtractLinkedFile(binaryContentItem, link, extractionRoot, validateOnExtraction);
    }
  }
}
