namespace CloudsferQA.Models.DTOs;

public class ModuleStatDto
{
    public string Module { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public int Blocked { get; set; }
    public int Skip { get; set; }
    public int Pending { get; set; }
    public double PassRate { get; set; }
}
