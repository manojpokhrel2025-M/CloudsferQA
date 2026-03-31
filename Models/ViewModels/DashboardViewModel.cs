namespace CloudsferQA.Models.ViewModels;

public class DashboardViewModel
{
    public List<TestSession> Sessions { get; set; } = new();
    public int? SelectedSessionId { get; set; }
}
