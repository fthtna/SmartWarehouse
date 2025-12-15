using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SmartWarehouse.Models;
using System.Data.Entity;
using SmartWarehouse.Services; // MLService'i tanıması için

namespace SmartWarehouse.Controllers
{
    [Authorize]
    public class SatisController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var satislar = db.Satislar.Include(s => s.Urun).Include(s => s.Musteri).ToList();
            return View(satislar);
        }

        public ActionResult YeniSatis()
        {
            List<SelectListItem> urunler = (from x in db.Urunler.ToList()
                                            select new SelectListItem
                                            {
                                                Text = x.UrunAdi,
                                                Value = x.UrunId.ToString()
                                            }).ToList();

            List<SelectListItem> musteriler = (from x in db.Musteriler.ToList()
                                               select new SelectListItem
                                               {
                                                   Text = x.MusteriAdi + " " + x.MusteriSoyadi,
                                                   Value = x.MusteriId.ToString()
                                               }).ToList();

            ViewBag.UrunListesi = urunler;
            ViewBag.MusteriListesi = musteriler;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult YeniSatis(Satis satis)
        {
            // 1. SATIŞI KAYDET
            var urun = db.Urunler.Find(satis.UrunId);
            var musteri = db.Musteriler.Find(satis.MusteriId);

            satis.Urun = urun;
            satis.Musteri = musteri;
            satis.Tarih = DateTime.Now;
            satis.Tutar = urun.Fiyat * satis.Adet;

            // Stok düşme işlemi
            urun.Stok = urun.Stok - satis.Adet;

            db.Satislar.Add(satis);
            db.SaveChanges(); // Satış veritabanına işlendi

            // --- 2. OTOMATİK EĞİTİM SİSTEMİ ---
            try
            {
                // Takipçiyi bul
                var tracker = db.ModelUpdateTrackers.OrderByDescending(m => m.LastUpdateDate).FirstOrDefault();

                if (tracker != null)
                {
                    // Sayacı 1 arttır
                    tracker.SalesSinceLastUpdate++;

                    // Eğer sayaç 20'ye ulaştıysa eğitimi başlat!
                    if (tracker.SalesSinceLastUpdate >= 20)
                    {
                        var mlService = new MLService();
                        mlService.EgitVeKaydet(); // Modeli Eğit

                        // Takipçiyi Sıfırla
                        tracker.LastUpdateDate = DateTime.Now;
                        tracker.SalesSinceLastUpdate = 0;
                        tracker.ModelVersion = "v" + DateTime.Now.ToString("yyyyMMdd-HHmm");
                        tracker.TotalSalesUsed = db.Satislar.Count();

                        // Başarılı olursa yeşil mesaj
                        TempData["SuccessMessage"] = "Satış yapıldı ve Yapay Zeka yeni verilerle kendini güncelledi! 🧠";
                    }
                    else
                    {
                        // Henüz 20 olmadıysa sadece kaydet
                        TempData["SuccessMessage"] = "Satış başarıyla gerçekleşti.";
                    }

                    // Takipçi tablosundaki değişikliği kaydet
                    db.SaveChanges();
                }
            }
            // --- HATA YAKALAMA ---
            catch (Exception ex)
            {
                string hataMesaji = "AI HATASI: " + ex.Message;
                if (ex.InnerException != null)
                {
                    hataMesaji += " | DETAY: " + ex.InnerException.Message;
                }
                TempData["ErrorMessage"] = hataMesaji;
                TempData["SuccessMessage"] = null;
            }

            return RedirectToAction("Index");
        }

        // --- YENİ EKLENEN KISIM: MANUEL EĞİTİM BUTONU ---
        // Bu metod sayesinde 20 satış beklemeden modeli zorla eğitebileceksin.
        [Authorize]
        public ActionResult ModeliZorlaEgit()
        {
            try
            {
                SmartWarehouse.Services.MLService mlService = new SmartWarehouse.Services.MLService();
                mlService.EgitVeKaydet(); // Modeli hemen şimdi eğit!

                // Takipçiyi sıfırla ki progress bar başa dönsün
                var tracker = db.ModelUpdateTrackers.OrderByDescending(t => t.LastUpdateDate).FirstOrDefault();
                if (tracker != null)
                {
                    tracker.SalesSinceLastUpdate = 0;
                    tracker.LastUpdateDate = DateTime.Now;
                    tracker.TotalSalesUsed = db.Satislar.Count();
                    db.SaveChanges();
                }

                TempData["SuccessMessage"] = "Yapay Zeka Modeli MANUEL olarak ve BAŞARIYLA güncellendi! 🧠";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Index", "Home"); // İşlem bitince Dashboard'a dön
        }
    }
}