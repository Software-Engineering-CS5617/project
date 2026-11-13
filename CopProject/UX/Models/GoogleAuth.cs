using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UX.Models;
public record GoogleUser(string Name, string Email, string Picture);
public static class GoogleAuth
{
    const string ClientId = "474271673658-9m47i1unkejgq0e9opirjeupk78ofn4o.apps.googleusercontent.com";
    const string ClientSecret = "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";
    private static readonly HttpClient s_http = new();
    public static async Task<GoogleUser> SignInAsync()
    {
        // Pick a free port on this computer. Google will send the user back to it.
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        string redirect = "http://127.0.0.1:" + port + "/";

        string verifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        string challenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        string code = await GetCodeFromBrowserAsync(redirect, challenge);
        string accessToken = await GetAccessTokenAsync(code, redirect, verifier);
        return await GetUserAsync(accessToken);
    }

    // Opens the Google login page and waits for Google to send the user back with a code
    static async Task<string> GetCodeFromBrowserAsync(string redirect, string challenge)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(redirect);
        listener.Start();
        try
        {
            string url = "https://accounts.google.com/o/oauth2/v2/auth"
                + "?client_id=" + Uri.EscapeDataString(ClientId)
                + "&redirect_uri=" + Uri.EscapeDataString(redirect)
                + "&response_type=code"
                + "&scope=" + Uri.EscapeDataString("openid email profile")
                + "&code_challenge=" + challenge
                + "&code_challenge_method=S256"
                + "&prompt=select_account";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            // give up after 2 minutes
            Task<HttpListenerContext> wait = listener.GetContextAsync();
            if (await Task.WhenAny(wait, Task.Delay(TimeSpan.FromMinutes(2))) != wait)
            {
                throw new Exception("Sign-in timed out. Please try again.");
            }

            HttpListenerContext context = wait.Result;
            string? code = context.Request.QueryString["code"];
            string? error = context.Request.QueryString["error"];

            // tell the user they can go back to the app
            byte[] page = Encoding.UTF8.GetBytes("<html><body style='font-family:sans-serif;text-align:center;margin-top:20vh'><h2>Signed in</h2><p>Returning you to SecureMeet...</p><script>window.close();</script></body></html>");
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.OutputStream.WriteAsync(page);
            context.Response.Close();

            if (code == null)
            {
                throw new Exception(error ?? "Google did not send a sign-in code.");
            }
            return code;
        }
        finally
        {
            listener.Close();
        }
    }
    static async Task<string> GetAccessTokenAsync(string code, string redirect, string verifier)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["redirect_uri"] = redirect,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier,
        });
        HttpResponseMessage reply = await s_http.PostAsync("https://oauth2.googleapis.com/token", form);
        using JsonDocument token = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());

        if (token.RootElement.TryGetProperty("access_token", out JsonElement accessToken))
        {
            return accessToken.GetString() ?? "";
        }
        throw new Exception("Google refused the sign-in. " + GetText(token.RootElement, "error_description", ""));
    }

    // Asks Google who this is (name, email, picture)
    static async Task<GoogleUser> GetUserAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage reply = await s_http.SendAsync(request);
        using JsonDocument info = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());

        string email = info.RootElement.GetProperty("email").GetString() ?? "";
        string name = GetText(info.RootElement, "name", email);
        string picture = GetText(info.RootElement, "picture", "");
        return new GoogleUser(name, email, picture);
    }
    static string GetText(JsonElement json, string key, string fallback)
    {
        return json.TryGetProperty(key, out JsonElement value) ? value.GetString() ?? fallback : fallback;
    }
}
