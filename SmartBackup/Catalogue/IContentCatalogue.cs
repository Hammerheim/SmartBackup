using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  public interface IContentCatalogue
  {
    BackupTargetItem GetNewestVersion(FileInformation file);
    void Add(TargetContentCatalogue catalogue);
    Task BuildFromExistingBackups(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes);
    
  }
}
