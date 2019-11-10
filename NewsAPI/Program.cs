using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;

namespace NewsAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpClient client = new HttpClient();
            var articleMap = new Dictionary<string, dynamic>();

            string html = string.Empty;
            string url = Properties.Settings.Default.NewsAPI; //add property with your NewsAPI - https://newsapi.org/docs
            int sleep = 15; //adjust to your preference

            while (true)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.AutomaticDecompression = DecompressionMethods.GZip;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        html = reader.ReadToEnd();
                    }

                    dynamic ignObject = JsonConvert.DeserializeObject(html);
                    foreach (dynamic article in ignObject.articles)
                    {
                        String title = article.title;
                        if (!articleMap.ContainsKey(title))
                        {
                            articleMap.Add(title, article);
                            String textMessage = "*" + title + "*\n" + article.description + "\n" + article.url;
                            textMessage = textMessage.Replace("'", "`");
                            Console.WriteLine(article.publishedAt);
                            Console.WriteLine(article.title);
                            var content = new StringContent("{'text':'" + textMessage + "'}");
                            var response = client.PostAsync(Properties.Settings.Default.SlackWebhookURL, content); //add property with your Slack Webhook - https://api.slack.com/messaging/webhooks
                            var responseString = response.Result.Content.ReadAsStringAsync();
                            Console.WriteLine(responseString.Result.ToString());
                            Console.WriteLine("\n\n");
                        }
                    }

                    var removeMap = new Dictionary<string, dynamic>();
                    foreach (KeyValuePair<string, dynamic> article in articleMap)
                    {
                        CultureInfo provider = CultureInfo.InvariantCulture;
                        String publishedAtString = article.Value.publishedAt;
                        DateTime publishedAt = DateTime.ParseExact(publishedAtString, "MM/dd/yyyy HH:mm:ss", provider);
                        if ((DateTime.Now - publishedAt).TotalDays > 3)
                        {
                            removeMap.Add(article.Key, article.Value);
                        }
                    }

                    foreach (KeyValuePair<string, dynamic> article in removeMap)
                    {
                        Console.WriteLine("REMOVING: " + article.Key);
                        articleMap.Remove(article.Key);
                    }
                    Console.WriteLine("Sleeping for " + sleep + " minutes...");
                    System.Threading.Thread.Sleep(60000 * sleep);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    System.Threading.Thread.Sleep(60000 * sleep);
                }
            }
        }
    }
}
