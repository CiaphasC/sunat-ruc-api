namespace SunatScraper.Core.Services;
using HtmlAgilityPack;
using SunatScraper.Core.Models;
using System.Net;
using System.Linq;
public static class RucParser
{
    public static RucInfo Parse(string html){
        var doc=new HtmlDocument();doc.LoadHtml(html);
        var map=new Dictionary<string,string>();
        foreach(var td in doc.DocumentNode.SelectNodes("//td[@class='bgn']") ?? Enumerable.Empty<HtmlNode>()){
            var key=td.InnerText.Trim();
            var val=td.NextSibling;
            while(val is {Name: not "td"}) val=val.NextSibling;
            map[key]=WebUtility.HtmlDecode(val?.InnerText.Trim()??"");
        }
        return new RucInfo(
            map.GetValueOrDefault("Número de RUC"),
            map.GetValueOrDefault("Nombre o Razón Social")??map.GetValueOrDefault("Nombre Comercial"),
            map.GetValueOrDefault("Estado del Contribuyente"),
            map.GetValueOrDefault("Condición del Contribuyente"),
            map.GetValueOrDefault("Dirección del Domicilio Fiscal"));
    }
}
