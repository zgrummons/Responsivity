using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NSwag.Annotations.AzureFunctionsV2;
using NSwag.Annotations;
using System.Net.Mail;

public class ResponsivityApi
{
    private readonly Config _config;
    private readonly IDictionary<string, App> _applications;
    // TODO: Mail service member variable declaration here
    private readonly MailAddress _fromAddress;
    private readonly List<MailAddress> _defaultAddress;

    public ResponsivityApi(Config config, IDictionary<string, App> applications /* TODO: Inject your mail service here */)
    {
        _config = config;
        _applications = applications;
        _defaultAddress = new List<MailAddress>(1) { new MailAddress(_config.DefaultMailName, _config.DefaultMailAddress) };
        _fromAddress = new MailAddress(_config.FromMailName, _config.FromMailAddress);
        // TODO: Mail service member variable assignment here
    }

    [SwaggerRequestBodyType(typeof(App), required: true, description: "JSON Application object")]
    [SwaggerResponse(200, typeof(string), Description = "Indicates that the request was accepted", IsNullable = true)]
    [OpenApiOperation("UpsertApplicationByBody")]
    [FunctionName("UpsertApplicationByBody")]
    public IActionResult UpsertTask([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Responsivity/Applications")] HttpRequest req, ILogger log)
        => ProcessApplication(JsonSerializer.Deserialize<App>(new StreamReader(req.Body).ReadToEnd()), log);

    [SwaggerResponse(200, typeof(string), Description = "Indicates that the request was accepted", IsNullable = true)]
    [OpenApiOperation("UpsertApplicationByPath")]
    [FunctionName("UpsertApplicationByPath")]
    public IActionResult UpsertTaskDefault([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Responsivity/Applications/{application}")] HttpRequest req, ILogger log, string application)
        => string.IsNullOrEmpty(application) ? new BadRequestObjectResult("Bad application name") : ProcessApplication(new App() { ApplicationName = application }, log);

    private IActionResult ProcessApplication(App app, ILogger log)
    {
        log.LogInformation($"Attempting to upsert application {app?.ApplicationName}");

        if (_applications.ContainsKey(app?.ApplicationName))
        {
            app = _applications[app.ApplicationName];
            app.CancellationTokenSource?.Cancel();
            app.CancellationTokenSource?.Dispose();
            app.DelayTask?.Dispose();
        }
        else
            _applications.Add(app?.ApplicationName, app);

        app.CancellationTokenSource = new CancellationTokenSource();
        app.DelayTask = Task.Delay(app.DelayMilliseconds == 0 ? TimeSpan.FromMinutes(15) : TimeSpan.FromMilliseconds(app.DelayMilliseconds), app.CancellationTokenSource.Token)
            .ContinueWith(async (x) =>
            {
                log.LogError($"Application {app.ApplicationName} failed to post within {(app.DelayMilliseconds == 0 ? "15 minutes" : $"{app.DelayMilliseconds} milliseconds")}");
                // TODO: Call your mail service to send mail here await _sendMailService.SendMail(ConstructEmail(app), log);
            }, app.CancellationTokenSource.Token);

        return new OkResult();
    }

    [SwaggerResponse(200, typeof(string), Description = "Indicates that the request was accepted", IsNullable = true)]
    [OpenApiOperation("ListApplications")]
    [FunctionName("ListApplications")]
    public IActionResult ListApplication([HttpTrigger(AuthorizationLevel.Function, "get", Route = "Responsivity/Applications")] HttpRequest req, ILogger log)
        => new OkObjectResult(JsonSerializer.Serialize(_applications, new JsonSerializerOptions() { MaxDepth = 0 }));

    [SwaggerResponse(200, typeof(string), Description = "Indicates that the request was accepted", IsNullable = true)]
    [OpenApiOperation("ListApplication")]
    [FunctionName("ListApplication")]
    public IActionResult ListApplications([HttpTrigger(AuthorizationLevel.Function, "get", Route = "Responsivity/Applications/{application}")] HttpRequest req, ILogger log, string application)
        => new OkObjectResult(JsonSerializer.Serialize(_applications.ContainsKey(application) ? _applications[application] : null, new JsonSerializerOptions() { MaxDepth = 0 }));

    [SwaggerResponse(200, typeof(string), Description = "Indicates that the request was accepted", IsNullable = true)]
    [OpenApiOperation("CancelApplicationByPath")]
    [FunctionName("CancelApplicationByPath")]
    public IActionResult CancelApplicationByPath([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Responsivity/Applications/{application}")] HttpRequest req, ILogger log, string application)
        => CancelApplication(new App() { ApplicationName = application }, log);

    [SwaggerResponse(200, typeof(string), Description = "Indicates that the request was accepted", IsNullable = true)]
    [OpenApiOperation("CancelApplicationByBody")]
    [FunctionName("CancelApplicationByBody")]
    [SwaggerRequestBodyType(typeof(App), true, "Application", "The Application to cancel.")]
    public IActionResult CancelApplicationByBody([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Responsivity/Applications")] HttpRequest req, ILogger log)
        => CancelApplication(JsonSerializer.Deserialize<App>(new StreamReader(req.Body).ReadToEnd()), log);

    private IActionResult CancelApplication(App app, ILogger log)
    {
        log.LogInformation($"Attempting to cancel application {app?.ApplicationName}");

        if (_applications.ContainsKey(app?.ApplicationName))
        {
            app = _applications[app.ApplicationName];
            app.CancellationTokenSource?.Cancel();
            app.CancellationTokenSource?.Dispose();
            app.DelayTask?.Dispose();
            _applications.Remove(app.ApplicationName);
            return new OkResult();
        }

        log.LogError($"Cannot find application {app?.ApplicationName}");
        return new BadRequestObjectResult($"Cannot find application {app?.ApplicationName}");
    }

    private MessageBase ConstructEmail(App app) => new MailMessage
    {
        From = _fromAddress,
        To = app?.MailAddresses == null || app.MailAddresses.Count() == 0 ? _defaultAddress : GetMailAddresses(app.MailAddresses),
        Subject = $"[{_config.Environment}] {app.ApplicationName} Application Timeout",
        Body = $"Hello, the application {app.ApplicationName} failed to post within {(app.DelayMilliseconds == 0 ? "15 minutes" : $"{app.DelayMilliseconds} milliseconds")}.\r\nYou should probably check on it... or don't, I'm not your father."
    };

    private List<MailAddress> GetMailAddresses(Dictionary<string, string> addresses)
    {
        var mailAddresses = new List<MailAddress>();
        foreach (var address in addresses)
            mailAddresses.Add(new MailAddress(address.Value, address.Key));
        return mailAddresses;
    }

    [OpenApiIgnore]
    [FunctionName("ResponsivitySwagger")]
    public async Task<IActionResult> Swagger([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Responsivity/Swagger")] HttpRequest req, ILogger log)
       => await GenerateOpenApi(log, req.Host, GetType(), "Responsivity API", "Responsivity");
}