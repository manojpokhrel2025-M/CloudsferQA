namespace CloudsferQA.Models.ViewModels;

public class TestViewModel
{
    public TestSession Session { get; set; } = null!;
    public List<TestCase> TestCases { get; set; } = new();
    public List<TestResult> Results { get; set; } = new();
    public List<string> ModuleOrder { get; set; } = new();
}
