using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vibe.Hammer.SmartBackup.Progress;
using Vibe.Hammer.SmartBackup.Target;

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

    public TargetContentCatalogue(int backupTargetIndex)
      : this()
    {
      BackupTargetIndex = backupTargetIndex;
    }

    [XmlAttribute("bti")]
    public int BackupTargetIndex { get; set; }

    [XmlArray("Content")]
    [XmlArrayItem("BTI")]
    public List<ContentCatalogueEntry> Content { get; private set; }

    [XmlIgnore]
    public Dictionary<string, List<ContentCatalogueEntry>> KeySearchContent { get; private set; }

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

    public long CalculateTail()
    {
      return Math.Max(CalculateBinaryTail(), CalculateUnclaimedLinkTail());
    }

    private long CalculateBinaryTail()
    {
      long tail = 0;
      var entries = Content.OfType<ContentCatalogueBinaryEntry>();
      if (entries.Any())
      {
        tail = entries.Max(item => item.TargetOffset + item.TargetLength);
      }
      return Math.Max(tail, BackupTargetConstants.DataOffset);
    }

    private long CalculateUnclaimedLinkTail()
    {
      long tail = 0;
      var entries = Content.OfType<ContentCatalogueUnclaimedLinkEntry>();
      if (entries.Any())
      {
        tail = entries.Max(item => item.TargetOffset + item.TargetLength);
      }
      return Math.Max(tail, BackupTargetConstants.DataOffset);
    }

    public virtual List<ContentCatalogueEntry> CloneContent()
    {
      var clone = new List<ContentCatalogueEntry>();
      foreach (var item in Content)
      {
        clone.Add(item.Clone());
      }
      return clone;
    }

    public virtual void PromoteClonedContent(List<ContentCatalogueEntry> clonedContent)
    {
      foreach (var clonedItem in clonedContent)
      {
        if (KeySearchContent.ContainsKey(clonedItem.Key))
        {
          ContentCatalogueEntry original = null;
          foreach (var possibleMatch in KeySearchContent[clonedItem.Key])
          {
            if (possibleMatch.Version == clonedItem.Version)
            {
              original = possibleMatch;
              break;
            }
          }
          if (original != null)
          {
            KeySearchContent[clonedItem.Key].Remove(original);
            Content.Remove(original);
            Content.Add(clonedItem);
            KeySearchContent[clonedItem.Key].Add(clonedItem);
          }
        }
      }
    }
  }
}
