using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlType("TCC")]
  [XmlInclude(typeof(BackupTargetItem))]
  public class TargetContentCatalogue
  {
    public TargetContentCatalogue()
    {
      Content = new List<BackupTargetItem>();
      KeySearchContent = new Dictionary<string, List<BackupTargetItem>>();
    }

    public TargetContentCatalogue(int backupTargetIndex, BackupTarget target)
      : this()
    {
      BackupTargetIndex = backupTargetIndex;
      BackupTarget = target;
    }

    [XmlAttribute("bti")]
    public int BackupTargetIndex { get; set; }

    public void Add(BackupTargetItem item)
    {
      Content.Add(item);
      AddToKeySearch(item);
    }

    [XmlArray("Content")]
    [XmlArrayItem("BTI")]
    public List<BackupTargetItem> Content { get; private set; }

    [XmlIgnore]
    public Dictionary<string, List<BackupTargetItem>> KeySearchContent { get; private set; }

    [XmlIgnore]
    public BackupTarget BackupTarget { get; set; }

    public void RebuildSearchIndex()
    {
      KeySearchContent = new Dictionary<string, List<BackupTargetItem>>();
      foreach (var item in Content)
      {
        AddToKeySearch(item);
      }
    }

    private void AddToKeySearch(BackupTargetItem item)
    {
      if (KeySearchContent.ContainsKey(item.File.FullyQualifiedFilename))
        KeySearchContent[item.File.FullyQualifiedFilename].Add(item);
      else
      {
        KeySearchContent.Add(item.File.FullyQualifiedFilename, new List<BackupTargetItem> { item });
      }
    }
  }
}
