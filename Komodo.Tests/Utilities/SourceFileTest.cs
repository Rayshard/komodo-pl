namespace Komodo.Tests.Utilities;

using Komodo.Utilities;

public class SourceFileTest
{
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(3, 1, 4)]
    [InlineData(4, 2, 1)]
    [InlineData(5, 2, 2)]
    [InlineData(10, 4, 1)]
    [InlineData(13, 4, 4)]
    [InlineData(14, 4, 5)]
    public void CorrectPositionGivenOffset(int offset, int expectedLine, int expectedColumn)
    {
        var sf = new SourceFile("Test", "abc\n 123\n\n q w");
        
        Assert.Equal(new Position(expectedLine, expectedColumn), sf.GetPosition(offset));
    }
}