using System;
using System.Collections;
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
    // private and protected fields

    private ContentCatalogueBinaryHandler binaryHandler;
    private IExtractableContentCatalogue extractableCatalogue = null;
    private IBackupTargetHandler targetHandler = null;

    protected DirectoryInfo TargetDirectory { get; set; }
    protected Dictionary<int, TargetContentCatalogue> SearchTargets { get; set; }
    protected Dictionary<string, List<ContentCatalogueBinaryEntry>> ContentHashes { get; set; }

    #region Construction

    protected ContentCatalogue()
    {
      SearchTargets = new Dictionary<int, TargetContentCatalogue>();
      Targets = new List<TargetContentCatalogue>();
      ContentHashes = new Dictionary<string, List<ContentCatalogueBinaryEntry>>();
    }

    protected ContentCatalogue(int maxSizeInMegaBytes, DirectoryInfo backupDirectory, string filenamePattern, IBackupTargetHandler backupTargetHandler)
      : this()
    {
      MaxSizeOfFiles = maxSizeInMegaBytes;
      BackupDirectory = backupDirectory.FullName;
      Version = 1;
      TargetDirectory = backupDirectory;
      FilenamePattern = filenamePattern;
      binaryHandler = new ContentCatalogueBinaryHandler(GetContentCatalogueFilename(), new CompressionHandler());
      targetHandler = backupTargetHandler;
    }

    public static async Task<ContentCatalogue> Build(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes, string filenamePattern, IBackupTargetHandler backupTargetHandler)
    {
      var instance = await BuildFromExistingBackups(backupDirectory, expectedMaxSizeInMegaBytes, filenamePattern);

      return instance ?? new ContentCatalogue(expectedMaxSizeInMegaBytes, backupDirectory, filenamePattern, backupTargetHandler);
    }

    private static async Task<ContentCatalogue> BuildFromExistingBackups(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes, string filenamePattern)
    {
      var catalogueFile = new FileInfo(Path.Combine(backupDirectory.FullName, $"{filenamePattern}.ContentCatalogue.exe"));
      var binaryHandler = new ContentCatalogueBinaryHandler(catalogueFile, new CompressionHandler());
      var backupTargetHandler = new BackupTargetHandler(expectedMaxSizeInMegaBytes, filenamePattern, backupDirectory.FullName);

      var instance = await binaryHandler.ReadContentCatalogue();

      if (instance == null)
        return null;

      instance.BackupDirectory = backupDirectory.FullName;
      instance.FilenamePattern = filenamePattern;
      instance.MaxSizeOfFiles = expectedMaxSizeInMegaBytes;
      instance.binaryHandler = binaryHandler;
      instance.targetHandler = backupTargetHandler;

      instance.RebuildSearchIndex();
      backupTargetHandler.InitializeTargets(instance.Targets);

      return instance;
    }

    #endregion

    #region Public Properties

    [XmlAttribute("ms")]
    public virtual int MaxSizeOfFiles { get; set; }

    [XmlElement("BD")]
    public virtual string BackupDirectory { get; set; }

    [XmlArray("Targets")]
    [XmlArrayItem("BTI")]
    public virtual List<TargetContentCatalogue> Targets { get; set; }

    [XmlAttribute("v")]
    public virtual int Version { get; set; }
    [XmlAttribute("fp")]
    public virtual string FilenamePattern { get; set; }

    #endregion

    protected IExtractableContentCatalogue ExtractableCatalogue
    {
      get
      {
        if (extractableCatalogue == null)
          extractableCatalogue = new ExtractableContentCatalogue(this);
        return extractableCatalogue;
      }
    }

    public virtual void AddItem(int targetId, ContentCatalogueBinaryEntry catalogueItem)
    {
      if (SearchTargets.ContainsKey(targetId))
      {
        SearchTargets[targetId].Add(catalogueItem);
      }
    }
    private void RebuildSearchIndex()
    {
      foreach (var item in Targets)
      {
        SearchTargets.Add(item.BackupTargetIndex, item);
        item.RebuildSearchIndex();
      }

      BuildContentHashesDictionary();
    }

    private FileInfo GetContentCatalogueFilename() => new FileInfo(Path.Combine(TargetDirectory.FullName, $"{FilenamePattern}.ContentCatalogue.exe"));

    private void AddTargetContentCatalogue(TargetContentCatalogue catalogue)
    {
      if (!SearchTargets.ContainsKey(catalogue.BackupTargetIndex))
      {
        Targets.Add(catalogue);
        SearchTargets.Add(catalogue.BackupTargetIndex, catalogue);
      }
    }

    public virtual void RemoveItem(ContentCatalogueBinaryEntry catalogueItem)
    {
      var (found, id) = GetBackupTargetFor(catalogueItem);
      if (found)
        Targets[id].Remove(catalogueItem);
    }

    public virtual void Close()
    {
      targetHandler.CloseTargets();
      binaryHandler.CloseStream();
    }
    public virtual void WriteCatalogue() => binaryHandler.WriteContentCatalogue(this, false);

    public virtual (bool Found, int Id) GetBackupTargetContainingFile(FileInformation file)
    {
      var target = Targets.FirstOrDefault(t => t.KeySearchContent.ContainsKey(file.FullyQualifiedFilename));
      return (target != null, target?.BackupTargetIndex ?? -1); //target == null ? null : BackupTargetFactory.GetCachedTarget(target.BackupTargetIndex);
    }

    public virtual IEnumerable<List<ContentCatalogueBinaryEntry>> GetAllPossibleDublicates(IProgress<ProgressReport> progressCallback)
    {
      if (!ContentHashes.Any())
        BuildContentHashesDictionary();

      foreach (var key in ContentHashes.Keys)
      {
        if (ContentHashes[key].Count > 1)
          yield return ContentHashes[key];
      }
    }

    public virtual IEnumerable<ContentCatalogueUnclaimedLinkEntry> GetUnclaimedLinks()
    {
      var entries = new List<ContentCatalogueUnclaimedLinkEntry>();
      foreach (var target in Targets)
      {
        entries.AddRange(target.Content.OfType<ContentCatalogueUnclaimedLinkEntry>());
      }
      return entries;
    }

    public virtual IEnumerable<ContentCatalogueUnclaimedLinkEntry> GetUnclaimedLinks(int backupTargetId)
    {
      if (SearchTargets.ContainsKey(backupTargetId))
      {
        return SearchTargets[backupTargetId].Content.OfType<ContentCatalogueUnclaimedLinkEntry>();
      }
      return new List<ContentCatalogueUnclaimedLinkEntry>();
    }

    public virtual void ReplaceBinaryEntryWithLink(ContentCatalogueBinaryEntry binary, ContentCatalogueLinkEntry link)
    {
      foreach (var target in Targets)
      {
        if (target.KeySearchContent.ContainsKey(binary.Key))
        {
          target.ReplaceContent(binary, link);
        }
      }
      
    }
    public virtual (bool Found, int Id) GetBackupTargetFor(ContentCatalogueEntry entry)
    {
      foreach (var target in Targets)
      {
        if (target.KeySearchContent.ContainsKey(entry.Key))
        {
          if (target.KeySearchContent[entry.Key].FirstOrDefault(e => e.Key == entry.Key && e.Version == entry.Version) != null)
            return (true, target.BackupTargetIndex);
        }
      }

      return (false, -1);
    }

    protected void BuildContentHashesDictionary()
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

    public virtual int CountTotalEntries()
    {
      return Targets.Sum(target => target.Content.Count);
    }

    public virtual int AddBackupTarget()
    {
      var id = SearchTargets.Keys.Count == 0 ? 0 : SearchTargets.Keys.Max() + 1;
      var target = new TargetContentCatalogue(id);
      AddTargetContentCatalogue(target);
      return id;
    }

    public virtual (bool Found, int TargetId) TryFindBackupTargetWithRoom(long requiredSpace)
    {
      foreach (var target in Targets)
      {
        if ((MaxSizeOfFiles * BackupTargetConstants.MegaByte) - target.CalculateTail() >= requiredSpace)
        {
          return (true, target.BackupTargetIndex);
        }
      }
      return (false, -1);
    }

    public virtual IEnumerable<ContentCatalogueEntry> EnumerateContent()
    {
      foreach (var target in Targets)
      {
        foreach (var entry in target.Content)
        {
          yield return entry;
        }
      }
    }

    public virtual void ReplaceContent(int backupTargetId, ContentCatalogueEntry toBeReplaced, ContentCatalogueEntry replaceWithThis)
    {
      if (SearchTargets.ContainsKey(backupTargetId))
      {
        SearchTargets[backupTargetId].ReplaceContent(toBeReplaced, replaceWithThis);
      }
    }

    public ContentCatalogueEntry GetNewestVersion(FileInformation file)
    {
      return ExtractableCatalogue.GetNewestVersion(file);
    }

    public ContentCatalogueEntry GetNewestVersion(string key)
    {
      return ExtractableCatalogue.GetNewestVersion(key);
    }

    public ContentCatalogueEntry GetSpecificVersion(string key, int version)
    {
      return ExtractableCatalogue.GetSpecificVersion(key, version);
    }

    public List<string> GetUniqueFileKeys()
    {
      return ExtractableCatalogue.GetUniqueFileKeys();
    }
  }
}
