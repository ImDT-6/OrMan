using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    public class BanAn : INotifyPropertyChanged
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SoBan { get; set; }

        // [SỬA QUAN TRỌNG] Logic set/get đầy đủ để giao diện tự đổi màu
        private string _trangThai;
        public string TrangThai
        {
            get => _trangThai;
            set { if (_trangThai != value) { _trangThai = value; OnPropertyChanged(); } }
        }

        private bool _yeuCauThanhToan;
        public bool YeuCauThanhToan
        {
            get => _yeuCauThanhToan;
            set { if (_yeuCauThanhToan != value) { _yeuCauThanhToan = value; OnPropertyChanged(); } }
        }

        public string TenBan => $"Bàn {SoBan:00}";

        protected BanAn() { }

        public BanAn(int soBan, string trangThai = "Trống")
        {
            SoBan = soBan;
            TrangThai = trangThai;
            YeuCauThanhToan = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}