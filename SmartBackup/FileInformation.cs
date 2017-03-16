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
      RelativePath = contentSections[5];
    }

    public FileInformation(FileInformation copyThis)
      : this()
    {
      Directory = copyThis.Directory;
      FileName = copyThis.FileName;
      Size = copyThis.Size;
      LastModified = copyThis.LastModified;
      FullyQualifiedFilename = copyThis.FullyQualifiedFilename;
      RelativePath = copyThis.RelativePath;
    }

    [XmlElement("FQF")]
    public string FullyQualifiedFilename { get; set; }
    [XmlElement("D")]
    public string Directory { get; set; }
    [XmlElement("FN")]
    public string FileName { get; set; }
    [XmlIgnore]
    public DateTime LastModified { get; set; }
    [XmlAttribute("s")]
    public long Size { get; set; }
    [XmlAttribute("lm")]
    public string LastModifiedText
    {
      get { return LastModified.ToString("o"); }
      set { LastModified = DateTime.Parse(value); }
    }

    [XmlElement("rp")]
    public string RelativePath { get; set; }
    public override string ToString()
    {
      return $"{Directory},{FileName},{Size},{LastModified.ToString("o")},{FullyQualifiedFilename},{RelativePath}";
    }

    public FileInformation Clone()
    {
      return new FileInformation(this);
    }
  }
}