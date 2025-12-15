using SmartWarehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SmartWarehouse.Models;
using SmartWarehouse.Services;
using Microsoft.ML;
using System.IO;

namespace SmartWarehouse.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Dashboard Sayfası
        public ActionResult Index()
        {
            // --- 1. MEVCUT İSTATİSTİKLER ---
            ViewBag.SatisSayisi = db.Satislar.Count();
            ViewBag.UrunSayisi = db.Urunler.Count();
            ViewBag.KategoriSayisi = db.Kategoriler.Count();
            ViewBag.MusteriSayisi = db.Musteriler.Count();
            ViewBag.KritikStok = db.Urunler.Where(x => x.Stok < 20).Count();
            var ciro = db.Satislar.Sum(x => (decimal?)x.Tutar);
            ViewBag.Ciro = ciro ?? 0;

            // --- 2. GRAFİK VERİLERİ ---
            var grafikVerisi = db.Kategoriler
                             .Select(k => new {
                                 KategoriAdi = k.KategoriAdi,
                                 UrunSayisi = k.Urunler.Count()
                             })
                             .ToList();
            ViewBag.KategoriIsimleri = grafikVerisi.Select(x => x.KategoriAdi).ToList();
            ViewBag.KategoriUrunSayilari = grafikVerisi.Select(x => x.UrunSayisi).ToList();

            // --- 3. MODEL TAKİP SİSTEMİ ---
            var modelTracker = db.ModelUpdateTrackers.OrderByDescending(m => m.LastUpdateDate).FirstOrDefault();
            if (modelTracker == null)
            {
                modelTracker = new ModelUpdateTracker
                {
                    LastUpdateDate = DateTime.Now,
                    SalesSinceLastUpdate = 0,
                    ModelVersion = "v1.0 (Başlangıç)",
                    TotalSalesUsed = db.Satislar.Count()
                };
                db.ModelUpdateTrackers.Add(modelTracker);
                db.SaveChanges();
            }
            ViewBag.ModelTracker = modelTracker;

            // --- 4. TOPLU AI TAHMİNİ (YENİLENEN KISIM) ---
            // Sonuçları tutacak listemiz
            List<TahminSonuc> tahminListesi = new List<TahminSonuc>();

            try
            {
                // En çok satan 5 ürünü bul
                var populerUrunler = db.Satislar
                    .GroupBy(x => x.Urun)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList();

                // Tarih Hesaplama (Gelecek ay için)
                DateTime hedefTarih = DateTime.Now.AddDays(30);
                DateTime baslangicTarihi = new DateTime(2024, 1, 1);
                float gunSayisi = (float)(hedefTarih - baslangicTarihi).TotalDays;

                // Modeli Yükle
                MLContext mlContext = new MLContext();
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "SatisTahminModeli.mlnet");

                if (System.IO.File.Exists(modelPath) && populerUrunler.Count > 0)
                {
                    ITransformer trainedModel = mlContext.Model.Load(modelPath, out var modelSchema);
                    var predictionEngine = mlContext.Model.CreatePredictionEngine<SmartWarehouse.Services.ModelInput, SmartWarehouse.Services.SatisTahmin>(trainedModel);

                    // Her bir popüler ürün için döngüye gir
                    foreach (var urun in populerUrunler)
                    {
                        var input = new SmartWarehouse.Services.ModelInput
                        {
                            UrunId = (float)urun.UrunId,
                            TarihSayisal = gunSayisi,
                            Adet = 0 // Tahmin edilecek değer
                        };

                        var prediction = predictionEngine.Predict(input);

                        // Listeye ekle
                        tahminListesi.Add(new TahminSonuc
                        {
                            UrunAdi = urun.UrunAdi,
                            TahminEdilenSatis = (int)prediction.Score, // Küsuratı at
                            StokDurumu = urun.Stok
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata olursa boş liste gider, sorun çıkmaz
                System.Diagnostics.Debug.WriteLine("AI Hatası: " + ex.Message);
            }

            // Listeyi View'a gönder
            ViewBag.TahminListesi = tahminListesi;

            return View();
        }

        // Listeyi taşımak için minik bir yardımcı sınıf
        public class TahminSonuc
        {
            public string UrunAdi { get; set; }
            public int TahminEdilenSatis { get; set; }
            public int StokDurumu { get; set; }
        }
    }
}