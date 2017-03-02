using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vibe.Hammer.SmartBackup.Compression;
using Vibe.Hammer.SmartBackup.Progress;
using Vibe.Hammer.SmartBackup.Target;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlRoot("CC")]
  [XmlInclude(typeof(TargetContentCatalogue))]
  public class ContentCatalogue : IContentCatalogue
  {
    private ContentCatalogueBinaryHandler binaryHandler;
    protected DirectoryInfo TargetDirectory { get; set; }

    public ContentCatalogue()
    {
      SearchTargets = new Dictionary<int, TargetContentCatalogue>();
      Targets = new List<TargetContentCatalogue>();
      ContentHashes = new Dictionary<string, List<ContentCatalogueBinaryEntry>>();
      ContentLengths = new Dictionary<long, List<ContentCatalogueBinaryEntry>>();
    }

    public ContentCatalogue(int maxSizeInMegaBytes, DirectoryInfo backupDirectory, string filenamePattern)
      : this()
    {
      MaxSizeOfFiles = maxSizeInMegaBytes;
      BackupDirectory = backupDirectory.FullName;
      Version = 1;
      TargetDirectory = backupDirectory;
      FilenamePattern = filenamePattern;
      binaryHandler = new ContentCatalogueBinaryHandler(GetContentCatalogueFilename(), new CompressionHandler());
    }

    [XmlAttribute("ms")]
    public int MaxSizeOfFiles { get; set; }

    [XmlElement("BD")]
    public string BackupDirectory { get; set; }

    [XmlArray("Targets")]
    [XmlArrayItem("BTI")]
    public List<TargetContentCatalogue> Targets { get; private set; }

    [XmlAttribute("v")]
    public int Version { get; set; }
    [XmlAttribute("fp")]
    public string FilenamePattern { get; set; }

    [XmlIgnore]
    public Dictionary<int, TargetContentCatalogue> SearchTargets { get; private set; }

    internal void AddItem(int targetId, ContentCatalogueBinaryEntry catalogueItem)
    {
      if (SearchTargets.ContainsKey(targetId))
      {
        SearchTargets[targetId].Add(catalogueItem);
      }
    }

    [XmlIgnore]
    public Dictionary<string, List<ContentCatalogueBinaryEntry>> ContentHashes { get; private set; }
    [XmlIgnore]
    public Dictionary<long, List<ContentCatalogueBinaryEntry>> ContentLengths { get; private set; }

    public void RebuildSearchIndex()
    {
      foreach (var item in Targets)
      {
        SearchTargets.Add(item.BackupTargetIndex, item);
        item.RebuildSearchIndex();
      }

      BuildContentLengthsDictionary();
      BuildContentHashesDictionary();
    }

    public ContentCatalogueEntry GetNewestVersion(FileInformation file)
    {
      return GetNewestVersion(file.FullyQualifiedFilename);
    }

    private FileInfo GetContentCatalogueFilename()
    {
      return new FileInfo(Path.Combine(TargetDirectory.FullName, $"{FilenamePattern}.ContentCatalogue.exe"));
    }

    public ContentCatalogueEntry GetNewestVersion(string key)
    {
      ContentCatalogueEntry newestItem = null;
      foreach (var catalogue in Targets)
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

    public ContentCatalogueEntry GetSpecificVersion(string key, int version)
    {
      foreach (var catalogue in Targets)
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

    public void AddContentHash(string primaryContentHash, ContentCatalogueBinaryEntry entry)
    {
      if (string.IsNullOrEmpty(primaryContentHash))
        return;
      if (string.IsNullOrEmpty(entry.PrimaryContentHash))
        return;

      if (ContentHashes.ContainsKey(primaryContentHash))
        ContentHashes[primaryContentHash].Add(entry);
      else
        ContentHashes.Add(primaryContentHash, new List<ContentCatalogueBinaryEntry> { entry });
    }

    public void Add(TargetContentCatalogue catalogue)
    {
      if (!SearchTargets.ContainsKey(catalogue.BackupTargetIndex))
      {
        Targets.Add(catalogue);
        SearchTargets.Add(catalogue.BackupTargetIndex, catalogue);
      }
    }

    public async Task BuildFromExistingBackups(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes, string filenamePattern)
    {
      this.BackupDirectory = backupDirectory.FullName;
      this.MaxSizeOfFiles = expectedMaxSizeInMegaBytes;
      this.FilenamePattern = filenamePattern;

      backupDirectory.Refresh();
      if (!backupDirectory.Exists)
        backupDirectory.Create();

      TargetDirectory = backupDirectory;
      if (binaryHandler == null)
      {
        binaryHandler = new ContentCatalogueBinaryHandler(GetContentCatalogueFilename(), new CompressionHandler());
      }
      var tempCatalogue = await binaryHandler.ReadContentCatalogue();
      if (tempCatalogue != null)
      {
        Targets = tempCatalogue.Targets;
        SearchTargets = tempCatalogue.SearchTargets;
        MaxSizeOfFiles = tempCatalogue.MaxSizeOfFiles;
        Version = tempCatalogue.Version;
        RebuildSearchIndex();
      }

      foreach (var target in Targets)
      {
        BackupTargetFactory.InitializeTarget(target.BackupTargetIndex, target.CalculateTail(), MaxSizeOfFiles, backupDirectory, filenamePattern);
      }
    }

    public void CloseTargets()
    {
      foreach (var target in Targets)
      {
        var binaryTarget = BackupTargetFactory.GetCachedTarget(target.BackupTargetIndex);
        if (binaryTarget != null)
          binaryTarget.CloseStream();
      }
      binaryHandler.CloseStream();
    }
    public void WriteCatalogue()
    {
      binaryHandler.WriteContentCatalogue(this, false);
    }

    public IBackupTarget GetBackupTargetContainingFile(FileInformation file)
    {
      var target = Targets.FirstOrDefault(t => t.KeySearchContent.ContainsKey(file.FullyQualifiedFilename));
      if (target == null)
        return null;
      return BackupTargetFactory.GetCachedTarget(target.BackupTargetIndex);
    }

    public List<string> GetUniqueFileKeys()
    {
      Dictionary<string, string> keys = new Dictionary<string, string>();
      List<string> returnThis = new List<string>();
      foreach (var target in Targets)
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

    public IEnumerable<List<ContentCatalogueBinaryEntry>> GetAllPossibleDublicates(IProgress<ProgressReport> progressCallback)
    {
      if (!ContentHashes.Any())
        BuildContentHashesDictionary();

      foreach (var key in ContentHashes.Keys)
      {
        if (ContentHashes[key].Count > 1)
          yield return ContentHashes[key];
      }
    }

    public IEnumerable<ContentCatalogueUnclaimedLinkEntry> GetUnclaimedLinks()
    {
      var entries = new List<ContentCatalogueUnclaimedLinkEntry>();
      foreach (var target in Targets)
      {
        entries.AddRange(target.Content.OfType<ContentCatalogueUnclaimedLinkEntry>());
      }
      return entries;
    }

    public void ReplaceBinaryEntryWithLink(ContentCatalogueBinaryEntry binary, ContentCatalogueLinkEntry link)
    {
      foreach (var target in Targets)
      {
        if (target.KeySearchContent.ContainsKey(binary.Key))
        {
          target.ReplaceContent(binary, link);
        }
      }
      
    }
    public IBackupTarget GetBackupTargetFor(ContentCatalogueEntry entry)
    {
      foreach (var target in Targets)
      {
        if (target.KeySearchContent.ContainsKey(entry.Key))
        {
          if (target.KeySearchContent[entry.Key].FirstOrDefault(e => e.Key == entry.Key && e.Version == entry.Version) != null)
            return BackupTargetFactory.GetCachedTarget(target.BackupTargetIndex);
        }
      }

      return null;
    }

    private void BuildContentHashesDictionary()
    {
      ContentHashes.Clear();
      foreach (var target in Targets)
      {
        foreach (var entry in target.Content.OfType<ContentCatalogueBinaryEntry>())
        {
          if (entry.PrimaryContentHash != null)
          {
            if (ContentHashes.ContainsKey(entry.PrimaryContentHash))
              ContentHashes[entry.PrimaryContentHash].Add(entry);
            else
            {
              ContentHashes.Add(entry.PrimaryContentHash, new List<ContentCatalogueBinaryEntry> { entry });
            }
          }
        }
      }
    }

    private void BuildContentLengthsDictionary()
    {
      ContentLengths.Clear();
      foreach (var target in Targets)
      {
        foreach (var entry in target.Content.OfType<ContentCatalogueBinaryEntry>())
        {
          if (ContentLengths.ContainsKey(entry.TargetLength))
            ContentLengths[entry.TargetLength].Add(entry);
          else
          {
            ContentLengths.Add(entry.TargetLength, new List<ContentCatalogueBinaryEntry> { entry });
          }
        }
      }
    }

    public IEnumerable<ContentCatalogueEntry> EnumerateContent()
    {
      foreach (var target in Targets)
      {
        foreach (var entry in target.Content)
        {
          yield return entry;
        }
      }
    }

    public int AddBackupTarget()
    {
      var id = SearchTargets.Keys.Count == 0 ? 0 : SearchTargets.Keys.Max() + 1;
      var target = new TargetContentCatalogue(id);
      Add(target);
      return id;
    }

    public bool TryFindBackupTargetWithRoom(long requiredSpace, out int id)
    {
      id = -1;
      foreach (var target in Targets)
      {
        if ((MaxSizeOfFiles * BackupTargetConstants.MegaByte) - target.CalculateTail() >= requiredSpace)
        {
          id = target.BackupTargetIndex;
          return true;
        }
      }
      return false;
      
    }
  }
}
