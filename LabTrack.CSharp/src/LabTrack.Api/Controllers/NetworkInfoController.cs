using LabTrack.Application.Interfaces;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using LabTrack.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
[AllowAnonymous]
public sealed class NetworkInfoController : ControllerBase
{
    private readonly ILogger<NetworkInfoController> _logger;

    public NetworkInfoController(ILogger<NetworkInfoController> logger)
    {
        _logger = logger;
    }

    [HttpGet("network")]
    public ActionResult<ApiResponse<object>> GetNetwork()
    {
        try
        {
            var ipAddresses = new List<string>();

            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (!IPAddress.IsLoopback(addr.Address))
                            {
                                ipAddresses.Add(addr.Address.ToString());
                            }
                        }
                    }
                }
            }
            catch { }

            var primaryIp = ipAddresses.FirstOrDefault() ?? "127.0.0.1";

            string? publicUrl = null;
            try
            {
                var pubUrlFile = System.IO.Path.Combine("C:\\labtrack", "public_url.txt");
                if (System.IO.File.Exists(pubUrlFile))
                {
                    var raw = System.IO.File.ReadAllText(pubUrlFile).Trim();
                    if (!string.IsNullOrWhiteSpace(raw) && (raw.StartsWith("http://") || raw.StartsWith("https://")))
                    {
                        publicUrl = raw;
                    }
                }
            }
            catch { }

            return Ok(ApiResponse<object>.Ok(new
            {
                hostname = Environment.MachineName,
                ipAddresses,
                primaryIp,
                publicUrl
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting network info");
            return StatusCode(500, ApiResponse<object>.Fail("NETWORK_INFO_ERROR", ex.Message));
        }
    }
}
