using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrMan.Models
{
    public class BanAn : INotifyPropertyChanged
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SoBan { get; set; }

        // [MỚI] Lưu tên riêng của bàn (Ví dụ: "VIP 1", "Góc Sân")
        private string _tenGoi;
        public string TenGoi
        {
            get => _tenGoi;
            set
            {
                if (_tenGoi != value)
                {
                    _tenGoi = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TenBan)); // Cập nhật luôn hiển thị
                }
            }
        }

        // [SỬA] Logic hiển thị tên: Ưu tiên TenGoi, nếu không có thì lấy "Bàn XX"
        public string TenBan => string.IsNullOrEmpty(TenGoi) ? $"Bàn {SoBan:00}" : TenGoi;

        private string _trangThai;
        public string TrangThai
        {
            get => _trangThai;
            set { if (_trangThai != value) { _trangThai = value; OnPropertyChanged(); } }
        }

        private string _hinhThucThanhToan;
        public string HinhThucThanhToan
        {
            get => _hinhThucThanhToan;
            set { if (_hinhThucThanhToan != value) { _hinhThucThanhToan = value; OnPropertyChanged(); } }
        }

        private bool _yeuCauThanhToan;
        public bool YeuCauThanhToan
        {
            get => _yeuCauThanhToan;
            set
            {
                if (_yeuCauThanhToan != value)
                {
                    _yeuCauThanhToan = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HienThiYeuCau));
                }
            }
        }

        private string _yeuCauHoTro;
        public string YeuCauHoTro
        {
            get => _yeuCauHoTro;
            set
            {
                if (_yeuCauHoTro != value)
                {
                    _yeuCauHoTro = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HienThiYeuCau));
                }
            }
        }

        public string HienThiYeuCau
        {
            get
            {
                if (YeuCauThanhToan) return "Yêu cầu thanh toán";
                if (!string.IsNullOrEmpty(YeuCauHoTro)) return $"Hỗ trợ: {YeuCauHoTro}";
                return "";
            }
        }

        private bool _daInTamTinh;
        [NotMapped]
        public bool DaInTamTinh
        {
            get => _daInTamTinh;
            set { if (_daInTamTinh != value) { _daInTamTinh = value; OnPropertyChanged(); } }
        }

        protected BanAn() { }

        public BanAn(int soBan, string trangThai = "Trống")
        {
            SoBan = soBan;
            TrangThai = trangThai;
            YeuCauThanhToan = false;
            YeuCauHoTro = null;
            DaInTamTinh = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}