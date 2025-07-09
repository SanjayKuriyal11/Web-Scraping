using System.Net.Http;
using HtmlAgilityPack;
using System.Text;
using System.IO;

class Program
{
    // Entry point: prompts for URLs, scrapes data, prints results, and offers CSV export
    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter a seed URL or multiple URLs separated by comma:");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("No URLs provided. Exiting...");
            return;
        }
        var urls = input.Split(',').Select(u => u.Trim()).ToList();

        var results = new List<Dictionary<string, string>>();

        foreach (var url in urls)
        {
            var info = await ScrapeCompanyData(url);
            PrintCompanyInfo(info);
            results.Add(info);
        }

        // Optionally export to CSV
        Console.WriteLine("\nExport results to CSV? (y/n):");
        var export = Console.ReadLine();
        if (export?.Trim().ToLower() == "y")
        {
            ExportToCsv(results, "scraped_companies.csv");
            Console.WriteLine("Results exported to scraped_companies.csv");
        }
    }

    // Scrapes company data from a given URL
    static async Task<Dictionary<string, string>> ScrapeCompanyData(string url)
    {
        var data = new Dictionary<string, string>();
        try
        {
            var client = new HttpClient();
            var html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
                
            // Extract company name (title)
            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
            data["Company Name"] = title ?? "N/A";
            data["Website"] = url;

            // Extract first email found
            var emailNode = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'mailto:')]")
                ?.FirstOrDefault();
            data["Email"] = emailNode?.Attributes["href"]?.Value?.Replace("mailto:", "") ?? "N/A";

            // Extract first phone found
            var phoneNode = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'tel:')]")
                ?.FirstOrDefault();
            data["Phone"] = phoneNode?.Attributes["href"]?.Value?.Replace("tel:", "") ?? "N/A";

            // Basic tech stack detection
            var bodyText = doc.DocumentNode.InnerText.ToLower();
            if (bodyText.Contains("angular") || bodyText.Contains(".net") || bodyText.Contains("react"))
                data["Tech Stack"] = "Modern web tech detected";
            else
                data["Tech Stack"] = "Unknown";
        }
        catch (Exception ex)
        {
            data["Error"] = ex.Message;
        }
        return data;
    }

    // Prints company info to the console
    static void PrintCompanyInfo(Dictionary<string, string> info)
    {
        Console.WriteLine("\n---- Company Info ----");
        foreach (var kvp in info)
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }

    // Exports results to a CSV file
    static void ExportToCsv(List<Dictionary<string, string>> results, string filePath)
    {
        if (results.Count == 0) return;
        var headers = results.SelectMany(d => d.Keys).Distinct().ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers));
        foreach (var dict in results)
        {
            var row = headers.Select(h => dict.ContainsKey(h) ? dict[h].Replace(",", " ") : "");
            sb.AppendLine(string.Join(",", row));
        }
        File.WriteAllText(filePath, sb.ToString());
    }
}