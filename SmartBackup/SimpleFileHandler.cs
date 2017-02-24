using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup
{
  internal class SimpleFileHandler : IFileHandler
  {
    private IFileInformationGatherer gatherer;
    private IFileLog logger;

    public SimpleFileHandler(IFileInformationGatherer gatherer, IFileLog logger)
    {
      this.gatherer = gatherer;
      this.logger = logger;
    }

    public bool Handle(FileInfo info, DirectoryInfo root, bool deepScan)
    {
      var fileData = gatherer.Gather(info, root, deepScan);
      if (fileData != null)
        logger.Add(fileData);
      return true;
    }
  }
}
