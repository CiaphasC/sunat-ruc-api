#define USE_TESSERACT
namespace SunatScraper.Core.Services;
using System.Security.Cryptography;
using Tesseract;
public class SunatSecurity
{
    private readonly HttpClient _http;
    public SunatSecurity(HttpClient h)=>_http=h;
    public string GenerateToken(int len=52){
        var sb=new System.Text.StringBuilder(len);
        using var rng=RandomNumberGenerator.Create();
        Span<byte> b=stackalloc byte[8];
        while(sb.Length<len){rng.GetBytes(b);sb.Append(Convert.ToString(BitConverter.ToUInt64(b),24));}
        return sb.ToString(0,len);
    }
    public async Task<string> SolveCaptchaAsync(){
        int rnd=Random.Shared.Next(1,9999);
        var png=await _http.GetByteArrayAsync($"/cl-ti-itmrconsruc/captcha?accion=image&nmagic={rnd}");
#if USE_TESSERACT
        try{
            using var eng=new TesseractEngine("/usr/share/tessdata","eng",EngineMode.Default);
            using var pix=Pix.LoadFromMemory(png);
            using var page=eng.Process(pix);
            var txt=page.GetText().Trim().Replace(" ","").ToUpper();
            if(txt.Length==4&&txt.All(char.IsLetterOrDigit))return txt;
        }catch{}
#endif
        var tmp=Path.GetTempFileName()+".png";
        await File.WriteAllBytesAsync(tmp,png);
        Console.Write($"Captcha manual ({tmp}): ");
        return Console.ReadLine()!.Trim().ToUpper();
    }
}
