using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Vibe.Hammer.SmartBackup.Catalogue;
using Vibe.Hammer.SmartBackup.Compression;

namespace Vibe.Hammer.SmartBackup.Target
{
  public class ContentCatalogueBinaryHandler : BinaryHandler
  {

    public ContentCatalogueBinaryHandler(FileInfo targetFile, ICompressionHandler compressionHandler)
      : base(targetFile, compressionHandler)
    {

    }

    public virtual void WriteContentCatalogue(ContentCatalogue catalogue, bool closeStreams)
    {
      if (!IsTargetLocked())
      {
        var serializer = new XmlSerializer(typeof(ContentCatalogue));
        using (var writer = new StreamWriter(TargetFile.FullName))
        {
          serializer.Serialize(writer, catalogue);
          writer.Close();
        }
      }
      else
        throw new UnableToOpenStreamException();
      GC.WaitForPendingFinalizers();
    }

    public virtual ContentCatalogue ReadContentCatalogue()
    {
      if (!TargetFile.Exists)
        return null;

      ContentCatalogue catalogue = null;

      if (!IsTargetLocked())
      {
        XmlSerializer serializer = new XmlSerializer(typeof(ContentCatalogue));
        using (var fs = new FileStream(TargetFile.FullName, FileMode.Open))
        {
          catalogue = serializer.Deserialize(fs) as ContentCatalogue;
        }
      }
      else
        throw new UnableToOpenStreamException();

      GC.WaitForPendingFinalizers();
      return catalogue;
    }
  }
}
