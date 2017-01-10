using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vibe.Hammer.SmartBackup.Test
{
  [TestFixture]
  public class FillOneBackupTargetSelectionStrategyTest
  {
    [SetUp]
    public void Setup()
    {

    }

    [Test]
    public void NoTargetsReturnsNull()
    {
      // Arrange
      var sut = new FillOneBackupTargetSelectionStrategy(2);

      // act
      var result = sut.GetTargetWithRoom(new List<IBackupTarget>(), new FileInformation());

      // Assert
      Assert.IsNull(result, "If no targets exists, then the result must be null");
    }

    [Test]
    public void NullListReturnsNull()
    {
      // Arrange
      var sut = new FillOneBackupTargetSelectionStrategy(2);

      // act
      var result = sut.GetTargetWithRoom(null, new FileInformation());

      // Assert
      Assert.IsNull(result, "If targets are null, then the result must be null");
    }

    [Test]
    public void NullFileIsAnError()
    {
      // Arrange
      var sut = new FillOneBackupTargetSelectionStrategy(2);

      // act and Assert
      Assert.That(() => sut.GetTargetWithRoom(new List<IBackupTarget>(), null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void SingleTargetReturnsFirst()
    {
      // Arrange
      var sut = new FillOneBackupTargetSelectionStrategy(2);
      BackupTarget backupTarget = new BackupTarget(2, new System.IO.DirectoryInfo(@"c:\test\"), 1);
      backupTarget.AddFile(new FileInformation
      {
        Size = 2
      });

      List<IBackupTarget> targets = new List<IBackupTarget> { backupTarget };

      // act
      var result = sut.GetTargetWithRoom(targets, new FileInformation());

      // Assert
      Assert.IsNotNull(result);

    }

    [Test]
    public void MaxSizeMustBeAtLeastTwo()
    {
      // Arrange, Act and Assert
      Assert.That(() => new FillOneBackupTargetSelectionStrategy(1), Throws.TypeOf<ArgumentException>());
      Assert.That(() => new FillOneBackupTargetSelectionStrategy(0), Throws.TypeOf<ArgumentException>());
      Assert.That(() => new FillOneBackupTargetSelectionStrategy(-1), Throws.TypeOf<ArgumentException>());
    }
  }
}
