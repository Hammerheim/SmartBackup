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
      Deleted = linkFrom.Deleted;
      Version = linkFrom.Version;
      ContentCatalogueEntryKey = linkTo.Key;
      ContentCatalogueEntryVersion = linkTo.Version;
    }

    public ContentCatalogueLinkEntry(ContentCatalogueUnclaimedLinkEntry linkFrom)
      : base()
    {
      ContentCatalogueEntryKey = linkFrom.ContentCatalogueEntryKey;
      ContentCatalogueEntryVersion = linkFrom.ContentCatalogueEntryVersion;
      Key = linkFrom.Key;
      SourceFileInfo = linkFrom.SourceFileInfo;
      Version = linkFrom.Version;
      Deleted = linkFrom.Deleted;
    }

    public ContentCatalogueLinkEntry(ContentCatalogueLinkEntry copyThis)
      : base(copyThis)
    {
      ContentCatalogueEntryKey = copyThis.Key;
      ContentCatalogueEntryVersion = copyThis.Version;
    }

    [XmlAttribute("ccek")]
    public string ContentCatalogueEntryKey { get; set; }

    [XmlAttribute("ccev")]
    public int ContentCatalogueEntryVersion { get; set; }

    public override ContentCatalogueEntry Clone()
    {
      return new ContentCatalogueLinkEntry(this);
    }
  }
}
