using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlType("TCC")]
  [XmlInclude(typeof(ContentCatalogueEntry))]
  public class TargetContentCatalogue
  {
    public TargetContentCatalogue()
    {
      Content = new List<ContentCatalogueEntry>();
      KeySearchContent = new Dictionary<string, List<ContentCatalogueEntry>>();
    }

    public TargetContentCatalogue(int backupTargetIndex, BackupTarget target)
      : this()
    {
      BackupTargetIndex = backupTargetIndex;
      BackupTarget = target;
    }

    [XmlAttribute("bti")]
    public int BackupTargetIndex { get; set; }

    public void Add(ContentCatalogueEntry item)
    {
      Content.Add(item);
      AddToKeySearch(item);
    }

    [XmlArray("Content")]
    [XmlArrayItem("BTI")]
    public List<ContentCatalogueEntry> Content { get; private set; }

    [XmlIgnore]
    public Dictionary<string, List<ContentCatalogueEntry>> KeySearchContent { get; private set; }

    [XmlIgnore]
    public BackupTarget BackupTarget { get; set; }

    public void RebuildSearchIndex()
    {
      KeySearchContent = new Dictionary<string, List<ContentCatalogueEntry>>();
      foreach (var item in Content)
      {
        AddToKeySearch(item);
      }
    }

    private void AddToKeySearch(ContentCatalogueEntry item)
    {
      if (KeySearchContent.ContainsKey(item.SourceFileInfo.FullyQualifiedFilename))
        KeySearchContent[item.SourceFileInfo.FullyQualifiedFilename].Add(item);
      else
      {
        KeySearchContent.Add(item.SourceFileInfo.FullyQualifiedFilename, new List<ContentCatalogueEntry> { item });
      }
    }
  }
}
