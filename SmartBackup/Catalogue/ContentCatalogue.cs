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

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlRoot("CC")]
  [XmlInclude(typeof(TargetContentCatalogue))]
  public class ContentCatalogue : IContentCatalogue
  {
    public ContentCatalogue()
    {
      SearchTargets = new Dictionary<int, TargetContentCatalogue>();
      Targets = new List<TargetContentCatalogue>();
      ContentHashes = new Dictionary<string, List<ContentCatalogueBinaryEntry>>();
    }

    public ContentCatalogue(int maxSizeInMegaBytes, DirectoryInfo backupDirectory)
      : this()
    {
      MaxSizeOfFiles = maxSizeInMegaBytes;
      BackupDirectory = backupDirectory.FullName;
    }

    [XmlAttribute("ms")]
    public int MaxSizeOfFiles { get; set; }

    [XmlElement("BD")]
    public string BackupDirectory { get; set; }

    [XmlArray("Targets")]
    [XmlArrayItem("BTI")]
    public List<TargetContentCatalogue> Targets { get; private set; }

    [XmlIgnore]
    public Dictionary<int, TargetContentCatalogue> SearchTargets { get; private set; }

    [XmlIgnore]
    public Dictionary<string, List<ContentCatalogueBinaryEntry>> ContentHashes { get; private set; }

    public void RebuildSearchIndex()
    {
      foreach (var item in Targets)
      {
        SearchTargets.Add(item.BackupTargetIndex, item);
        item.RebuildSearchIndex();
      }

      BuildContentHashesDictionary();
    }

    public ContentCatalogueEntry GetNewestVersion(FileInformation file)
    {
      return GetNewestVersion(file.FullyQualifiedFilename);
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

    public async Task BuildFromExistingBackups(DirectoryInfo backupDirectory, int expectedMaxSizeInMegaBytes)
    {
      this.BackupDirectory = backupDirectory.FullName;
      this.MaxSizeOfFiles = expectedMaxSizeInMegaBytes;

      if (!backupDirectory.Exists)
        return;

      var files = backupDirectory.GetFiles("BackupTarget.*.exe");
      if (files.Length == 0)
        return;

      var regEx = new Regex(@"^BackupTarget\.(\d+)\.exe$");
      foreach (var file in files)
      {
        var match = regEx.Match(file.Name);
        if (match.Success)
        {
          var number = int.Parse(match.Groups[1].Value);
          var backupTarget = new BackupTarget(new MD5Hasher(), new Sha256Hasher(), new CompressionHandler());
          await backupTarget.Initialize(expectedMaxSizeInMegaBytes, backupDirectory, number, this);
        }
      }
    }

    public async Task InsertFile(FileInformation file)
    {
      var backupTarget = GetBackupTargetForFile(file);
      if (backupTarget == null)
      {
        backupTarget = await CreateNewBackupTarget();
      }
      await backupTarget.AddFile(file);
    }

    public async Task CloseTargets()
    {
      foreach (var target in Targets)
      {
        //Backup target er null
        await target.BackupTarget.WriteCatalogue(true);
      }
    }
    public async Task WriteCatalogue()
    {
      foreach (var target in Targets)
      {
        //Backup target er null
        await target.BackupTarget.WriteCatalogue(false);
      }
    }

    private async Task ExtractFile(string key, DirectoryInfo extractionRoot)
    {
      var item = GetNewestVersion(key);
      if (item is ContentCatalogueLinkEntry)
      {
        await ExtractFile(item.Key, item.Version, extractionRoot);
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
      if (item is ContentCatalogueLinkEntry)
      {
        await ExtractFile(item.Key, item.Version, extractionRoot);
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

    private async Task<BackupTarget> CreateNewBackupTarget()
    {
      var target = new BackupTarget(new MD5Hasher(), new Sha256Hasher(), new CompressionHandler());
      var id = SearchTargets.Keys.Count == 0 ? 0 : SearchTargets.Keys.Max() + 1;
      await target.Initialize(MaxSizeOfFiles, new DirectoryInfo(BackupDirectory), id, this);
      Add(new TargetContentCatalogue(id, target));
      return target;
    }

    private BackupTarget GetBackupTargetForFile(FileInformation file)
    {
      foreach (var target in Targets)
      {
        if (target.BackupTarget.CanContain(file))
          return target.BackupTarget;
      }
      return null;
    }

    private BackupTarget GetBackupTargetContainingFile(FileInformation file)
    {
      return Targets.FirstOrDefault(t => t.BackupTarget.Contains(file.FullyQualifiedFilename)).BackupTarget;
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
    public BackupTarget GetBackupTargetFor(ContentCatalogueEntry entry)
    {
      foreach (var target in Targets)
      {
        if (target.BackupTarget.Contains(entry.Key, entry.Version))
          return target.BackupTarget;
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
  }
}
