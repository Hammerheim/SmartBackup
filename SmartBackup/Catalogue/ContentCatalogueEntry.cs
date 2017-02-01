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
    private FileInformation sourceFileInfo;
    private string key;

    public ContentCatalogueEntry()
    {

    }

    [XmlElement("SFI")]
    public FileInformation SourceFileInfo
    {
      get { return sourceFileInfo; }
      set
      {
        sourceFileInfo = value;
        Key = sourceFileInfo.FullyQualifiedFilename;
      }
    }

    [XmlAttribute("v")]
    public int Version { get; set; }

    [XmlAttribute("k")]
    public string Key
    {
      get
      {
        if (string.IsNullOrEmpty(key) && sourceFileInfo != null)
          key = sourceFileInfo.FullyQualifiedFilename;
        return key;
      }
      set { key = value; }
    }
  }
}
