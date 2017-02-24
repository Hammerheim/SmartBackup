using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlType("TCC")]
  [XmlInclude(typeof(ContentCatalogueEntry))]
  [XmlInclude(typeof(ContentCatalogueBinaryEntry))]
  [XmlInclude(typeof(ContentCatalogueLinkEntry))]
  [XmlInclude(typeof(ContentCatalogueUnclaimedLinkEntry))]
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

    public void Remove(ContentCatalogueEntry item)
    {
      Content.Remove(item);
      if (KeySearchContent.ContainsKey(item.Key))
        KeySearchContent.Remove(item.Key);
    }

    [XmlArray("Content")]
    [XmlArrayItem("BTI")]
    public List<ContentCatalogueEntry> Content { get; private set; }

    [XmlIgnore]
    public Dictionary<string, List<ContentCatalogueEntry>> KeySearchContent { get; private set; }

    [XmlIgnore]
    public IBackupTarget BackupTarget { get; set; }

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

    public void ReplaceContent(ContentCatalogueEntry toBeReplaced, ContentCatalogueEntry replaceWithThis)
    {
      Remove(toBeReplaced);
      Add(replaceWithThis);
    }

    internal async Task<bool> Defragment(List<ContentCatalogueBinaryEntry> binariesToMove, IProgress<ProgressReport> progressCallback)
    {
      return await BackupTarget.Defragment(binariesToMove, progressCallback);
    }
  }
}
