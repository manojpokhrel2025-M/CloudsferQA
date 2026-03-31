namespace CloudsferQA.Models.ViewModels;

public class AdminUsersViewModel
{
    public List<User>           Users         { get; set; } = new();
    public Dictionary<int, int> SessionCounts { get; set; } = new();
    public User                 CurrentAdmin  { get; set; } = null!;
}

public class AdminUserDetailViewModel
{
    public User                  User         { get; set; } = null!;
    public List<SessionSummary>  Sessions     { get; set; } = new();
    public User                  CurrentAdmin { get; set; } = null!;
}

public class SessionSummary
{
    public TestSession Session { get; set; } = null!;
    public int Total   { get; set; }
    public int Pass    { get; set; }
    public int Fail    { get; set; }
    public int Blocked { get; set; }
    public int Skip    { get; set; }
    public int Tested  { get; set; }
    public double PassRate => Total > 0 ? Math.Round((double)Pass / Total * 100, 1) : 0;
}
