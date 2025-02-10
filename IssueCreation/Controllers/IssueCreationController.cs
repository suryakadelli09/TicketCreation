using IssueCreation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IssueCreation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssueCreationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        public IssueCreationController(IConfiguration configuration,  HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost//("{action}/{id:int}")
                 ]
        public async Task<IActionResult> CreateIssueAndAttachFileAsync([FromForm] Issue fields, IFormFile file )
        {
            //if (file == null || string.IsNullOrEmpty(fields))
            //    return BadRequest("File and fields are required.");
            //var jiraFields = JsonSerializer.Deserialize<Issue>(fields);
            var issueKey = await CreateJiraIssue(fields);
            if (string.IsNullOrEmpty(issueKey))
                return StatusCode(500, "Failed to create JIRA issue.");
            var attachmentResult = await AttachFileToIssue(issueKey, file);
            if (!attachmentResult)
                return StatusCode(500, "Failed to attach file to JIRA issue.");
            return Ok(new { IssueKey = issueKey });
        }
        private async Task<string> CreateJiraIssue(Issue fields)
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration["JiraSettings:Email"] + ":" + _configuration["JiraSettings:ApiToken"]));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var issueRequest = new
            {
                fields = new
                {
                    project = new { key = fields.Key },
                    summary = fields.Summary,
                    description = fields.Description,
                    issuetype = new { name = fields.Issuetype },
                    priority = new { name = fields.Priority }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                _configuration["JiraSettings:BaseUrl"] + "/rest/api/2/issue",
                issueRequest
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content).RootElement.GetProperty("id").GetString();
        }
        private async Task<bool> AttachFileToIssue(string issueKey, IFormFile file)
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(_configuration["JiraSettings:Email"] + ":" + _configuration["JiraSettings:ApiToken"]));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            _httpClient.DefaultRequestHeaders.Add("X-Atlassian-Token", "no-check");

            using var multipartContent = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);

            var fileName = file.FileName;
            multipartContent.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync(
                _configuration["JiraSettings:BaseUrl"] + "/rest/api/2/issue/" + issueKey + "/attachments",
                multipartContent
            );

            return response.IsSuccessStatusCode;
        }

    }
}
