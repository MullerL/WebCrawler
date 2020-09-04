using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Processamento iniciado aguarde");
            StartCrawlerAsync();
            Console.ReadLine();
        }
        private static async Task StartCrawlerAsync()
        {
            var path = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var parentDir = "";

            parentDir = Directory.GetParent(path).Parent.Parent + "\\HtmlPagina";
            System.IO.Directory.Delete(parentDir, true);
            Directory.CreateDirectory(parentDir);

            DateTime dataInicio = DateTime.Now;
            int qtdLinhasTotal = 0;
            int pageIndex = 1;
            HtmlNodeCollection divs;
            List <FreeProxy> freeProxyList = new List<FreeProxy>();
            do
            {
                var url = "https://proxyservers.pro/proxy/list/order/updated/order_dir/asc/page/" + pageIndex;
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                divs = htmlDocument.DocumentNode.SelectNodes("//tr[@valign='top']");

                if (divs != null)
                {
                    qtdLinhasTotal += divs.Count();

                    foreach (var item in divs)
                    {
                        FreeProxy freeProxy = new FreeProxy();
                        freeProxy.IpAdress = item.Descendants("a").FirstOrDefault().InnerText;
                        freeProxy.Port = item.SelectSingleNode("td[3]/span").InnerText;
                        freeProxy.Country = item.SelectSingleNode("td[4]").InnerText.Replace("\n","").Trim();
                        freeProxy.Protocol = item.SelectSingleNode("td[7]").InnerText;

                        freeProxyList.Add(freeProxy);
                    }
                    
                    parentDir = Directory.GetParent(path).Parent.Parent + "\\HtmlPagina";
                    System.IO.File.WriteAllText(parentDir + "\\htmlPagina" + pageIndex + ".html", html);

                    pageIndex++;
                }
                
            } while (divs != null);

            DateTime dataTermino = DateTime.Now;

            string json = JsonConvert.SerializeObject(freeProxyList);
            parentDir = Directory.GetParent(path).Parent.Parent + "\\Json";
            System.IO.File.WriteAllText(parentDir + "\\arquivo.json", json);

            try
            {
                parentDir = Directory.GetParent(path).Parent.Parent + "\\DatabaseWebCrawler.mdf";
                SqlConnection con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + parentDir + ";Integrated Security=True");              
                con.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.Text;
                string bcd = "INSERT INTO FreeProxy VALUES (@DataInicio, @DataTermino, @QtdPaginas, @QtdLinhasExtraidas, @Json)";
                cmd = new SqlCommand(bcd, con);
                cmd.Parameters.AddWithValue("@DataInicio", dataInicio);
                cmd.Parameters.AddWithValue("@DataTermino", dataTermino);
                cmd.Parameters.AddWithValue("@QtdPaginas", pageIndex - 1);
                cmd.Parameters.AddWithValue("@QtdLinhasExtraidas", qtdLinhasTotal);
                cmd.Parameters.AddWithValue("@Json", json);

                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
            Console.WriteLine("Processamento concluido");
        }
    }
}
