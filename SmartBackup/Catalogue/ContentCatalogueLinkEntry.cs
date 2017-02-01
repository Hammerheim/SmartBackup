using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup.Catalogue
{
  [XmlType("CCLE")]
  public class ContentCatalogueLinkEntry : ContentCatalogueEntry
  {
    public ContentCatalogueLinkEntry()
    {

    }

    public ContentCatalogueLinkEntry(ContentCatalogueBinaryEntry linkFrom, ContentCatalogueBinaryEntry linkTo)
      : this()
    {
      Key = linkFrom.Key;
      SourceFileInfo = linkFrom.SourceFileInfo;
      Version = linkFrom.Version;
      ContentCatalogueEntryKey = linkTo.Key;
      ContentCatalogueEntryVersion = linkTo.Version;
    }

    [XmlAttribute("ccek")]
    public string ContentCatalogueEntryKey { get; set; }

    [XmlAttribute("ccev")]
    public int ContentCatalogueEntryVersion { get; set; }
  }
}
