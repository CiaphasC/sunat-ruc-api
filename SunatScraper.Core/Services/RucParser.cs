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
        var rucLine=GetValue(map,"RUC");
        string? ruc=null,razon=null;
        if(rucLine!=null){
            var m=System.Text.RegularExpressions.Regex.Match(rucLine,@"\d{11}");
            if(m.Success){
                ruc=m.Value;
                razon=rucLine[(m.Index+m.Length)..].TrimStart('-',' ').Trim();
            }else ruc=rucLine;
        }
        razon=GetValue(map,"Razón") ?? GetValue(map,"Nombre") ?? razon;
        return new RucInfo(
            ruc,
            razon,
            GetValue(map,"Estado"),
            GetValue(map,"Condición"),
            GetValue(map,"Dirección") ?? GetValue(map,"Domicilio"));
    }
}
