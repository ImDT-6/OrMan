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

        private string _trangThai;
        public string TrangThai
        {
            get => _trangThai;
            set { if (_trangThai != value) { _trangThai = value; OnPropertyChanged(); } }
        }

        // [MỚI] Lưu hình thức thanh toán khách chọn (Tiền mặt, QR, Thẻ)
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
                    OnPropertyChanged(nameof(HienThiYeuCau)); // Cập nhật text hiển thị
                }
            }
        }

        // [MỚI] Lưu nội dung yêu cầu hỗ trợ (Ví dụ: "Xin thêm nước")
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
                    OnPropertyChanged(nameof(HienThiYeuCau)); // Cập nhật text hiển thị
                }
            }
        }

        // [MỚI] Property phụ trợ để hiển thị trên Dashboard Admin
        public string HienThiYeuCau
        {
            get
            {
                if (YeuCauThanhToan) return "Yêu cầu thanh toán";
                if (!string.IsNullOrEmpty(YeuCauHoTro)) return $"Hỗ trợ: {YeuCauHoTro}";
                return "";
            }
        }


        public string TenBan => $"Bàn {SoBan:00}";

        protected BanAn() { }

        public BanAn(int soBan, string trangThai = "Trống")
        {
            SoBan = soBan;
            TrangThai = trangThai;
            YeuCauThanhToan = false;
            YeuCauHoTro = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}