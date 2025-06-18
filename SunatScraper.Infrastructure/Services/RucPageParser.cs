/// <summary>
/// Analiza el HTML obtenido en las búsquedas por RUC.
/// </summary>
namespace SunatScraper.Infrastructure.Services;
using HtmlAgilityPack;
using SunatScraper.Domain.Models;
using System.Net;
using System.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

internal static class RucPageParser
{
    /// <summary>
    /// Normaliza un texto eliminando signos diacríticos y convirtiéndolo a minúsculas.
    /// </summary>
    static string Normalize(string s){
        var f=s.Normalize(NormalizationForm.FormD);
        Span<char> buf=stackalloc char[f.Length];
        int idx=0;
        foreach(var c in f)
            if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)
                buf[idx++]=char.ToLowerInvariant(c);
        return new string(buf[..idx]);
    }
    /// <summary>
    /// Busca un valor dentro del diccionario ignorando mayúsculas y acentos.
    /// </summary>
    static string? GetValue(IDictionary<string,string> map,string label)
    {
        var normLabel = Normalize(label);
        foreach (var (k, v) in map)
        {
            if (k.Contains(label, StringComparison.OrdinalIgnoreCase) ||
                Normalize(k).Contains(normLabel, StringComparison.OrdinalIgnoreCase))
                return v;
        }
        return null;
    }
    /// <summary>
    /// Extrae la información detallada de un RUC desde el HTML proporcionado.
    /// </summary>
    internal static RucInfo Parse(string html){
        var doc=new HtmlDocument();doc.LoadHtml(html);
        var map=new Dictionary<string,string>();
        foreach(var td in doc.DocumentNode.SelectNodes("//td[@class='bgn']") ?? Enumerable.Empty<HtmlNode>()){
            var key=td.InnerText.Trim();
            var val=td.NextSibling;
            while(val is {Name: not "td"}) val=val.NextSibling;
            map[key]=WebUtility.HtmlDecode(val?.InnerText.Trim()??"");
        }
        if(map.Count==0){
            foreach(var node in doc.DocumentNode.SelectNodes("//div[contains(@class,'list-group-item')]") ?? Enumerable.Empty<HtmlNode>()){
                var text=WebUtility.HtmlDecode(node.InnerText.Trim());
                int idx=text.IndexOf(':');
                if(idx>0){
                    var key=text[..idx].Trim();
                    var val=text[(idx+1)..].Trim();
                    map[key]=val;
                }
            }
        }
        var rucLine=GetValue(map,"RUC");
        string? ruc=null,razon=null;
        if(rucLine!=null)
        {
            var m=Regex.Match(rucLine,@"\d{11}");
            if(m.Success)
            {
                ruc=m.Value;
                var after=rucLine[(m.Index+m.Length)..].Trim();
                if(after.StartsWith("-")) after=after[1..].Trim();
                razon=after.Length>0?after:null;
            }
            else ruc=rucLine.Trim();
        }

        string? estado = GetValue(map,"Estado");
        string? condicion = GetValue(map,"Condición");
        string? direccion = GetValue(map,"Dirección") ?? GetValue(map,"Domicilio");
        string? ubic = GetValue(map,"Ubicación");
        razon=GetValue(map,"Razón") ?? GetValue(map,"Nombre") ?? razon;
        var docLine=GetValue(map,"Tipo de Documento");
        var contribLine=GetValue(map,"Tipo Contribuyente");
        string? documento=null,contribuyente=null;
        if(docLine!=null)
        {
            int dash=docLine.IndexOf('-');
            documento=(dash>0?docLine[..dash]:docLine).Trim();
        }
        if(contribLine!=null)
        {
            int dash=contribLine.IndexOf('-');
            contribuyente=(dash>0?contribLine[..dash]:contribLine).Trim();
        }

        // Uso de expresiones regulares como respaldo cuando faltan datos
        var plain = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
        if(string.IsNullOrWhiteSpace(ruc))
        {
            var m=Regex.Match(plain,@"\b(\d{11})\b");
            if(m.Success) ruc=m.Groups[1].Value;
        }
        if(string.IsNullOrWhiteSpace(razon) || razon=="-")
        {
            var m=Regex.Match(plain,@"\b\d{11}\s*-\s*([^\n]+)");
            if(m.Success) razon=m.Groups[1].Value.Trim();
        }
        estado ??= Regex.Match(plain,@"Estado\s*:\s*([^\n]+)",RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if(string.IsNullOrWhiteSpace(estado)) estado=null;
        condicion ??= Regex.Match(plain,@"Condici(?:\u00f3|o)n\s*:\s*([^\n]+)",RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if(string.IsNullOrWhiteSpace(condicion)) condicion=null;
        if(string.IsNullOrWhiteSpace(direccion))
        {
            var m=Regex.Match(plain,@"Direcci(?:\u00f3|o)n\s*:\s*([^\n]+)",RegexOptions.IgnoreCase);
            if(m.Success) direccion=m.Groups[1].Value.Trim();
        }
        ubic ??= Regex.Match(plain,@"Ubicaci(?:\u00f3|o)n\s*:\s*([^\n]+)",RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if(string.IsNullOrWhiteSpace(ubic)) ubic=null;
        if(documento==null)
        {
            var m=Regex.Match(plain,@"Tipo de Documento\s*:\s*([^\n]+)",RegexOptions.IgnoreCase);
            if(m.Success)
            {
                var d=m.Groups[1].Value.Trim();
                int dash=d.IndexOf('-');
                documento=(dash>0?d[..dash]:d).Trim();
            }
        }
        if(contribuyente==null)
        {
            var m=Regex.Match(plain,@"Tipo Contribuyente\s*:\s*([^\n]+)",RegexOptions.IgnoreCase);
            if(m.Success)
            {
                var d=m.Groups[1].Value.Trim();
                int dash=d.IndexOf('-');
                contribuyente=(dash>0?d[..dash]:d).Trim();
            }
        }

        return new RucInfo(
            ruc,
            razon,
            estado,
            condicion,
            direccion,
            ubic,
            documento,
            contribuyente);
    }

    /// <summary>
    /// Obtiene una colección de coincidencias desde la página de resultados de búsqueda.
    /// </summary>
    internal static IEnumerable<SearchResultItem> ParseList(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        // Procesamos cada enlace que representa un contribuyente en la lista
        foreach(var a in doc.DocumentNode.SelectNodes("//a[contains(@class,'aRucs')]") ?? Enumerable.Empty<HtmlNode>())
        {
            string? ruc=null, razon=null, ubic=null, estado=null;
            var rucNode=a.SelectSingleNode(".//h4[contains(text(),'RUC')]");
            if(rucNode!=null)
            {
                var m=System.Text.RegularExpressions.Regex.Match(rucNode.InnerText,@"\d{11}");
                if(m.Success) ruc=m.Value;
            }
            var h4s=a.SelectNodes(".//h4");
            if(h4s!=null && h4s.Count>1)
                razon=WebUtility.HtmlDecode(h4s[1].InnerText.Trim());
            var text=WebUtility.HtmlDecode(a.InnerText);
            var ub=a.SelectSingleNode(".//p[contains(text(),'Ubicación')]");
            if(ub!=null) ubic=ub.InnerText.Split(':',2).Last().Trim();
            else {
                var mU=System.Text.RegularExpressions.Regex.Match(text,@"Ubicaci(?:\u00f3|o)n\s*:\s*([^\n]+)",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if(mU.Success) ubic=mU.Groups[1].Value.Trim();
            }
            var st=a.SelectSingleNode(".//p[contains(text(),'Estado')]");
            if(st!=null) estado=st.InnerText.Split(':',2).Last().Trim();
            else {
                var mE=System.Text.RegularExpressions.Regex.Match(text,@"Estado\s*:\s*([^\n]+)",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if(mE.Success) estado=mE.Groups[1].Value.Trim();
            }
            yield return new SearchResultItem(ruc,razon,ubic,estado);
        }
    }
}
