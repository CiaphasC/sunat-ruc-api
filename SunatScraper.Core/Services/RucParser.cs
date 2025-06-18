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
        if(rucLine!=null){
            var m=System.Text.RegularExpressions.Regex.Match(rucLine,@"\d{11}");
            if(m.Success){
                ruc=m.Value;
                razon=rucLine[(m.Index+m.Length)..].TrimStart('-',' ').Trim();
            }else ruc=rucLine;
        }
        razon=GetValue(map,"Razón") ?? GetValue(map,"Nombre") ?? razon;
        var docLine=GetValue(map,"Tipo de Documento");
        string? documento=null;
        if(docLine!=null)
        {
            int dash=docLine.IndexOf('-');
            documento=(dash>0?docLine[..dash]:docLine).Trim();
        }
        return new RucInfo(
            ruc,
            razon,
            GetValue(map,"Estado"),
            GetValue(map,"Condición"),
            GetValue(map,"Dirección") ?? GetValue(map,"Domicilio"),
            GetValue(map,"Ubicación"),
            documento);
    }

    public static IEnumerable<SearchResultItem> ParseList(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
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
