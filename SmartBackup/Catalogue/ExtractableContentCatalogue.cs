using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  internal class ExtractableContentCatalogue : IExtractableContentCatalogue
  {
    private IContentCatalogue catalogue;
    public ExtractableContentCatalogue(IContentCatalogue catalogue)
    {
      this.catalogue = catalogue;
    }

    public virtual ContentCatalogueEntry GetNewestVersion(FileInformation file) => GetNewestVersion(file.FullyQualifiedFilename);

    public virtual ContentCatalogueEntry GetNewestVersion(string key)
    {
      ContentCatalogueEntry newestItem = null;
      foreach (var catalogue in catalogue.Targets)
      {
        if (catalogue.KeySearchContent.ContainsKey(key))
        {
          foreach (var item in catalogue.KeySearchContent[key])
          {
            if (newestItem == null)
              newestItem = item;
            if (item.Version > newestItem.Version)
              newestItem = item;
          }
        }
      }
      return newestItem;
    }

    public virtual ContentCatalogueEntry GetSpecificVersion(string key, int version)
    {
      foreach (var catalogue in catalogue.Targets)
      {
        if (catalogue.KeySearchContent.ContainsKey(key))
        {
          foreach (var item in catalogue.KeySearchContent[key])
          {
            if (item.Version == version)
              return item;
          }
        }
      }
      return null;
    }

    public virtual List<string> GetUniqueFileKeys()
    {
      Dictionary<string, string> keys = new Dictionary<string, string>();
      List<string> returnThis = new List<string>();
      foreach (var target in catalogue.Targets)
      {
        foreach (var file in target.Content)
        {
          if (!keys.ContainsKey(file.SourceFileInfo.FullyQualifiedFilename))
          {
            keys.Add(file.SourceFileInfo.FullyQualifiedFilename, string.Empty);
            returnThis.Add(file.SourceFileInfo.FullyQualifiedFilename);
          }
        }
      }
      return returnThis;
    }

  }
}
