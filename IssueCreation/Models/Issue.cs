namespace IssueCreation.Models
{
    public class Issue
    {
        public string? Project { get; set; }
        public string Issuetype { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public string Priority { get; set; }


    }
}
