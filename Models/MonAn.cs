using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    public class MonAn : INotifyPropertyChanged
    {
        [Key]
        public string MaMon { get; set; }
        public string TenMon { get; set; }
        public decimal GiaBan { get; set; }
        public string DonViTinh { get; set; }
        public string HinhAnhUrl { get; set; }

        // [SỬA] Chuyển thành Property đầy đủ
        private bool _isSoldOut;
        public bool IsSoldOut
        {
            get => _isSoldOut;
            set
            {
                if (_isSoldOut != value)
                {
                    _isSoldOut = value;
                    OnPropertyChanged();
                }
            }
        }

        protected MonAn() { }

        public MonAn(string maMon, string tenMon, decimal giaBan, string donViTinh)
        {
            MaMon = maMon;
            TenMon = tenMon;
            GiaBan = giaBan;
            DonViTinh = donViTinh;
            IsSoldOut = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MonMiCay : MonAn
    {
        public int CapDoCayMin { get; set; }
        public int CapDoCayMax { get; set; }
        public string LoaiMi { get; set; }
        protected MonMiCay() { }
        public MonMiCay(string maMon, string tenMon, decimal giaBan, string loaiMi, int min, int max)
            : base(maMon, tenMon, giaBan, "Tô")
        {
            LoaiMi = loaiMi;
            CapDoCayMin = min;
            CapDoCayMax = max;
        }
    }

    public class MonPhu : MonAn
    {
        public string TheLoai { get; set; }
        protected MonPhu() { }
        public MonPhu(string maMon, string tenMon, decimal giaBan, string donViTinh, string theLoai)
            : base(maMon, tenMon, giaBan, donViTinh)
        {
            TheLoai = theLoai;
        }
    }
}