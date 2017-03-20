using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  public interface IExtractableContentCatalogue
  {
    ContentCatalogueEntry GetNewestVersion(FileInformation file);
    ContentCatalogueEntry GetNewestVersion(string key);
    ContentCatalogueEntry GetSpecificVersion(string key, int version);
    List<string> GetUniqueFileKeys();
  }
}
