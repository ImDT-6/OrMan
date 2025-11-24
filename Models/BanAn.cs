using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [QUAN TRỌNG] Thêm dòng này

namespace GymManagement.Models
{
    public class BanAn : INotifyPropertyChanged
    {
        [Key]
        // [QUAN TRỌNG] Thêm dòng dưới để tắt tự động tăng ID
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SoBan { get; set; }

        public string TrangThai { get; set; }
        public string TenBan => $"Bàn {SoBan:00}";

        protected BanAn() { }

        public BanAn(int soBan, string trangThai = "Trống")
        {
            SoBan = soBan;
            TrangThai = trangThai;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}