namespace CloudsferQA.Models;

public class TestSession
{
    public int    Id          { get; set; }
    public int?   UserId      { get; set; }   // nullable so old rows without a user still work
    public string Tester      { get; set; } = string.Empty;
    public string Version     { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime  StartedAt   { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string    Status      { get; set; } = "In Progress"; // In Progress | Completed | Skipped | Halted

    public User?             User    { get; set; }
    public List<TestResult>  Results { get; set; } = new();
}
