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

    private ContentCatalogueEntry GetNewestVersion(string key)
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

    private ContentCatalogueEntry GetSpecificVersion(string key, int version)
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

    public bool IsKnownPrimaryHash(string primaryContentHash)
    {
      if (ContentHashes == null || !ContentHashes.Any())
        BuildContentHashesDictionary();

      return ContentHashes.ContainsKey(primaryContentHash);
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

    internal void EnsureTargetCatalogueExists(ContentCatalogue persistedCatalogue, int targetBinaryID, BackupTarget backupTarget)
    {
      if (!SearchTargets.ContainsKey(targetBinaryID))
      {
        Add(persistedCatalogue.SearchTargets[targetBinaryID]);
        persistedCatalogue.SearchTargets[targetBinaryID].BackupTarget = backupTarget;
      }
      else
      {
        ReplaceTargetContentCatalogue(persistedCatalogue, targetBinaryID, backupTarget);
      }
    }

    private void ReplaceTargetContentCatalogue(ContentCatalogue persistedCatalogue, int targetBinaryID, BackupTarget backupTarget)
    {
      if (SearchTargets.ContainsKey(targetBinaryID))
      {
        var currentTarget = Targets.FirstOrDefault(t => t.BackupTargetIndex == targetBinaryID);
        if (currentTarget != null)
          Targets.Remove(currentTarget);
        SearchTargets[targetBinaryID] = persistedCatalogue.SearchTargets[targetBinaryID];
        Add(persistedCatalogue.SearchTargets[targetBinaryID]);
        persistedCatalogue.SearchTargets[targetBinaryID].BackupTarget = backupTarget;
      }
      else
        Add(persistedCatalogue.SearchTargets[targetBinaryID]);
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

      if (!backupDirectory.Exists)
        return;

      TargetDirectory = backupDirectory;
      if (binaryHandler == null)
      {
        binaryHandler = new ContentCatalogueBinaryHandler(GetContentCatalogueFilename(), new CompressionHandler());
      }
      var tempCatalogue = binaryHandler.ReadContentCatalogue();
      if (tempCatalogue != null)
      {
        Targets = tempCatalogue.Targets;
        SearchTargets = tempCatalogue.SearchTargets;
        MaxSizeOfFiles = tempCatalogue.MaxSizeOfFiles;
        Version = tempCatalogue.Version;
        RebuildSearchIndex();
      }
      var files = backupDirectory.GetFiles($"{filenamePattern}.*.exe");
      if (files.Length == 0)
        return;

      var regEx = new Regex($"^{filenamePattern}\\.(\\d+)\\.exe$");
      foreach (var file in files)
      {
        var match = regEx.Match(file.Name);
        if (match.Success)
        {
          var number = int.Parse(match.Groups[1].Value);
          if (SearchTargets.ContainsKey(number))
          {
            var backupTarget = new BackupTarget(new MD5Hasher(), new Sha256Hasher(), new CompressionHandler());
            backupTarget.Initialize(expectedMaxSizeInMegaBytes, backupDirectory, number, CalculateTail(number), filenamePattern);
            SearchTargets[number].BackupTarget = backupTarget;
          }
          else
          {
            file.Delete();
          }
        }
      }
    }

    private long CalculateTail(int targetId)
    {
      if (!SearchTargets.ContainsKey(targetId))
      {
        return BackupTargetConstants.DataOffset;
      }

      long calculatedTail = 0;
      var entries = SearchTargets[targetId].Content.OfType<ContentCatalogueBinaryEntry>();
      if (entries.Any())
      {
        calculatedTail = entries.Max(item => item.TargetOffset + item.TargetLength);
      }
      return Math.Max(calculatedTail, BackupTargetConstants.DataOffset);
    }

    public async Task InsertFile(FileInformation file, int version, bool compressIfPossible)
    {
      var backupTarget = GetBackupTargetForFile(file);
      if (backupTarget == null)
      {
        backupTarget = CreateNewBackupTarget();
      }
      var entry = await backupTarget.AddFile(file, version, compressIfPossible);
      SearchTargets[backupTarget.TargetId].Add(entry);
    }

    public void CloseTargets()
    {
      foreach (var target in Targets)
      {
        target.BackupTarget.CloseStream();
      }
      binaryHandler.CloseStream();
    }
    public void WriteCatalogue()
    {
      binaryHandler.WriteContentCatalogue(this, false);
    }

    private async Task ExtractFile(string key, DirectoryInfo extractionRoot)
    {
      var item = GetNewestVersion(key);
      if (item.Deleted)
        return;

      var linkItem = item as ContentCatalogueLinkEntry;
      if (linkItem != null)
      {
        await ExtractLinkedFile(linkItem, extractionRoot);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = GetBackupTargetContainingFile(item.SourceFileInfo);
      if (backupTarget == null)
        throw new FileNotFoundException();

      await backupTarget.ExtractFile(binaryContentItem, extractionRoot);
    }

    private async Task ExtractFile(string key, int version, DirectoryInfo extractionRoot)
    {
      var item = GetSpecificVersion(key, version);
      if (item.Deleted)
        return;

      var linkItem = item as ContentCatalogueLinkEntry;
      if (linkItem != null)
      {
        await ExtractLinkedFile(linkItem, extractionRoot);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = GetBackupTargetContainingFile(item.SourceFileInfo);
      if (backupTarget == null)
        throw new FileNotFoundException();
      await backupTarget.ExtractFile(binaryContentItem, extractionRoot);
    }

    private async Task ExtractLinkedFile(ContentCatalogueLinkEntry link, DirectoryInfo extractionRoot)
    {
      var item = GetSpecificVersion(link.ContentCatalogueEntryKey, link.ContentCatalogueEntryVersion);

      var newLinkItem = item as ContentCatalogueLinkEntry;
      if (newLinkItem != null)
      {
        await ExtractLinkedFile(newLinkItem, extractionRoot);
        return;
      }

      var binaryContentItem = item as ContentCatalogueBinaryEntry;
      if (binaryContentItem == null)
        throw new FileNotFoundException();

      var backupTarget = GetBackupTargetContainingFile(item.SourceFileInfo);
      if (backupTarget == null)
        throw new FileNotFoundException();
      await backupTarget.ExtractLinkedFile(binaryContentItem, link, extractionRoot);
    }

    public async Task ExtractAll(DirectoryInfo extractionRoot, IProgress<ProgressReport> progressCallback)
    {
      var keys = GetUniqueFileKeys();
      var numberOfFiles = keys.Count;
      var lastReport = DateTime.Now;
      int fileNumber = 0;
      
      foreach (var key in keys)
      {
        await ExtractFile(key, extractionRoot);
        if (DateTime.Now - lastReport > TimeSpan.FromSeconds(5))
        {
          var file = GetNewestVersion(key);
          progressCallback.Report(new ProgressReport(file.SourceFileInfo.FileName, fileNumber, numberOfFiles));
          lastReport = DateTime.Now;
        }

        fileNumber++;
      }
    }

    private BackupTarget CreateNewBackupTarget()
    {
      var target = new BackupTarget(new MD5Hasher(), new Sha256Hasher(), new CompressionHandler());
      var id = SearchTargets.Keys.Count == 0 ? 0 : SearchTargets.Keys.Max() + 1;
      target.Initialize(MaxSizeOfFiles, new DirectoryInfo(BackupDirectory), id, 0, FilenamePattern);
      Add(new TargetContentCatalogue(id, target));
      return target;
    }

    private IBackupTarget GetBackupTargetForFile(FileInformation file)
    {
      foreach (var target in Targets)
      {
        if (target.BackupTarget.CanContain(file))
          return target.BackupTarget;
      }
      return null;
    }

    private IBackupTarget GetBackupTargetContainingFile(FileInformation file)
    {
      return Targets.FirstOrDefault(t => t.KeySearchContent.ContainsKey(file.FullyQualifiedFilename)).BackupTarget;
    }

    private List<string> GetUniqueFileKeys()
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

    public IEnumerable<ContentCatalogueBinaryEntry> GetAllContentEntriesWithoutHashes(IProgress<ProgressReport> progressCallback)
    {
      var entries = new List<ContentCatalogueBinaryEntry>();
      foreach (var target in Targets)
      {
        entries.AddRange(target.Content.OfType<ContentCatalogueBinaryEntry>().Where(entry => string.IsNullOrEmpty(entry.PrimaryContentHash)));
      }
      return entries;
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
            return target.BackupTarget;
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
  }
}
