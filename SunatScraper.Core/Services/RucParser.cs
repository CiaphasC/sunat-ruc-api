namespace SunatScraper.Core.Services;
using HtmlAgilityPack;
using SunatScraper.Core.Models;
using System.Net;
using System.Linq;
using System;
public static class RucParser
{
    static string? GetValue(IDictionary<string,string> map,string label){
        foreach(var (k,v) in map)
            if(k.Contains(label,StringComparison.OrdinalIgnoreCase))
                return v;
        return null;
    }
    public static RucInfo Parse(string html){
        var doc=new HtmlDocument();doc.LoadHtml(html);
        var map=new Dictionary<string,string>();
        foreach(var td in doc.DocumentNode.SelectNodes("//td[@class='bgn']") ?? Enumerable.Empty<HtmlNode>()){
            var key=td.InnerText.Trim();
            var val=td.NextSibling;
            while(val is {Name: not "td"}) val=val.NextSibling;
            map[key]=WebUtility.HtmlDecode(val?.InnerText.Trim()??"");
        }
        if(map.Count==0){
            foreach(var node in doc.DocumentNode.SelectNodes("//*[contains(@class,'list-group-item')]") ?? Enumerable.Empty<HtmlNode>()){
                var text=WebUtility.HtmlDecode(node.InnerText.Trim());
                int idx=text.IndexOf(':');
                if(idx>0){
                    var key=text[..idx].Trim();
                    var val=text[(idx+1)..].Trim();
                    map[key]=val;
                }
            }
        }
        string? ruc = GetValue(map, "RUC");
        string? razon = GetValue(map, "Razón") ?? GetValue(map, "Nombre");
        if(ruc is not null){
            int p = ruc.IndexOf('-');
            if(p>0){
                var maybe = ruc[..p].Trim();
                if(Validation.InputGuards.IsValidRuc(maybe)){
                    razon ??= ruc[(p+1)..].Trim();
                    ruc = maybe;
                }
            }
        }
        return new RucInfo(
            ruc,
            razon,
            GetValue(map, "Estado"),
            GetValue(map, "Condición"),
            GetValue(map, "Dirección") ?? GetValue(map, "Domicilio"));
    }

    public static IEnumerable<(string Ruc,string Nombre)> ParseListado(string html){
        var doc=new HtmlDocument();doc.LoadHtml(html);
        foreach(var a in doc.DocumentNode.SelectNodes("//a[contains(@class,'aRucs')]") ?? Enumerable.Empty<HtmlNode>()){
            var ruc=a.GetAttributeValue("data-ruc",string.Empty);
            if(string.IsNullOrWhiteSpace(ruc)){
                var m=System.Text.RegularExpressions.Regex.Match(a.InnerText,@"RUC:\s*(\d{11})");
                if(m.Success) ruc=m.Groups[1].Value;
            }
            var nombre=a.SelectNodes(".//h4").Skip(1).FirstOrDefault()?.InnerText.Trim() ?? string.Empty;
            if(!string.IsNullOrWhiteSpace(ruc))
                yield return (ruc,WebUtility.HtmlDecode(nombre));
        }
    }
}
