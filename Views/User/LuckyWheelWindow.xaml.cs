using Orman.Models;
using OrMan.Data;
using OrMan.Models; // Namespace chứa KhachHang và MenuContext
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
            new PhanThuong { Ten = "Pepsi", TiLe = 20 },    // Index 2
            new PhanThuong { Ten = "Voucher 20k", TiLe = 5 },    // Index 3
            new PhanThuong { Ten = "Chúc may mắn", TiLe = 40 },  // Index 4
            new PhanThuong { Ten = "Voucher 5%", TiLe = 10 },    // Index 5
            new PhanThuong { Ten = "Kimchi", TiLe = 20 },   // Index 6
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
                string template = GetRes("Str_CurrentPoints_Format");
                txtDiem.Text = string.Format(template, _khachHang.DiemTichLuy);
                string btnTemplate = GetRes("Str_Btn_SpinNow_Format");
                btnSpin.Content = string.Format(btnTemplate, _costPerSpin);
                btnSpin.IsEnabled = _khachHang.DiemTichLuy >= _costPerSpin;
                btnSpin.Opacity = btnSpin.IsEnabled ? 1 : 0.5;
            }
        }
        // Thêm hàm helper để convert từ PhanThuong object sang Data Model
        private void LuuPhanThuongVaoDb(PhanThuong prize)
        {
            // Nếu trúng ô "Chúc may mắn" thì không lưu gì cả
            if (prize.Ten.Contains("Chúc may mắn") || prize.Ten.Contains("Trượt")) return;

            using (var db = new MenuContext())
            {
                var voucher = new VoucherCuaKhach
                {
                    KhachHangID = _khachHang.KhachHangID,
                    TenPhanThuong = prize.Ten,
                    NgayTrungThuong = DateTime.Now,
                    DaSuDung = false
                };

                // Phân loại đơn giản dựa trên Tên (Hoặc bạn có thể thêm thuộc tính Type vào class PhanThuong)
                if (prize.Ten.Contains("Voucher 50k")) { voucher.LoaiVoucher = 1; voucher.GiaTri = 50000; }
                else if (prize.Ten.Contains("Voucher 20k")) { voucher.LoaiVoucher = 1; voucher.GiaTri = 20000; }
                else if (prize.Ten.Contains("%"))
                {
                    voucher.LoaiVoucher = 2;
                    // Lấy số ra từ chuỗi (ví dụ "10%" -> 10)
                    voucher.GiaTri = double.Parse(System.Text.RegularExpressions.Regex.Match(prize.Ten, @"\d+").Value);
                }
                else // Tặng món (Free Pepsi, Free Kimchi)
                {
                    voucher.LoaiVoucher = 3;
                    voucher.GiaTri = 0;
                }

                db.VoucherCuaKhachs.Add(voucher);
                db.SaveChanges();
            }
        }
        private string GetRes(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }
        // Cập nhật sự kiện Click
        private void BtnSpin_Click(object sender, RoutedEventArgs e)
        {
            if (_isSpinning) return;

            // 1. Kiểm tra điểm local trước cho nhanh
            if (_khachHang.DiemTichLuy < _costPerSpin)
            {
                MessageBox.Show(GetRes("Str_Msg_NotEnoughPoints"),
                         GetRes("Str_Title_Notice"),
                         MessageBoxButton.OK,
                         MessageBoxImage.Warning);
                return;
            }

            _isSpinning = true;
            btnSpin.SetResourceReference(ContentControl.ContentProperty, "Str_Btn_Spinning");

            // 2. Tính toán kết quả TRƯỚC khi quay (Backend Logic)
            int selectedIndex = GetRandomPrizeIndex();
            PhanThuong wonPrize = _prizes[selectedIndex];

            // 3. Trừ điểm và Lưu quà vào DB NGAY LẬP TỨC (Tránh trường hợp khách tắt máy giữa chừng)
            using (var db = new MenuContext())
            {
                var k = db.KhachHangs.Find(_khachHang.KhachHangID);
                if (k != null)
                {
                    if (k.DiemTichLuy < _costPerSpin)
                    {
                        MessageBox.Show("Điểm không đủ (đồng bộ chậm). Vui lòng thử lại.");
                        _isSpinning = false;
                        return;
                    }

                    k.DiemTichLuy -= _costPerSpin;
                    db.SaveChanges(); // Lưu trừ điểm

                    // Cập nhật giao diện
                    _khachHang.DiemTichLuy = k.DiemTichLuy;
                    UpdatePointDisplay();
                }
            }

            // Lưu phần thưởng (Hàm viết ở trên)
            LuuPhanThuongVaoDb(wonPrize);

            // 4. Bắt đầu Animation (UI Logic)
            double segmentAngle = 360.0 / _prizes.Count;
            double targetAngle = 360 - (selectedIndex * segmentAngle) - (segmentAngle / 2);
            double totalRotation = (360 * 5) + targetAngle;

            DoubleAnimation rotateAnim = new DoubleAnimation
            {
                From = 0,
                To = totalRotation,
                Duration = new Duration(TimeSpan.FromSeconds(5)),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 6 }
            };

            rotateAnim.Completed += (s, _) =>
            {
                _isSpinning = false;
                string btnTemplate = GetRes("Str_Btn_SpinNow_Format");
                btnSpin.Content = string.Format(btnTemplate, _costPerSpin);
                WheelRotation.Angle = targetAngle; // Giữ nguyên vị trí kim

                if (wonPrize.Ten.Contains("Chúc may mắn"))
                {
                    MessageBox.Show(GetRes("Str_Msg_SpinLost"),
                        GetRes("Str_Title_Result"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    string msgTemplate = GetRes("Str_Msg_SpinWon_Format");
                    string message = string.Format(msgTemplate, wonPrize.Ten);

                    MessageBox.Show(message,
                                    GetRes("Str_Title_Win"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Exclamation);
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