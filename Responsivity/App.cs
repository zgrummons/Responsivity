using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public class App
{
    public string ApplicationName { get; set; }
    public int DelayMilliseconds { get; set; }
    public Dictionary<string, string> MailAddresses { get; set; }

    [JsonIgnore]
    public CancellationTokenSource CancellationTokenSource { get; set; }
    [JsonIgnore]
    public Task DelayTask { get; set; }
}