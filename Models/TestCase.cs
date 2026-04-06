namespace CloudsferQA.Models;

public class TestCase
{
    public string Id { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Submodule { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int SortOrder { get; set; } = 0;
}
