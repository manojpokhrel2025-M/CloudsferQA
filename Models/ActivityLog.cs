namespace CloudsferQA.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;      // e.g. "Import", "AddTestCase", "AddUser"
    public string Details { get; set; } = string.Empty;     // human-readable description
    public string PerformedBy { get; set; } = string.Empty; // user email
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = string.Empty;    // "TestCase", "User", "Module"
}
