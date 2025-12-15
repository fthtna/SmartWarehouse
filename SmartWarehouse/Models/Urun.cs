using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SmartWarehouse.Models
{
    public class Urun
    {
        [Key]
        public int UrunId { get; set; }

        public string UrunAdi { get; set; }
        public int Stok { get; set; }
        public decimal Fiyat { get; set; }

        public int KategoriId { get; set; }
        public virtual Kategori Kategori { get; set; }
    }
}