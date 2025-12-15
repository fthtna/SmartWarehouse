using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree; // <--- FastTree Kütüphanesi
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace SmartWarehouse.Services
{
    public class MLService
    {
        private static string ModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "SatisTahminModeli.mlnet");

        public void EgitVeKaydet()
        {
            MLContext mlContext = new MLContext();

            // 1. BAĞLANTIYI AL
            string baglantiAdresi = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            // 2. VERİ YÜKLEME
            DatabaseLoader loader = mlContext.Data.CreateDatabaseLoader<ModelInput>();

            // NOT: Tablo adın 'Satis' olduğu için FROM Satis yazdık.
            string sqlKomutu = @"
                SELECT 
                    CAST(UrunId as REAL) as UrunId, 
                    CAST(DATEDIFF(day, '2024-01-01', Tarih) as REAL) as TarihSayisal, 
                    CAST(Adet as REAL) as Adet 
                FROM Satis";

            DatabaseSource dbSource = new DatabaseSource(SqlClientFactory.Instance, baglantiAdresi, sqlKomutu);

            try
            {
                IDataView trainingData = loader.Load(dbSource);

                // Veri kontrolü: Hiç veri yoksa işlem yapma
                var onizleme = trainingData.Preview(1);
                if (onizleme.RowView.Length == 0) return;

                // 3. EĞİTİM PLANI (PIPELINE) - FastTree Algoritması
                // Bu algoritma "Karar Ağacı" mantığıyla çalışır. 
                // Ürünler arasındaki satış farklarını (50 vs 1) çok daha net ayırır.
                var pipeline = mlContext.Transforms.Categorical.OneHotEncoding("UrunIdEncoded", "UrunId")
                    .Append(mlContext.Transforms.Concatenate("Features", "UrunIdEncoded", "TarihSayisal"))
                    .Append(mlContext.Regression.Trainers.FastTree(labelColumnName: "Adet", featureColumnName: "Features"));

                // 4. EĞİT
                var model = pipeline.Fit(trainingData);

                // 5. KAYDET
                mlContext.Model.Save(model, trainingData.Schema, ModelPath);
            }
            catch (Exception ex)
            {
                // Hatanın detayını yakala
                string hataDetayi = ex.Message;
                if (ex.InnerException != null)
                {
                    hataDetayi += " || KÖK NEDEN: " + ex.InnerException.Message;
                }

                throw new Exception("AI Eğitim Hatası: " + hataDetayi);
            }
        }
    }

    public class ModelInput
    {
        public float UrunId { get; set; }
        public float TarihSayisal { get; set; }
        public float Adet { get; set; }
    }

    public class SatisTahmin
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}