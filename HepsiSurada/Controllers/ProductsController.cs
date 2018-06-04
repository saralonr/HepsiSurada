using HepsiSurada.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace HepsiSurada.Controllers
{
    public class ProductsController : ApiController
    {
        HepsiSuradaEntities db = new HepsiSuradaEntities();
        [HttpGet]
        public IHttpActionResult Search(string q)
        {
            int counter = 0;
            try
            {
                if (q == null) return Json("Parametre verilmemiş.");
                //Arama sonuçlarının geleceği sayfaya istek atıyoruz.
                string url = "https://www.hepsiburada.com/ara?q=" + q;
                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;
                string raw = client.DownloadString(url);

                List<Products> productList = new List<Products>();

                //Arama sonuçları sayfasının HTML içeriğini HTMLDocument'a dönüştürüyoruz. Paramparça edip ilgili ürünün detay sayfasına gidicez birazdan.
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(raw);
                //Sayfadaki ürünleri aldık bir kenarı. Sırayla dönüp box ın içindeki a'nın href ini alıyoruz.
                HtmlNodeCollection products = doc.DocumentNode.SelectSingleNode("//ul[contains(@class,'product-list')]").SelectNodes("li");
                foreach (HtmlNode pd in products)
                {
                    #region Product Save
                    WebClient productDetailClient = new WebClient();
                    productDetailClient.Encoding = Encoding.UTF8;
                    string productDetailurl = pd.SelectSingleNode("div[contains(@class,'box product')]").SelectSingleNode("a").Attributes["href"].Value;
                    //Ürünün detay sayfasına gidiyoruz.
                    productDetailurl = "https://www.hepsiburada.com" + productDetailurl;
                    string productDetailRaw = productDetailClient.DownloadString(productDetailurl);
                    HtmlDocument productDoc = new HtmlDocument();
                    productDoc.LoadHtml(productDetailRaw);

                    Products newProduct = new Products();
                    //Burada ürün bilgilerine ulaşıyoruz artık. İsim , 1 resim, eğer indirim varsa indirimli hali yoksa normal fiyatı, ve daha sonra özellikleri. Özellikler ayrı tabloda tutuluyor.
                    newProduct.ProductName = productDoc.DocumentNode.SelectSingleNode("//h1[@id='product-name']").InnerText.Trim();
                    Products checkProduct = db.Products.FirstOrDefault(x => x.ProductName == newProduct.ProductName);
                    if (checkProduct != null) continue;
                    HtmlNode dd = productDoc.DocumentNode.SelectSingleNode("//div[@id='productDetailsCarousel']").SelectSingleNode("a").SelectSingleNode("img");
                    newProduct.ProductImage = productDoc.DocumentNode.SelectSingleNode("//div[@id='productDetailsCarousel']").SelectSingleNode("a").SelectSingleNode("img").Attributes["data-img"].Value.Replace("#imgSize", "1024");
                    HtmlNode extraDiscount = productDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'extra-discount-price')]");
                    if (extraDiscount == null || extraDiscount.InnerText == " TL")
                    {
                        newProduct.ProductPrice = productDoc.DocumentNode.SelectSingleNode("//span[@id='offering-price']").Attributes["content"].Value.Trim();
                    }
                    else
                    {
                        newProduct.ProductPrice = extraDiscount.InnerText.Trim();
                    }

                    db.Products.Add(newProduct);
                    db.SaveChanges();
                    counter++;

                    //Özellikleri aldığımız yer.
                    HtmlNodeCollection tables = productDoc.DocumentNode.SelectNodes("//table[contains(@class,'data-list tech-spec')]");
                    foreach (HtmlNode table in tables)
                    {
                        HtmlNodeCollection trList = table.SelectSingleNode("tbody").SelectNodes("tr");
                        foreach (HtmlNode tr in trList)
                        {
                            ProductSpecs spec = new ProductSpecs();
                            spec.SpecKey = HtmlEntity.DeEntitize(tr.SelectSingleNode("th").InnerText.Trim());
                            spec.SpecValue = HtmlEntity.DeEntitize(tr.SelectSingleNode("td").InnerText.Trim());
                            spec.ProductID = newProduct.ID;
                            db.ProductSpecs.Add(spec);
                            db.SaveChanges();
                            newProduct.ProductSpecs.Add(spec);
                        }
                    }
                    productList.Add(newProduct);
                    #endregion
                }

                //Eğer tek sayfa değilse kaç sayfa var? Bu sayfa sayısı kadar for içinde dön ama 2.sayfadan başlıyoruz zaten 1'i dolaştık.Geri kalan işlemler yukarıdakiyle aynı.
                HtmlNode pagination = doc.DocumentNode.SelectSingleNode("//div[@id='pagination']");
                if (pagination != null)
                {
                    HtmlNodeCollection pages = pagination.SelectSingleNode("ul").SelectNodes("li");
                    HtmlNode lastPage = pages[pages.Count - 1];
                    int pageCount = Convert.ToInt32(lastPage.InnerText);
                    for (int i = 2; i <= pageCount; i++)
                    {
                        WebClient pageClient = new WebClient();
                        pageClient.Encoding = Encoding.UTF8;
                        string pageUrl = "https://www.hepsiburada.com/ara?q=" + q + "&sayfa=" + i;
                        string pageRaw = pageClient.DownloadString(pageUrl);
                        HtmlDocument pageDoc = new HtmlDocument();
                        pageDoc.LoadHtml(pageRaw);

                        products = pageDoc.DocumentNode.SelectSingleNode("//ul[contains(@class,'product-list')]").SelectNodes("li");
                        foreach (HtmlNode product in products)
                        {
                            #region Product Save
                            WebClient productDetailClient = new WebClient();
                            productDetailClient.Encoding = Encoding.UTF8;
                            string productDetailUrl = product.SelectSingleNode("div[contains(@class,'box product')]").SelectSingleNode("a").Attributes["href"].Value;
                            productDetailUrl = "https://www.hepsiburada.com" + productDetailUrl;
                            string productDetailRaw = productDetailClient.DownloadString(productDetailUrl);

                            HtmlDocument productDoc = new HtmlDocument();
                            productDoc.LoadHtml(productDetailRaw);

                            Products newProduct = new Products();
                            newProduct.ProductName = productDoc.DocumentNode.SelectSingleNode("//h1[@id='product-name']").InnerText.Trim();
                            Products checkProduct = db.Products.FirstOrDefault(x => x.ProductName == newProduct.ProductName);
                            if (checkProduct != null) continue;
                            newProduct.ProductImage = productDoc.DocumentNode.SelectSingleNode("//div[@id='productDetailsCarousel']").SelectSingleNode("a").SelectSingleNode("img").Attributes["data-img"].Value.Replace("#imgSize", "1024");
                            HtmlNode extraDiscount = productDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'extra-discount-price')]");
                            if (extraDiscount == null || extraDiscount.InnerText == " TL")
                            {
                                newProduct.ProductPrice = productDoc.DocumentNode.SelectSingleNode("//span[@id='offering-price']").Attributes["content"].Value.Trim();
                            }
                            else
                            {
                                newProduct.ProductPrice = extraDiscount.InnerText.Trim();
                            }

                            db.Products.Add(newProduct);
                            db.SaveChanges();
                            productList.Add(newProduct);
                            counter++;

                            HtmlNodeCollection tables = productDoc.DocumentNode.SelectNodes("//table[contains(@class,'data-list tech-spec')]");
                            foreach (HtmlNode table in tables)
                            {
                                HtmlNodeCollection trList = table.SelectSingleNode("tbody").SelectNodes("tr");
                                foreach (HtmlNode tr in trList)
                                {
                                    ProductSpecs spec = new ProductSpecs();
                                    spec.SpecKey = HtmlEntity.DeEntitize(tr.SelectSingleNode("th").InnerText.Trim());
                                    spec.SpecValue = HtmlEntity.DeEntitize(tr.SelectSingleNode("td").InnerText.Trim());
                                    spec.ProductID = newProduct.ID;
                                    db.ProductSpecs.Add(spec);
                                    db.SaveChanges();
                                    newProduct.ProductSpecs.Add(spec);
                                }
                            }
                            productList.Add(newProduct);
                            #endregion
                        }
                    }
                }

                return Json(productList.Select(x => new {
                    ProductName = x.ProductName,
                    ProductImage = x.ProductImage,
                    ProductPrice = x.ProductPrice,
                    ProductSpec = x.ProductSpecs.Select(y=> new {
                        Key = y.SpecKey,
                        Value = y.SpecValue
                    })
                }).ToList());
            }
            catch (Exception ex)
            {
                return Json("Hata meydana geldi.Şimdiye kadar "+counter+" adet ürün kaydedildi.");
            }
        }
        

    }
}
