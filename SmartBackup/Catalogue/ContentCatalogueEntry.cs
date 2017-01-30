using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlType("CCE")]
  [XmlInclude(typeof(FileInformation))]
  [XmlInclude(typeof(ContentCatalogueBinaryEntry))]
  [XmlInclude(typeof(ContentCatalogueLinkEntry))]
  public class ContentCatalogueEntry
  {
    public ContentCatalogueEntry()
    {

    }

    [XmlElement("SFI")]
    public FileInformation SourceFileInfo { get; set; }

    [XmlAttribute("v")]
    public int Version { get; set; }

    [XmlAttribute("k")]
    public string Key { get; set; }

  }
}
