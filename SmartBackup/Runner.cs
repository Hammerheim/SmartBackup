using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibe.Hammer.SmartBackup.Catalogue;

namespace Vibe.Hammer.SmartBackup
{
  public class Runner : IRunner
  {
    private IFileInformationGatherer gatherer = new FileInformationGatherer(new MD5FileHasher());

    public async Task<IFileLog> Run(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, Action<double> progressCallback, bool deepScan)
    {
      var logger = new FileTreeLog();
      var recurser = new DirectoryRecurser();
      var result = await recurser.RecurseDirectory(sourceRoot, new SimpleFileHandler(new FileInformationGatherer(new MD5FileHasher()), logger), deepScan);
      if (result)
      {
        var contentCatalogue = new ContentCatalogue();
        await contentCatalogue.BuildFromExistingBackups(targetRoot, 1024);

        foreach (var file in logger.Files)
        {
          var currentVersion = contentCatalogue.GetNewestVersion(file);
          if (currentVersion == null)
            await contentCatalogue.InsertFile(file);
          else
          {
            if (currentVersion.File.LastModified < file.LastModified)
            {
              file.Version = currentVersion.File.Version + 1;
              await contentCatalogue.InsertFile(file);
            }
          }
        }
        await contentCatalogue.CloseTargets();
        //  await target.AddFile(file);
        //}
        //await target.WriteCatalogue();
        return logger;
      }
      return null;
    }
  }
}
