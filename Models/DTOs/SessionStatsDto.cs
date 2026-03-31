namespace CloudsferQA.Models.DTOs;

public class SessionStatsDto
{
    public int SessionId { get; set; }
    public string Tester { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public int Blocked { get; set; }
    public int Skip { get; set; }
    public int Pending { get; set; }
    public double PassRate { get; set; }
    public List<ModuleStatDto> Modules { get; set; } = new();
    public List<PriorityStatDto> PriorityBreakdown { get; set; } = new();
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
    public List<FailedCaseDto> FailedAndBlocked { get; set; } = new();
}

public class PriorityStatDto
{
    public string Priority { get; set; } = string.Empty;
    public int Pass { get; set; }
    public int Fail { get; set; }
    public int Blocked { get; set; }
    public int Skip { get; set; }
    public int Pending { get; set; }
}

public class RecentActivityDto
{
    public string Scenario { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? TestedAt { get; set; }
}

public class FailedCaseDto
{
    public string TestCaseId { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
