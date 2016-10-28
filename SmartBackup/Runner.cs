using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  public class Runner : IRunner
  {
    private IFileLog logger = new SimpleFileLog();
    private IFileInformationGatherer gatherer = new FileInformationGatherer(new MD5FileHasher());

    public async Task<IFileLog> Run(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, Action<double> progressCallback)
    {
      var recurser = new DirectoryRecurser();
      var result = await recurser.RecurseDirectory(sourceRoot, FileHandler);
      if (result)
        return logger;
      return null;
    }

    private async Task<bool> FileHandler(FileInfo info)
    {
      var fileData = await gatherer.Gather(info);
      if (fileData != null)
        logger.Log(fileData);
      return true;
    }
  }
}
