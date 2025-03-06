using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Lacuna.Pki;
using Lacuna.Pki.Pades;
using System.Runtime.ConstrainedExecution;
using RestSharp;
using Lacuna.SignerService.Models;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using HttpTracer;
using HttpTracer.Logger;
using RestSharp.Serializers.Json;
using System.Threading;
using Serilog.Core;


namespace Lacuna.SignerService;

public class DirectoryWatcher : BackgroundService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<DirectoryWatcher> logger;
    private readonly DocumentService documentService;
    private readonly RestClient restClient;
    private string userId = string.Empty;
    private string sdkLicenseHash = string.Empty;

    public DirectoryWatcher(IConfiguration configuration, ILogger<DirectoryWatcher> logger, DocumentService documentService)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.documentService = documentService;

        var options = new RestClientOptions("https://billing-api.lacunasoftware.com/")
        {
            ThrowOnAnyError = false,
            MaxTimeout = 60000,
            //			ConfigureMessageHandler = handler => new HttpTracerHandler(handler, new ConsoleLogger(), HttpMessageParts.All)
        };
        restClient = new RestClient(options)
            .AddDefaultHeader("Content-Type", "application/json")
            .AddDefaultHeader("Accept", "application/json")
            .AddDefaultHeader("X-API-KEY", configuration["apiKey"] ?? string.Empty);
        restClient.UseSystemTextJson(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });


    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await initAsync();
        using var watcher = new FileSystemWatcher(configuration["RootPathInput"] ?? string.Empty);
        watcher.NotifyFilter = NotifyFilters.Attributes
                                      | NotifyFilters.CreationTime
                                      | NotifyFilters.DirectoryName
                                      | NotifyFilters.FileName
                                      | NotifyFilters.LastAccess
                                      | NotifyFilters.LastWrite
                                      | NotifyFilters.Security
                                      | NotifyFilters.Size;

        watcher.Created += onChanged;
        watcher.Filter = "*.pdf";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;


        try
        {
            DocumentService.MoveFiles(configuration["RootPathTemp"]!, configuration["RootPathError"]!);
            var files = Directory
                .EnumerateFiles(configuration["RootPathInput"] ?? string.Empty, "*.*", SearchOption.AllDirectories)
                .ToList();
            if (files.Any())
            {
                logger.LogInformation("Found : {n} files in {RootPathInput}", files.Count(), configuration["RootPathInput"]);
            }

            foreach (var file in files)
            {
                try
                {
                    documentService.Enqueue(file);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "File {file} error", file);
                }
            }

            logger.LogInformation("Directory Watcher Started. Listening directory {RootPathInput} ", configuration["RootPathInput"]);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                while (documentService.TryNext(out var document))
                {
                    Debug.Assert(document != null, nameof(document) + " != null");
                    await signAsync(document, stoppingToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("Task Canceled");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
            Environment.Exit(1);
        }
    }

    private async Task<bool> signAsync(DocumentModel document, CancellationToken cancellationToken)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            File.Move(document.FileName, document.TempFileName);
            var padesSigner = new PadesSigner();
            padesSigner.SetPdfToSign(document.TempFileName);
            var policy = new PadesPolicySpec();
            if ((configuration["TimeStampActive"]?.ToLowerInvariant() == "true"))
            {
                policy = getSignaturePolicyTimeStamp().GetPolicy(document.Certificate.Certificate);
                padesSigner.SetTimestampRequester(Util.GetTimestampRequester(configuration));
            }
            else
            {
                policy = getSignaturePolicy().GetPolicy(document.Certificate.Certificate);
            }
            padesSigner.SetPolicy(policy);
            padesSigner.SetSigningCertificate(document.Certificate);
            if (configuration.GetSection("PadesVisualRepresentation").Exists())
            {
                padesSigner.SetVisualRepresentation(Util.GetVisualRepresentation(document.Certificate.Certificate, configuration, logger));
            }
            padesSigner.ComputeSignature();
            var signatureContent = padesSigner.GetPadesSignature();
            await File.WriteAllBytesAsync(document.SignedFileName, signatureContent, cancellationToken);
            File.Delete(document.TempFileName);
            logger.LogInformation("file {file} signed in {timespan} s", document.FileName, sw.Elapsed.TotalSeconds.ToString("N1"));
            RestRequest request = new RestRequest("api/SdkPaayo")
                .AddJsonBody(new SdkPaYGModel()
                {
                    Success = true,
                    UserId = this.userId,
                    TypeCode = "PADES",
                    SdkHash = this.sdkLicenseHash,
                    Details = document.Certificate.Certificate.SubjectDisplayName
                });
            var response = await restClient.PostAsync<SdkPaYGReturnModel>(request, cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error on signing document: {Document} message: {ErrorMessage} ", document.FileName, e.Message);
            documentService.MoveFileToError(document, $"Error on signing document: {document.FileName} message:{e.Message} ");
        }
        return false;
    }

    private IPadesPolicyMapper getSignaturePolicyTimeStamp()
    {
        // return PadesPoliciesForGeneration.GetPadesBasic(GetTrustArbitrator());
        return PadesPoliciesForGeneration.GetPadesT(GetTrustArbitrator());
    }

    private IPadesPolicyMapper getSignaturePolicy()
    {
        // return PadesPoliciesForGeneration.GetPadesBasic(GetTrustArbitrator());
        return PadesPoliciesForGeneration.GetPadesBasic(GetTrustArbitrator());
    }

    public ITrustArbitrator GetTrustArbitrator()
    {
        // We start by trusting the ICP-Brasil roots and the roots registered as trusted on the host
        // Windows Server.
        var trustArbitrator = new LinkedTrustArbitrator(TrustArbitrators.PkiBrazil, TrustArbitrators.Windows);
        if (configuration["AcceptLacunaTestCertificates"]?.ToLowerInvariant() == "true")
        {
            var lacunaRoot = PKCertificate.Decode(Convert.FromBase64String(
                "MIIFnjCCA4agAwIBAgIBATANBgkqhkiG9w0BAQ0FADBfMQswCQYDVQQGEwJCUjETMBEGA1UEChMKSUNQLUJyYXNpbDEdMBsGA1UECxMUTGFjdW5hIFNvZnR3YXJlIC0gTFMxHDAaBgNVBAMTE0xhY3VuYSBSb290IFRlc3QgdjMwIBcNMjQxMjE2MjA0MjEwWhgPMjA3NDEyMTYwMzAwMDBaMF8xCzAJBgNVBAYTAkJSMRMwEQYDVQQKEwpJQ1AtQnJhc2lsMR0wGwYDVQQLExRMYWN1bmEgU29mdHdhcmUgLSBMUzEcMBoGA1UEAxMTTGFjdW5hIFJvb3QgVGVzdCB2MzCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAN2j+WJCzBe0/PCCXClKQy89kfdFmSyH3wYDsMyUSIBFroeRhv5DEUWMIJcq0fx8bwXIfHv7FQEjPO91GyS3ke2f9uvT0qnAvB2hlwCqF5/eU+LuJYtgxc7vW3QCiUNsYkiHysnIvOB46YwmwOMcpi01eQjrkgsYBzBa4TYZymhD28oGT9VEXepVpzcxF/H1zAnBHrpQRTPz5AjsJ7x3IKOLWFScTCtTnpp4HZslBvnUIwiTQNu4HgsUOekRfDbVwdftcwFHFmW5Z3w6zJvJ/b1x7iv7+g2lskWBNzEV769KoOT6+uwr1zk6Zwv/Oeze22GXuWrKKqLVanvgSCMPSFFlwGYOyjfpXC6Ccwe7Ptnb3cfhvX4V3BtXxkcBvfT34jhT6eoqT6RPqCtA18YTF9qLXHxnQ/AmqJdgsu0+JBWGIQWj95/qv7Y2Q47/7Q6866Bp6SbWDWLd161l4IYe/R9DE8gsujGey9gydiNrtzxNW3BrtssmjnYs9no3X/tRvXLCA7A0jebjqQBg91CamveU0Ou5Cz7uzR5OUsGNuyIFLZSXc2v+WGhjGAEyNSO9Mqwl+Lm8huyzmiwtoyIfMWsKTHbp4iQwqAWkQYcmMTXip/+lJOVi3yQSNOs5o7GyWsPtKZRglUs2cVnhe6dk0Ke/y8j+1MevmBnvZ1SQQoOpAgMBAAGjYzBhMA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFHKswdeMDNW5PgtBsXnjJLEdoEotMB8GA1UdIwQYMBaAFHKswdeMDNW5PgtBsXnjJLEdoEotMA4GA1UdDwEB/wQEAwIBBjANBgkqhkiG9w0BAQ0FAAOCAgEAhjObhPicGpi1iXbixX4yNIQj39XWnbY4f1j/SsSkQFBqnFF6vfleyaf4YridawGJcrXv1iX/KOeFLs02+f/rTH+wDmWs8a0mImyqOVW/Vn4gs/oSHu2ynTr/l9exvV0CLAqS7GPSj1CZ+j5gaFsq/NQxGImAHr/zr5E/XIgkwyF9PwGBAnO7QRj6n5au3swm0S+KqNSdaRtQkq61yZKdHxGS6K2bQAx88gWqWmiQ+P6XqKBn87+zTe5PsAqwvyM1sd9/oa0IF58o6g48dnxnpdMLs9KbB3/Rzh3n33JoQkjg2ZXyXk8fhefdAr18YKrmL9aX9q2GK1bnSGUPJkzbMbLIKpkAyboXG/zdbPDg3B7keZGdCgXrrBl1zoP3klieqclQJ0gWMVx5IbJfo/MmtgxbVNtd/CezXAESaCaApBQVC0U9GUVvekN6OrBYhkwA+HVbmF1fRznL05gV091Uc2iYOV+hiNrAJHvXuPxPVgqd2Mrx+9xc8VGOT6jGWkGAOEHbW1uVeZNcWTsHg9eQbRwiSUouR7zrMy+be/xMDloYJLE/94VuoGS8/Z2FS95HZ183J82Ihf2F2zsK485cmY4Unipq98yiHWtRWbaihQe1Dzfp1Av1U9gAMbOL0ounKC0dxstpNqfzC+z9nBwQpAviqUzghQIom9Q6aDk4gZ8="));
            trustArbitrator.Add(new TrustedRoots(lacunaRoot));
        }
        return trustArbitrator;
    }

    private void onChanged(object sender, FileSystemEventArgs e)
    {
        logger.LogInformation("file {file} {ChangeType}", e.FullPath, e.ChangeType);
        documentService.Enqueue(e.FullPath);
    }

    public async Task initAsync()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
            logger.LogInformation($"Lacuna Signer Service version: {version}");
            var sdkLicense = string.Empty;
            if (string.IsNullOrEmpty(configuration["apiKey"]))
            {
                logger.LogError("apiKey is null or empty");
                Environment.Exit(1);
            }
            if (string.IsNullOrEmpty(configuration["userId"]))
            {
                logger.LogError("userId is null or empty");
                Environment.Exit(1);
            }
            if (!string.IsNullOrEmpty(configuration["PkiSDKLicense"]))
            {
                sdkLicense = configuration["PkiSDKLicense"] ?? string.Empty;
                PkiConfig.BinaryLicense = Convert.FromBase64String(sdkLicense);
                PkiConfig.LoadLicense(Convert.FromBase64String(sdkLicense));
                var exp = Lacuna.Pki.Util.PkiInfo.License.Expiration;
                logger.LogInformation("SDK License Expiration: " + exp.ToString());
            }
            else
            {
                var request = new RestRequest($"api/SdkPaayo/{configuration["userId"]}");
                var license = await restClient.ExecuteGetAsync<SdkPaayo>(request);
                if (license.Data == null)
                {
                    logger.LogError("Service could not get SDK license to {userId}. Error {Error}", configuration["userId"], license.ErrorException?.ToString());
                    Environment.Exit(1);
                }
                sdkLicense = license.Data.SdkLicense;
                if (string.IsNullOrEmpty(sdkLicense))
                {
                    logger.LogError("Service could not get SDK license to {userId}, error {error}.", configuration["userId"], license.Data.ErrorMessage);
                    Environment.Exit(1);

                }
                PkiConfig.BinaryLicense = Convert.FromBase64String(license.Data.SdkLicense);
                PkiConfig.LoadLicense(Convert.FromBase64String(sdkLicense));
                var exp = Lacuna.Pki.Util.PkiInfo.License.Expiration;
                logger.LogInformation("SDK License Expiration: " + exp.ToString());
            }

            userId = configuration["userId"] ?? string.Empty;
            sdkLicenseHash = sdkLicense.Sha256();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error on obtain Pki SDK License!");
            Environment.Exit(1);
        }
        try
        {
            documentService.LazyInitializer();
            if (string.IsNullOrEmpty(configuration["RootPathTemp"]))
            {
                logger.LogError("RootPathTemp is null or empty");
                Environment.Exit(1);
            }
            if (string.IsNullOrEmpty(configuration["RootPathError"]))
            {
                logger.LogError("RootPathError is null or empty");
                Environment.Exit(1);
            }
            if (string.IsNullOrEmpty(configuration["RootPathInput"]))
            {
                logger.LogError("RootPathInput is null or empty");
                Environment.Exit(1);
            }
            if (string.IsNullOrEmpty(configuration["RootPathSigned"]))
            {
                logger.LogError("RootPathSigned is null or empty");
                Environment.Exit(1);
            }
            if (!Directory.Exists(configuration["RootPathTemp"]))
            {
                Directory.CreateDirectory(configuration["RootPathTemp"]!);
                logger.LogInformation("Temp Directory {RootPathTemp} created", configuration["RootPathTemp"]);
            }
            if (!Directory.Exists(configuration["RootPathError"]))
            {
                Directory.CreateDirectory(configuration["RootPathError"]!);
                logger.LogInformation("Error Directory {RootPathError} created", configuration["RootPathError"]);
            }
            if (!Directory.Exists(configuration["RootPathInput"]))
            {
                Directory.CreateDirectory(configuration["RootPathInput"]!);
                logger.LogInformation("Input Directory {RootPathInput} created", configuration["RootPathInput"]);
            }
            if (!Directory.Exists(configuration["RootPathSigned"]))
            {
                Directory.CreateDirectory(configuration["RootPathSigned"]!);
                logger.LogInformation("Signed Directory {RootPathLogs} created", configuration["RootPathSigned"]);
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error on directory create!");
            Environment.Exit(1);
        }
    }
}
