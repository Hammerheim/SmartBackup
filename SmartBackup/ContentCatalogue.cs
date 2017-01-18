using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [XmlRoot("CC")]
  [XmlInclude(typeof(BackupTargetItem))]
  public class ContentCatalogue
  {
    public ContentCatalogue()
    {
      Content = new List<BackupTargetItem>();
      KeySearchContent = new Dictionary<string, BackupTargetItem>();
    }

    public void Add(BackupTargetItem item)
    {
      Content.Add(item);
      KeySearchContent.Add(item.File.FullyQualifiedFilename, item);
    }

    [XmlArray("Content")]
    [XmlArrayItem("BTI")]
    public List<BackupTargetItem> Content { get; private set; }

    [XmlIgnore()]
    public Dictionary<string, BackupTargetItem> KeySearchContent { get; private set; }

    public void RebuildSearchIndex()
    {
      KeySearchContent = new Dictionary<string, BackupTargetItem>();
      foreach (var item in Content)
      {
        KeySearchContent.Add(item.File.FullyQualifiedFilename, item);
      }
    }
  }
}
