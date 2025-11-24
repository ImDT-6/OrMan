using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GymManagement.Models
{
    // Class đại diện cho 1 dòng trong giỏ hàng
    public class CartItem : INotifyPropertyChanged
    {
        public MonAn MonAn { get; set; }
        public int SoLuong { get; set; }
        public string GhiChu { get; set; }
        public int CapDoCay { get; set; } // 0-7 (Chỉ dùng cho Mì Cay)

        // Tính tổng tiền của dòng này
        public decimal ThanhTien => MonAn.GiaBan * SoLuong;

        // Tên hiển thị kèm cấp độ (VD: Mì Bò - Cấp 3)
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