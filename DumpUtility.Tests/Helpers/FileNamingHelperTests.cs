using DumpUtility.Helpers;
using Xunit;

namespace DumpUtility.Tests.Helpers;

public class FileNamingHelperTests
{
    [Theory]
    [InlineData(123, "Rishikesh", "0123-Rishikesh")]
    [InlineData(456, "Aamir", "0456-Aamir")]
    [InlineData(9999, "Test", "9999-Test")]
    [InlineData(10000, "LargeId", "10000-LargeId")]
    [InlineData(1, "X", "0001-X")]
    public void GetEmployeeFolder_ReturnsCorrectFormat(int id, string name, string expected)
    {
        var result = FileNamingHelper.GetEmployeeFolder(id, name);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(@"1. Documents for Onboarding\1.1 CV Signed", "CV Signed")]
    [InlineData(@"9. Performance Appraisal Form\9.2 Current Year\9.2.1 Mid-Year", "Mid-Year")]
    [InlineData(@"2. Education Proof\2.3 Graduation Degree or Provisional Certificate", "Graduation Degree or Provisional Certificate")]
    [InlineData(@"NoPrefix", "NoPrefix")]
    [InlineData(@"17. Exit\17.1 Resignation", "Resignation")]
    public void GetSubcategoryName_StripsNumericPrefix(string folderPath, string expected)
    {
        var result = FileNamingHelper.GetSubcategoryName(folderPath);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello:World", "Hello_World")]
    [InlineData("File*Name?", "File_Name_")]
    [InlineData("Normal Name", "Normal Name")]
    [InlineData("Doc\"Title\"", "Doc_Title_")]
    public void SanitizeSegment_ReplacesInvalidChars(string input, string expected)
    {
        var result = FileNamingHelper.SanitizeSegment(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildFileName_ReturnsCorrectFormat()
    {
        var result = FileNamingHelper.BuildFileName("CV Signed", "0123-Rishikesh", 1, ".pdf");
        Assert.Equal("CV Signed_0123-Rishikesh_1.pdf", result);
    }

    [Fact]
    public void BuildFileName_SequenceIncrements()
    {
        var first  = FileNamingHelper.BuildFileName("CV Signed", "0123-Rishikesh", 1, ".pdf");
        var second = FileNamingHelper.BuildFileName("CV Signed", "0123-Rishikesh", 2, ".docx");
        Assert.Equal("CV Signed_0123-Rishikesh_1.pdf",  first);
        Assert.Equal("CV Signed_0123-Rishikesh_2.docx", second);
    }
}
