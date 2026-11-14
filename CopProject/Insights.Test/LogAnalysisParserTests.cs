using Insights.LogAnalysis;
using Xunit;

namespace Insights.Test;

public sealed class LogAnalysisParserTests
{
    [Fact]
    public void Parse_FailureResponse_ReturnsStructuredFindings()
    {
        LogAnalysisResult result = LogAnalysisParser.Parse(CreateResponse());
        Assert.True(result.FailureDetected);
        Assert.Equal("The application failed while connecting to the database.", result.Summary);
        Assert.Equal("DatabaseService.Connect (DatabaseService.cs:42)", result.FailureLocation);
        Assert.Equal("The database connection timed out.", result.LikelyCause);
        Assert.Equal(2, result.Recommendations.Count);
    }

    [Fact]
    public void Parse_NoFailureResponse_AllowsEmptyFailureDetails()
    {
        LogAnalysisResult result = LogAnalysisParser.Parse(CreateResponse(false, "", "", "[]"));
        Assert.False(result.FailureDetected);
        Assert.Null(result.FailureLocation);
        Assert.Null(result.LikelyCause);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public void Parse_MissingOptionalDetails_ReturnsNull()
    {
        LogAnalysisResult result = LogAnalysisParser.Parse(
            """{"summary":"No failure found.","failureDetected":false,"recommendations":[]}""");
        Assert.Null(result.FailureLocation);
        Assert.Null(result.LikelyCause);
    }

    [Fact]
    public void Parse_ExtractsJsonFromMarkdownAndSurroundingText()
    {
        string json = CreateResponse();
        LogAnalysisResult result = LogAnalysisParser.Parse($"Analysis:\n```json\n{json}\n```\nEnd");
        Assert.Equal("The application failed while connecting to the database.", result.Summary);
    }

    [Fact]
    public void Parse_EmptyOrMalformedResponse_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse(null!));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("   "));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("no JSON here"));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("{ invalid json }"));
    }

    [Fact]
    public void Parse_MissingOrInvalidSummary_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("""{"failureDetected":false,"recommendations":[]}"""));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("""{"summary":42,"failureDetected":false,"recommendations":[]}"""));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("""{"summary":"  ","failureDetected":false,"recommendations":[]}"""));
    }

    [Fact]
    public void Parse_MissingOrInvalidFailureFlag_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("""{"summary":"s","recommendations":[]}"""));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("""{"summary":"s","failureDetected":"true","recommendations":[]}"""));
    }

    [Fact]
    public void Parse_InvalidOptionalString_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse(
            """{"summary":"s","failureDetected":true,"failureLocation":12,"recommendations":[]}"""));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse(
            """{"summary":"s","failureDetected":true,"likelyCause":false,"recommendations":[]}"""));
    }

    [Fact]
    public void Parse_MissingOrInvalidRecommendations_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("""{"summary":"s","failureDetected":false}"""));
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse("""{"summary":"s","failureDetected":false,"recommendations":"none"}"""));
    }

    [Fact]
    public void Parse_NonStringRecommendation_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => LogAnalysisParser.Parse(
            """{"summary":"s","failureDetected":false,"recommendations":["valid",3]}"""));
    }

    [Fact]
    public void Parse_EmptyRecommendationsAreIgnored()
    {
        LogAnalysisResult result = LogAnalysisParser.Parse(
            """{"summary":"s","failureDetected":false,"recommendations":["", "  ", "Check the service."]}""");
        Assert.Single(result.Recommendations);
        Assert.Equal("Check the service.", result.Recommendations[0]);
    }

    private static string CreateResponse(
        bool failureDetected = true,
        string failureLocation = "DatabaseService.Connect (DatabaseService.cs:42)",
        string likelyCause = "The database connection timed out.",
        string recommendations = "[\"Check database availability.\",\"Verify connection settings.\"]")
    {
        return $$"""
        {
          "summary": "The application failed while connecting to the database.",
          "failureDetected": {{failureDetected.ToString().ToLowerInvariant()}},
          "failureLocation": "{{failureLocation}}",
          "likelyCause": "{{likelyCause}}",
          "recommendations": {{recommendations}}
        }
        """;
    }
}
