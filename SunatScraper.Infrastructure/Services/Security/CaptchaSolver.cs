#define USE_TESSERACT
/// <summary>
/// Encapsula la lógica de seguridad y resolución de captcha utilizada por SUNAT.
/// </summary>
namespace SunatScraper.Infrastructure.Services;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Tesseract;

public class CaptchaSolver : IDisposable
{
    private readonly HttpClient _httpClient;
#if USE_TESSERACT
    private readonly TesseractEngine? _engine;
    private readonly object _ocrLock = new();
#endif
    private bool _disposed;

    public int LastRandom { get; private set; }

    public CaptchaSolver(HttpClient httpClient)
    {
        _httpClient = httpClient;
#if USE_TESSERACT
        try
        {
            _engine = new TesseractEngine("/usr/share/tessdata", "eng", EngineMode.Default);
        }
        catch
        {
            _engine = null;
        }
#endif
    }

    /// <summary>
    /// Genera un token pseudoaleatorio utilizado por el portal de SUNAT.
    /// </summary>
    public string GenerateToken(int length = 52)
    {
        var sb = new StringBuilder(length);
        using var rng = RandomNumberGenerator.Create();
        Span<byte> buffer = stackalloc byte[8];
        const string Alphabet = "0123456789ABCDEFGHIJKLMN"; // 24 caracteres permitidos

        while (sb.Length < length)
        {
            rng.GetBytes(buffer);
            ulong value = BitConverter.ToUInt64(buffer);
            while (value > 0 && sb.Length < length)
            {
                sb.Append(Alphabet[(int)(value % 24)]);
                value /= 24;
            }
        }

        return sb.ToString(0, length);
    }

    /// <summary>
    /// Obtiene y resuelve el captcha; intenta Tesseract antes de recurrir a la entrada manual.
    /// </summary>
    public async Task<string> SolveCaptchaAsync()
    {
        int rnd = Random.Shared.Next(1, 9999);
        LastRandom = rnd;

        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"/cl-ti-itmrconsruc/captcha?accion=image&nmagic={rnd}");
        req.Headers.Referrer = new Uri(_httpClient.BaseAddress!,
            "cl-ti-itmrconsruc/FrameCriterioBusquedaWeb.jsp");
        req.Headers.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
        req.Headers.AcceptLanguage.ParseAdd("es-PE,es;q=0.9");

        using var res = await _httpClient.SendAsync(req);
        if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            Console.WriteLine($"[WARN] Captcha skipped: {(int)res.StatusCode} {res.ReasonPhrase}");
            return string.Empty;
        }

        if (!res.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Captcha request failed: {(int)res.StatusCode} {res.ReasonPhrase}");
        }

#if USE_TESSERACT
        var png = await res.Content.ReadAsByteArrayAsync();
        if (_engine != null)
        {
            try
            {
                lock (_ocrLock)
                {
                    using var pix = Pix.LoadFromMemory(png);
                    using var page = _engine.Process(pix);
                    var ocrText = page.GetText().Trim().Replace(" ", string.Empty).ToUpper();
                    if (ocrText.Length == 4 && ocrText.All(char.IsLetterOrDigit))
                        return ocrText;
                }
            }
            catch
            {
                // si falla el OCR se solicita el captcha manualmente
            }
        }
#else
        var png = await res.Content.ReadAsByteArrayAsync();
#endif
        var tmp = Path.GetTempFileName() + ".png";
        await File.WriteAllBytesAsync(tmp, png);
        Console.Write($"Captcha manual ({tmp}): ");
        var text = Console.ReadLine()!.Trim().ToUpper();
        try
        {
            File.Delete(tmp);
        }
        catch
        {
            // ignore failures when cleaning up temp file
        }
        return text;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
#if USE_TESSERACT
        _engine?.Dispose();
#endif
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
