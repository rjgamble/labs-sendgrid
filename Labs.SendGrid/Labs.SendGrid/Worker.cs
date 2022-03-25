using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using Newtonsoft.Json;
using static Labs.SendGrid.SendGridApiHelper;

namespace Labs.SendGrid;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISendGridClient _client;
    private readonly IConfiguration _config;

    public Worker(ILogger<Worker> logger, ISendGridClient client, IConfiguration config)
    {
        _logger = logger;
        _client = client;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Labs.SendGrid started");
        _logger.LogDebug("SendGrid API version: {@version}", _client.Version);

        var allTemplates = await GetAllTemplates(stoppingToken);

        var templateToSend = GetTemplateToSend();
        var templateId = await GetOrAddTemplateId(templateToSend, allTemplates, stoppingToken);

        var message = new SendGridMessage
        {
            From = Recipients.From,
            TemplateId = templateId,
        };

        foreach(var recipient in Recipients.Tos)
        {
            message.AddTos(recipient.Tos, personalization: recipient);
        }

        var isSandboxModeEnabled = _config.GetValue("SendGrid:SandboxModeEnabled", true);
        _logger.LogDebug("Sandbox mode enabled: {@sandbox}", isSandboxModeEnabled);

        if (isSandboxModeEnabled)
        {
            message.MailSettings = new MailSettings
            {
                SandboxMode = new SandboxMode
                {
                    Enable = true
                }
            };
        }

        var serialisedMessage = message.Serialize();
        
        _logger.LogDebug("Ready to send message (length, {@length}):", serialisedMessage.Length);
        _logger.LogDebug("{@message}", serialisedMessage);

        var response = await _client.SendEmailAsync(message, stoppingToken);

        await LogResponse(response, stoppingToken);
    }

    private Template GetTemplateToSend()
    {
        var configTemplate = _config.GetValue("Template", "Simple");

        return configTemplate.ToLowerInvariant() switch
        {
            "invoice" => Templates.Invoice,
            _ => Templates.Simple
        };
    }

    private async Task LogResponse(Response response, CancellationToken stoppingToken)
    {
        _logger.LogDebug("Response code: {@code}", response.StatusCode);
        _logger.LogDebug("Response headers:");
        _logger.LogDebug("{@headers}", response.DeserializeResponseHeaders(response.Headers));
        _logger.LogDebug("Response content:");
        _logger.LogDebug(" {@body}", await response.Body.ReadAsStringAsync(stoppingToken));
    }

    private async Task<List<SendGridTemplate>> GetAllTemplates(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Searching for existing templates");

        var queryParams = new { generations = "legacy,dynamic", page_size = 10 };

        var response = await _client.RequestAsync(
            method: BaseClient.Method.GET, 
            urlPath: "templates", 
            queryParams: JsonConvert.SerializeObject(queryParams), 
            cancellationToken: stoppingToken);

        if (!response.IsSuccessStatusCode)
        {
            await LogResponse(response, stoppingToken);
            throw new Exception("Failed to fetch templates");
        }

        var sendGridTemplates = JsonConvert.DeserializeObject<ListResult<SendGridTemplate>>(await response.Body.ReadAsStringAsync(stoppingToken));

        _logger.LogInformation("Found {@count} templates", sendGridTemplates.Metadata.Count);
        _logger.LogDebug("{@templates}", sendGridTemplates.Result);

        return sendGridTemplates.Result;
    }

    private async Task<string> GetOrAddTemplateId(Template template, List<SendGridTemplate> allTemplates, CancellationToken stoppingToken)
    {
        if(allTemplates.Any(t => t.Name == template.Name))
        {
            _logger.LogInformation("SendGrid Template already exists for {@templateName}", template.Name);
            return allTemplates.First(t => t.Name == template.Name).Id;
        }

        var payload = new { name = template.Name, generation = "dynamic" };
        var payloadJson = JsonConvert.SerializeObject(payload);

        _logger.LogDebug("Creating new template with name {@name}", template.Name);
        _logger.LogDebug("{@payload}", payloadJson);

        var response = await _client.RequestAsync(
            method: BaseClient.Method.POST,
            urlPath: "templates",
            requestBody: JsonConvert.SerializeObject(payload),
            cancellationToken: stoppingToken);

        if (!response.IsSuccessStatusCode)
        {
            await LogResponse(response, stoppingToken);
            throw new Exception($"Failed to create template with name {template.Name}");
        }

        var sendGridTemplate = JsonConvert.DeserializeObject<SendGridTemplate>(await response.Body.ReadAsStringAsync(stoppingToken));

        _logger.LogInformation("Created Transactional Template for {@name} with id {@id}", sendGridTemplate.Name, sendGridTemplate.Id);

        await AddTemplateVersion(sendGridTemplate.Id, template, stoppingToken);

        return sendGridTemplate.Id;
    }

    private async Task AddTemplateVersion(string templateId, Template template, CancellationToken stoppingToken)
    {
        var payload = new SendGridTemplateVersion(
            templateId, 
            $"{template.Name} v1", 
            template.Subject, 
            template.Content, 
            TestData: JsonConvert.SerializeObject(template.SampleData));
        var payloadJson = JsonConvert.SerializeObject(payload);

        _logger.LogDebug("Creating new template version for Transactional Template with id {@id}", templateId);
        _logger.LogDebug("{@payload}", payloadJson);

        var response = await _client.RequestAsync(
            method: BaseClient.Method.POST,
            urlPath: $"templates/{templateId}/versions",
            requestBody: payloadJson,
            cancellationToken: stoppingToken);

        if (!response.IsSuccessStatusCode)
        {
            await LogResponse(response, stoppingToken);
            throw new Exception($"Failed to create template version for template with id {templateId}");
        }

        var sendGridTemplateVersion = JsonConvert.DeserializeObject<SendGridTemplateVersion>(await response.Body.ReadAsStringAsync(stoppingToken));

        _logger.LogInformation("Created template version with id {@id}", sendGridTemplateVersion.Id);
        _logger.LogDebug("{@version}", sendGridTemplateVersion);
    }
}
