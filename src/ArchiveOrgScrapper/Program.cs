using HtmlAgilityPack;
using System.Text;

var urls = GetArguments();

var partial = new List<(string, string)>();

foreach (var url in urls)
{
    var document = await GetDocument(url);

    ReadHtml(document, url, partial);
}

var hashtable = GetHashtable(partial);

await CreateCsv(hashtable);

string[] GetArguments()
{
    string urls = Environment.GetCommandLineArgs()[1];

    return urls.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
}

async Task<HtmlDocument> GetDocument(string url)
{
    var client = new HttpClient();

    var response = await client.GetAsync(url);

    var body = await response.Content.ReadAsStringAsync();

    var document = new HtmlDocument();
    document.LoadHtml(body);

    return document;
}

void ReadHtml(HtmlDocument html, string url, List<(string, string)> partial)
{
    var rows = html.DocumentNode.SelectNodes("//table[@class='directory-listing-table']/tbody/tr");

    var elements = new List<HtmlNode>();

    foreach (var row in rows)
    {
        foreach (var element in row.Descendants())
        {
            if (element.OuterHtml.Contains("<a href"))
            {
                elements.Add(element);
            }
        }
    }

    foreach (var element in elements)
    {
        var a = element.SelectSingleNode("a");

        if (a != null)
        {
            var href = a.Attributes["href"];

            if (href != null && !a.InnerHtml.Contains("Go to parent directory"))
            {
                Console.WriteLine("Read: " + a.InnerHtml);

                partial.Add((a.InnerHtml, url + @"/" + href.Value));
            }
        }
    }
}

Dictionary<string, string> GetHashtable(List<(string, string)> partial)
{
    var hashtable = new Dictionary<string, string>();

    foreach (var item in partial)
    {
        if (!hashtable.ContainsKey(item.Item1))
        {
            Console.WriteLine("Getting: " + item.Item1);

            hashtable.Add(item.Item1, item.Item2);
        }
    }

    return hashtable;
}

async Task CreateCsv(Dictionary<string, string> hashtable)
{
    var csv = new StringBuilder();

    foreach (var item in hashtable)
    {
        var newLine = string.Format("{0}#:#:#{1}", item.Key, item.Value);

        csv.AppendLine(newLine);
    }

    await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + @"\games.csv", csv.ToString());
}