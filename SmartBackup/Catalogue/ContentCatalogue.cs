﻿using System;
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
    private ContentCatalogueBinaryHandler binaryHandler;
    protected DirectoryInfo TargetDirectory { get; set; }

    public ContentCatalogue()
    {
      SearchTargets = new Dictionary<int, TargetContentCatalogue>();
      Targets = new List<TargetContentCatalogue>();
      ContentHashes = new Dictionary<string, List<ContentCatalogueBinaryEntry>>();
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

    internal void AddItem(int targetId, ContentCatalogueBinaryEntry catalogueItem)
    {
      if (SearchTargets.ContainsKey(targetId))
      {
        SearchTargets[targetId].Add(catalogueItem);
      }
    }
    public void RebuildSearchIndex()
    {
      foreach (var item in Targets)
      {
        SearchTargets.Add(item.BackupTargetIndex, item);
        item.RebuildSearchIndex();
      }

      BuildContentHashesDictionary();
    }

    public ContentCatalogueEntry GetNewestVersion(FileInformation file) => GetNewestVersion(file.FullyQualifiedFilename);
    private FileInfo GetContentCatalogueFilename() => new FileInfo(Path.Combine(TargetDirectory.FullName, $"{FilenamePattern}.ContentCatalogue.exe"));

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

    private void AddTargetContentCatalogue(TargetContentCatalogue catalogue)
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

    internal void RemoveItem(ContentCatalogueBinaryEntry catalogueItem)
    {
      var (found, id) = GetBackupTargetFor(catalogueItem);
      if (found)
        Targets[id].Remove(catalogueItem);
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
    public void WriteCatalogue() => binaryHandler.WriteContentCatalogue(this, false);

    public (bool Found, int Id) GetBackupTargetContainingFile(FileInformation file)
    {
      var target = Targets.FirstOrDefault(t => t.KeySearchContent.ContainsKey(file.FullyQualifiedFilename));
      return (target != null, target?.BackupTargetIndex ?? -1); //target == null ? null : BackupTargetFactory.GetCachedTarget(target.BackupTargetIndex);
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
    public (bool Found, int Id) GetBackupTargetFor(ContentCatalogueEntry entry)
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

    internal int CountTotalEntries()
    {
      return Targets.Sum(target => target.Content.Count);
    }

    public int AddBackupTarget()
    {
      var id = SearchTargets.Keys.Count == 0 ? 0 : SearchTargets.Keys.Max() + 1;
      var target = new TargetContentCatalogue(id);
      AddTargetContentCatalogue(target);
      return id;
    }

    public (bool Found, int TargetId) TryFindBackupTargetWithRoom(long requiredSpace)
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
