using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Progress;

namespace Vibe.Hammer.SmartBackup
{
  public class Runner : IRunner
  {
    private IFileInformationGatherer gatherer = new FileInformationGatherer(new MD5Hasher());
    private int currentFile;
    private int maxNumberOfFiles;
    private DateTime lastProgressReport;
    private ContentCatalogue catalogue;
    private readonly DirectoryInfo root;

    public Runner(DirectoryInfo targetRoot)
    {
      if (targetRoot == null)
        throw new ArgumentNullException(nameof(targetRoot));
      catalogue = null;
      root = targetRoot;
    }
    public async Task<bool> Backup(IFileLog log, DirectoryInfo targetRoot, int fileSize, IProgress<ProgressReport> progressCallback)
    {
      currentFile = 0;
      progressCallback.Report(new ProgressReport("Starting backup"));
      progressCallback.Report(new ProgressReport("Reading existing content catalogues if any..."));
      await InitializeContentCatalogue(targetRoot, fileSize, progressCallback);

      progressCallback.Report(new ProgressReport("Performing backup..."));
      lastProgressReport = DateTime.Now;
      maxNumberOfFiles = log.Files.Count();
      foreach (var file in log.Files)
      {
        currentFile++;
        ReportProgress(progressCallback, file);
        var currentVersion = catalogue.GetNewestVersion(file);
        if (currentVersion == null)
          await catalogue.InsertFile(file, 1);
        else
        {
          if (currentVersion.SourceFileInfo.LastModified < file.LastModified)
          {
            await catalogue.InsertFile(file, currentVersion.Version + 1);
          } 
          else if (currentVersion.SourceFileInfo.LastModified == file.LastModified && currentVersion.Deleted)
          {
            // The deleted file has likely been restored, but there is a slight posibility that it is not the same file. Therefore the file is reinserted. 
            // If the binaries are the same, a maintenance run will convert one of them to a link and reclaim the space. This is the safe option rather than 
            // just setting the currentVersion.Deleted = false.
            await catalogue.InsertFile(file, currentVersion.Version + 1);
          }
        }
      }
      catalogue.WriteCatalogue();
      catalogue.CloseTargets();
      progressCallback.Report(new ProgressReport("Backup complete", currentFile, currentFile));
      return true;
    }

    public async Task<bool> IdentifyDeletedFiles(DirectoryInfo targetRoot, int fileSize, IProgress<ProgressReport> progressCallback)
    {
      currentFile = 0;
      var numberOfDeletedFilesFound = 0;
      progressCallback.Report(new ProgressReport("Starting run to look for deleted files..."));
      progressCallback.Report(new ProgressReport("Reading existing content catalogues if any..."));
      await InitializeContentCatalogue(targetRoot, fileSize, progressCallback);

      progressCallback.Report(new ProgressReport("Tombstoning deleted files..."));
      lastProgressReport = DateTime.Now;
      
      var entries = catalogue.EnumerateContent().ToList();
      maxNumberOfFiles = entries.Count();
      foreach (var entry in entries)
      {
        currentFile++;
        if (!entry.Deleted)
        {
          if (!File.Exists(entry.SourceFileInfo.FullyQualifiedFilename))
          {
            entry.Deleted = true;
            numberOfDeletedFilesFound++;
          }
        }
        ReportProgress(progressCallback, $"Found {numberOfDeletedFilesFound} deleted file(s)");
      }
      catalogue.CloseTargets();
      progressCallback.Report(new ProgressReport("Backup complete", currentFile, currentFile));
      return true;
    }

    private void ReportProgress(IProgress<ProgressReport> progressCallback, FileInformation file)
    {
      if (DateTime.Now - lastProgressReport > TimeSpan.FromSeconds(5))
      {
        progressCallback.Report(new ProgressReport(file.FullyQualifiedFilename, currentFile, maxNumberOfFiles));
        catalogue.WriteCatalogue();
        lastProgressReport = DateTime.Now;
      }
    }

    private void ReportProgress(IProgress<ProgressReport> progressCallback, string message)
    {
      if (DateTime.Now - lastProgressReport > TimeSpan.FromSeconds(5))
      {
        progressCallback.Report(new ProgressReport(message, currentFile, maxNumberOfFiles));
        catalogue.WriteCatalogue();
        lastProgressReport = DateTime.Now;
      }
    }
    public async Task<IFileLog> Scan(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, IProgress<ProgressReport> progressCallback)
    {
      var logger = new FileTreeLog();
      var recurser = new DirectoryRecurser();
      var result = await recurser.RecurseDirectory(sourceRoot, new SimpleFileHandler(new FileInformationGatherer(new MD5Hasher()), logger), false, progressCallback);
      if (result)
      {
        return logger;
      }
      return null;

    }

    public async Task<bool> CalculateMissingHashes(DirectoryInfo targetRoot, int fileSize, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, fileSize, progressCallback);

      currentFile = 0;
      var allContentWithoutHashes = catalogue.GetAllContentEntriesWithoutHashes(progressCallback);
      if (!allContentWithoutHashes.Any())
      {
        progressCallback.Report(new ProgressReport("All files are already hashed."));
        return true;
      }
      maxNumberOfFiles = allContentWithoutHashes.Count();
      lastProgressReport = DateTime.Now;

      progressCallback.Report(new ProgressReport("Scanning for missing file hashes..."));
      foreach (var contentItem in allContentWithoutHashes)
      {
        var backupTarget = catalogue.GetBackupTargetFor(contentItem);
        var hashes = await backupTarget.CalculateHashes(contentItem);
        if (hashes != null)
        {
          contentItem.PrimaryContentHash = hashes.PrimaryHash;
          contentItem.SecondaryContentHash = hashes.SecondaryHash;
          catalogue.AddContentHash(hashes.PrimaryHash, contentItem);
        }

        currentFile++;
        ReportProgress(progressCallback, contentItem.SourceFileInfo);
      }
      catalogue.WriteCatalogue();
      return true;
    }

    public async Task<bool> ReplaceDublicatesWithLinks(DirectoryInfo targetRoot, int fileSize, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, fileSize, progressCallback);

      currentFile = 0;
      var allPossibleDublicates = catalogue.GetAllPossibleDublicates(progressCallback);
      if (!allPossibleDublicates.Any())
      {
        progressCallback.Report(new ProgressReport("There are no dublicates in the backup."));
        return true;
      }
      maxNumberOfFiles = allPossibleDublicates.Count();
      lastProgressReport = DateTime.Now;

      progressCallback.Report(new ProgressReport("Linking dublicate files..."));

      foreach (var entryList in allPossibleDublicates)
      {
        currentFile++;
        ContentCatalogueBinaryEntry primaryEntry = null;
        foreach (var entry in entryList)
        {
          if (primaryEntry == null)
          {
            primaryEntry = entry;
            continue;
          }

          var link = new ContentCatalogueUnclaimedLinkEntry(entry, primaryEntry);
          catalogue.ReplaceBinaryEntryWithLink(entry, link);

          ReportProgress(progressCallback, link.SourceFileInfo);
        }
      }
      catalogue.WriteCatalogue();
      return true;
    }

    private async Task InitializeContentCatalogue(DirectoryInfo targetRoot, int fileSize, IProgress<ProgressReport> progressCallback)
    {
      if (catalogue == null)
      {
        progressCallback.Report(new ProgressReport("Reading content catalogue..."));
        catalogue = new ContentCatalogue(fileSize, targetRoot);
        await catalogue.BuildFromExistingBackups(targetRoot, fileSize);
      }
    }

    public async Task<bool> DefragmentBinaries(DirectoryInfo targetRoot, int fileSize, IProgress<ProgressReport> progressCallback)
    {
      await InitializeContentCatalogue(targetRoot, fileSize, progressCallback);

      currentFile = 0;
      var allUnclaimedLinks = catalogue.GetUnclaimedLinks();
      if (!allUnclaimedLinks.Any())
      {
        progressCallback.Report(new ProgressReport("There are no space that must be reclaimed."));
        return true;
      }
      maxNumberOfFiles = allUnclaimedLinks.Count();
      lastProgressReport = DateTime.Now;

      progressCallback.Report(new ProgressReport("Reclaiming space..."));
       
      foreach (var target in catalogue.Targets)
      {
        currentFile++;
        var entries = catalogue.Targets[target.BackupTargetIndex].Content.OfType<ContentCatalogueBinaryEntry>().ToList();
        if (await target.ReclaimSpace(entries, progressCallback))
        {
          ConvertAllUnclaimedLinksToClaimedLinks(target.BackupTargetIndex);
        }
      }
      catalogue.WriteCatalogue();
      return true;
    }

    private void ConvertAllUnclaimedLinksToClaimedLinks(int id)
    {
      var unclaimedLinks = catalogue.SearchTargets[id].Content.OfType<ContentCatalogueUnclaimedLinkEntry>().ToArray();
      foreach (var unclaimedLink in unclaimedLinks)
      {
        catalogue.Targets[id].ReplaceContent(unclaimedLink, new ContentCatalogueLinkEntry(unclaimedLink));
      }
    }
  }
}
