using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OrMan.Models
{
    public class CartItem : INotifyPropertyChanged
    {
        public MonAn MonAn { get; set; }

        // [SỬA] Chuyển SoLuong thành Full Property để có OnPropertyChanged
        private int _soLuong;
        public int SoLuong
        {
            get => _soLuong;
            set
            {
                if (_soLuong != value)
                {
                    _soLuong = value;
                    OnPropertyChanged(); // Báo UI cập nhật số lượng
                    OnPropertyChanged(nameof(ThanhTien)); // Báo UI cập nhật thành tiền dòng này
                }
            }
        }

        public string GhiChu { get; set; }
        public int CapDoCay { get; set; }

        public decimal ThanhTien => MonAn.GiaBan * SoLuong;

        public string TenHienThi
        {
            get
            {
                if (MonAn is MonMiCay) return $"{MonAn.TenMon} (Cấp {CapDoCay})";
                return MonAn.TenMon;
            }
        }

        public CartItem(MonAn mon, int sl, int capDo, string note)
        {
            MonAn = mon;
            SoLuong = sl;
            CapDoCay = capDo;
            GhiChu = note;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}