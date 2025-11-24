using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations; // Thêm dòng này
using System.ComponentModel.DataAnnotations.Schema; // Thêm dòng này

namespace GymManagement.Models
{
    // Class cha
    public class MonAn : INotifyPropertyChanged
    {
        [Key] // Đánh dấu đây là Khóa chính
        public string MaMon { get; set; }
        public string TenMon { get; set; }
        public decimal GiaBan { get; set; }
        public string DonViTinh { get; set; }
        public string HinhAnhUrl { get; set; }

        // [BẮT BUỘC] Constructor rỗng cho Entity Framework
        protected MonAn() { }

        public MonAn(string maMon, string tenMon, decimal giaBan, string donViTinh)
        {
            MaMon = maMon;
            TenMon = tenMon;
            GiaBan = giaBan;
            DonViTinh = donViTinh;
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

        // Constructor rỗng
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

        // Constructor rỗng
        protected MonPhu() { }

        public MonPhu(string maMon, string tenMon, decimal giaBan, string donViTinh, string theLoai)
            : base(maMon, tenMon, giaBan, donViTinh)
        {
            TheLoai = theLoai;
        }
    }
}