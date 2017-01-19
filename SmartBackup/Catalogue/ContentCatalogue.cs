﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

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

    public void RebuildSearchIndex()
    {
      foreach (var item in Targets)
      {
        SearchTargets.Add(item.BackupTargetIndex, item);
        item.RebuildSearchIndex();
      }
    }

    public BackupTargetItem GetNewestVersion(FileInformation file)
    {
      BackupTargetItem newestItem = null;
      foreach (var catalogue in Targets)
      {
        if (catalogue.KeySearchContent.ContainsKey(file.FullyQualifiedFilename))
        {
          foreach (var item in catalogue.KeySearchContent[file.FullyQualifiedFilename])
          {
            if (newestItem == null)
              newestItem = item;
            if (item.File.Version > newestItem.File.Version)
              newestItem = item;
          }
        }
      }
      return newestItem;
    }

    internal void EnsureTargetCatalogueExists(ContentCatalogue persistedCatalogue, int targetBinaryID)
    {
      if (!SearchTargets.ContainsKey(targetBinaryID))
      {
        Add(persistedCatalogue.SearchTargets[targetBinaryID]);
      }
      else
      {
        ReplaceTargetContentCatalogue(persistedCatalogue, targetBinaryID);
      }
    }

    private void ReplaceTargetContentCatalogue(ContentCatalogue persistedCatalogue, int targetBinaryID)
    {
      if (SearchTargets.ContainsKey(targetBinaryID))
      {
        var currentTarget = Targets.FirstOrDefault(t => t.BackupTargetIndex == targetBinaryID);
        if (currentTarget != null)
          Targets.Remove(currentTarget);
        SearchTargets[targetBinaryID] = persistedCatalogue.SearchTargets[targetBinaryID];
        Add(persistedCatalogue.SearchTargets[targetBinaryID]);
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
      if (!backupDirectory.Exists)
        return;

      this.BackupDirectory = backupDirectory.FullName;
      this.MaxSizeOfFiles = expectedMaxSizeInMegaBytes;
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
          var backupTarget = new BackupTarget();
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
        Backup target er null
        await target.BackupTarget.WriteCatalogue();
      }
    }

    private async Task<BackupTarget> CreateNewBackupTarget()
    {
      var target = new BackupTarget();
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
  }
}
