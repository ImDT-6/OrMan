using Orman.Models;
using OrMan.Data;
using OrMan.Models; // Namespace chứa KhachHang và MenuContext
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OrMan.Views.User
{
    public partial class LuckyWheelWindow : Window
    {
        private KhachHang _khachHang;
        private readonly int _costPerSpin = 500; // Giá mỗi lần quay
        private bool _isSpinning = false;

        // Danh sách phần thưởng (Thứ tự phải khớp với hình vẽ trong XAML theo chiều kim đồng hồ)
        // Ô 1: 0-45 độ, Ô 2: 45-90 độ...
        // Mũi tên ở trên cùng (góc 0), vòng quay xoay chiều kim đồng hồ -> kết quả sẽ chạy ngược lại
        private readonly List<PhanThuong> _prizes = new List<PhanThuong>
        {
            new PhanThuong { Ten = "Voucher 10%", TiLe = 5 },    // Index 0
            new PhanThuong { Ten = "Chúc may mắn", TiLe = 40 },  // Index 1
            new PhanThuong { Ten = "Free Pepsi", TiLe = 20 },    // Index 2
            new PhanThuong { Ten = "Voucher 20k", TiLe = 5 },    // Index 3
            new PhanThuong { Ten = "Chúc may mắn", TiLe = 40 },  // Index 4
            new PhanThuong { Ten = "Voucher 5%", TiLe = 10 },    // Index 5
            new PhanThuong { Ten = "Free Kimchi", TiLe = 20 },   // Index 6
            new PhanThuong { Ten = "Voucher 50k", TiLe = 1 }     // Index 7 (Hiếm nhất)
        };

        public LuckyWheelWindow(KhachHang khach)
        {
            InitializeComponent();
            _khachHang = khach;
            UpdatePointDisplay();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSpinning) this.Close();
        }

        private void UpdatePointDisplay()
        {
            if (_khachHang != null)
            {
                txtDiem.Text = $"Điểm tích lũy: {_khachHang.DiemTichLuy:N0}";
                btnSpin.IsEnabled = _khachHang.DiemTichLuy >= _costPerSpin;
                btnSpin.Opacity = btnSpin.IsEnabled ? 1 : 0.5;
            }
        }

        private void BtnSpin_Click(object sender, RoutedEventArgs e)
        {
            if (_isSpinning) return;
            if (_khachHang.DiemTichLuy < _costPerSpin)
            {
                MessageBox.Show("Bạn không đủ điểm để quay!", "Thông báo");
                return;
            }

            // 1. Trừ điểm ngay lập tức
            using (var db = new MenuContext())
            {
                var k = db.KhachHangs.Find(_khachHang.KhachHangID);
                if (k != null)
                {
                    k.DiemTichLuy -= _costPerSpin;
                    db.SaveChanges();

                    _khachHang.DiemTichLuy = k.DiemTichLuy; // Cập nhật local
                    UpdatePointDisplay();
                }
            }

            _isSpinning = true;
            btnSpin.Content = "Đang quay...";

            // 2. Chọn giải thưởng ngẫu nhiên theo tỉ lệ
            int selectedIndex = GetRandomPrizeIndex();
            PhanThuong wonPrize = _prizes[selectedIndex];

            // 3. Tính toán góc quay
            // Một vòng 360 độ, có 8 ô -> mỗi ô 45 độ
            // Vì mũi tên cố định ở trên (Góc 0), nên muốn trúng ô Index i, ta phải xoay sao cho ô i nằm ở vị trí 0.
            // Công thức: 360 - (Index * 45) - (Lệch tâm 22.5 để vào giữa ô)

            double segmentAngle = 360.0 / _prizes.Count; // 45 độ
            double targetAngle = 360 - (selectedIndex * segmentAngle) - (segmentAngle / 2);

            // Cộng thêm 5 vòng quay (5 * 360) để tạo hiệu ứng quay nhanh
            double totalRotation = (360 * 5) + targetAngle;

            // 4. Animation
            DoubleAnimation rotateAnim = new DoubleAnimation
            {
                From = 0,
                To = totalRotation,
                Duration = new Duration(TimeSpan.FromSeconds(5)), // Quay trong 5 giây
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 6 } // Quay nhanh rồi chậm dần
            };

            rotateAnim.Completed += (s, _) =>
            {
                _isSpinning = false;
                btnSpin.Content = $"QUAY NGAY ({_costPerSpin} điểm)";

                // Reset góc quay về vị trí thực tế (để lần sau quay tiếp không bị lỗi)
                WheelRotation.Angle = targetAngle;

                // 5. Thông báo kết quả
                if (wonPrize.Ten.Contains("Chúc may mắn"))
                {
                    MessageBox.Show("Rất tiếc! Chúc bạn may mắn lần sau.", "Kết quả");
                }
                else
                {
                    MessageBox.Show($"CHÚC MỪNG! Bạn nhận được: {wonPrize.Ten}\n(Đưa màn hình này cho nhân viên để nhận quà)", "TRÚNG THƯỞNG");
                    // Ở đây bạn có thể lưu Voucher vào Database nếu muốn
                }
            };

            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
        }

        private int GetRandomPrizeIndex()
        {
            // Thuật toán quay số có trọng số (Weighted Random)
            // Tổng tỉ lệ không nhất thiết phải là 100, cứ cộng dồn là được
            Random rnd = new Random();
            double totalWeight = 0;
            foreach (var p in _prizes) totalWeight += p.TiLe;

            double randomNumber = rnd.NextDouble() * totalWeight;
            double currentSum = 0;

            for (int i = 0; i < _prizes.Count; i++)
            {
                currentSum += _prizes[i].TiLe;
                if (randomNumber <= currentSum)
                {
                    return i;
                }
            }
            return 0; // Fallback
        }
    }
}