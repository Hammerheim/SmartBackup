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
    private IFileInformationGatherer gatherer = new FileInformationGatherer(new MD5FileHasher());

    public async Task<IFileLog> Run(DirectoryInfo sourceRoot, DirectoryInfo targetRoot, Action<double> progressCallback)
    {
      var logger = new SimpleFileLog();
      var recurser = new DirectoryRecurser();
      var result = await recurser.RecurseDirectory(sourceRoot, new SimpleFileHandler(new FileInformationGatherer(new MD5FileHasher()), logger));
      if (result)
        return logger;
      return null;
    }
  }
}
