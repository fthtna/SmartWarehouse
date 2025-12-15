using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SmartWarehouse.Models
{
    [Table("Satis")]
    public class Satis
    {
        [Key]
        public int SatisId { get; set; }

        public int Adet { get; set; }
        public decimal Tutar { get; set; }
        public System.DateTime Tarih { get; set; }

        public int UrunId { get; set; }
        public int MusteriId { get; set; }

        public virtual Urun Urun { get; set; }
        public virtual Musteri Musteri { get; set; }
    }
}