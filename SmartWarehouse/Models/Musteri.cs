using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SmartWarehouse.Models
{
    public class Musteri
    {
        [Key]
        public int MusteriId { get; set; }

        public string MusteriAdi { get; set; }
        public string MusteriSoyadi { get; set; }
        public string Telefon { get; set; }

        public ICollection<Satis> Satislar { get; set; }
    }
}