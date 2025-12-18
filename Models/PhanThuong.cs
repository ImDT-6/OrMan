using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orman.Models
{
    public class PhanThuong
    {
        public string Ten { get; set; }
        public double TiLe { get; set; } // Tỉ lệ phần trăm (0-100)
        public string MauSac { get; set; } // Màu nền của ô
    }
}
