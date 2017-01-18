using System;
using System.Xml.Serialization;

namespace Vibe.Hammer.SmartBackup
{
  [XmlType("FI")]
  public class FileInformation
  {
    public FileInformation()
    {

    }

    public FileInformation(string content)
      : this()
    {
      var contentSections = content.Split(',');
      Directory = contentSections[0];
      FileName = contentSections[1];
      Size = long.Parse(contentSections[2]);
      LastModified = DateTime.Parse(contentSections[3]);
      FullyQualifiedFilename = contentSections[4];
      ContentHash = contentSections[5];
    }

    [XmlElement("FQF")]
    public string FullyQualifiedFilename { get; set; }
    [XmlAttribute("ch")]
    public string ContentHash { get; set; }
    [XmlElement("D")]
    public string Directory { get; set; }
    [XmlElement("FN")]
    public string FileName { get; set; }
    [XmlIgnore]
    public DateTimeOffset LastModified { get; set; }
    [XmlAttribute("s")]
    public long Size { get; set; }
    [XmlAttribute("lm")]
    public string LastModifiedText
    {
      get { return LastModified.ToString("O"); }
      set { LastModified = DateTimeOffset.Parse(value); }
    }
    public override string ToString()
    {
      return $"{Directory},{FileName},{Size},{LastModified.ToString("O")},{FullyQualifiedFilename},{ContentHash}";
    }
  }
}