namespace CloudsferQA.Models;

public class User
{
    public int    Id              { get; set; }
    public string Email           { get; set; } = string.Empty;
    public string PasswordHash    { get; set; } = string.Empty;
    public string Role            { get; set; } = "QA";   // QA | Dev | Admin
    public bool   IsEmailVerified { get; set; }
    public bool   IsActive        { get; set; } = true;
    public string? VerificationToken { get; set; }
    public DateTime? VerifiedAt   { get; set; }
    public DateTime  CreatedAt    { get; set; }
    public List<TestSession> Sessions { get; set; } = new();
}
