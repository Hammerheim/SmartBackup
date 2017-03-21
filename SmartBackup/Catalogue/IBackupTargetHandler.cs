using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  public interface IBackupTargetHandler
  {
    IBackupTarget GetTarget(int id);
    IBackupTarget GetTarget(int id, bool allowCreation);
    void InitializeTargets(List<TargetContentCatalogue> catalogueTargets);
    void CloseTargets();
    IBackupTarget GetBackupTargetContainingFile(IContentCatalogue catalogue, FileInformation file);
    IBackupTarget GetBackupTargetFor(IContentCatalogue catalogue, ContentCatalogueEntry entry);
  }
}
