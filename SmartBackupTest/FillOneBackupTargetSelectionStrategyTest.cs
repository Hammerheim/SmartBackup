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
    public void MaxSizeMustBeAtLeastTwo()
    {
      // Arrange, Act and Assert
      Assert.That(() => new FillOneBackupTargetSelectionStrategy(1), Throws.TypeOf<ArgumentException>());
      Assert.That(() => new FillOneBackupTargetSelectionStrategy(0), Throws.TypeOf<ArgumentException>());
      Assert.That(() => new FillOneBackupTargetSelectionStrategy(-1), Throws.TypeOf<ArgumentException>());
    }
  }
}
