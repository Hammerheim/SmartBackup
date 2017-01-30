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
    void Add(TargetContentCatalogue catalogue);
    Task BuildFromExistingBackups(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes);

    //Task ExtractFile(string key, DirectoryInfo extractionRoot);
    //Task ExtractFile(string key, int version, DirectoryInfo extractionRoot);

    Task ExtractAll(DirectoryInfo extractionRoot, IProgress<ProgressReport> progressCallback);
  }
}
