using System;
using System.ComponentModel.DataAnnotations;

namespace SmartWarehouse.Models
{
    public class ModelUpdateTracker
    {
        [Key]
        public int Id { get; set; }

        public DateTime LastUpdateDate { get; set; } // Son güncelleme tarihi

        public int SalesSinceLastUpdate { get; set; } // Güncellemeden sonraki satış sayısı

        public string ModelVersion { get; set; } // Versiyon (Örn: 20251213_1530)

        public int TotalSalesUsed { get; set; } // Eğitimde kullanılan toplam veri
    }
}