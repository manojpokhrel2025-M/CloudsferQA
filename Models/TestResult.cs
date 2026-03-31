namespace CloudsferQA.Models;

public class TestResult
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string TestCaseId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string Notes { get; set; } = string.Empty;
    public DateTime? TestedAt { get; set; }
    public TestSession Session { get; set; } = null!;
}
