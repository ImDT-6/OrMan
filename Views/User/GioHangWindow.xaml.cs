using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OrMan.Data;
using OrMan.Models;
using OrMan.ViewModels.User;

namespace OrMan.Views.User
{
    public partial class GioHangWindow : Window
    {
        private UserViewModel _vm;

        // Biến lưu voucher đang chọn
        private VoucherCuaKhach _selectedVoucher = null;
        private decimal _finalTotal = 0; // Lưu số tiền cuối cùng để thanh toán

        public GioHangWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = _vm;

            // Load voucher và tính tiền lần đầu
            Loaded += GioHangWindow_Loaded;
        }

        // Nút Đóng (X)
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Sự kiện khi cửa sổ hiện lên
        private void GioHangWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVouchers();
            CalculateTotal(); // Tính toán hiển thị ban đầu
        }

        // [QUAN TRỌNG] Hàm tải danh sách voucher từ Database
        private void LoadVouchers()
        {
            try
            {
                // Kiểm tra null để tránh lỗi
                if (_vm.CurrentCustomer == null || _vm.CurrentCustomer.KhachHangID <= 0) return;

                using (var db = new MenuContext())
                {
                    var list = db.VoucherCuaKhachs
                                 .Where(v => v.KhachHangID == _vm.CurrentCustomer.KhachHangID && v.DaSuDung == false)
                                 .ToList();

                    // Sử dụng class VoucherItem (khai báo ở cuối file) thay vì dynamic để hiển thị tên đẹp
                    var displayList = new List<VoucherItem>();

                    // Thêm option mặc định
                    displayList.Add(new VoucherItem { Id = 0, TenHienThi = "--- Chọn mã ưu đãi ---", Data = null });

                    foreach (var v in list)
                    {
                        string ten = v.TenPhanThuong;
                        // Format tên cho đẹp
                        if (v.LoaiVoucher == 1) ten += $" (Giảm {v.GiaTri:N0}đ)";
                        else if (v.LoaiVoucher == 2) ten += $" (Giảm {v.GiaTri}%)";

                        displayList.Add(new VoucherItem { Id = v.Id, TenHienThi = ten, Data = v });
                    }

                    if (cboVoucher != null)
                    {
                        cboVoucher.ItemsSource = displayList;
                        cboVoucher.DisplayMemberPath = "TenHienThi"; // Chỉ định hiển thị thuộc tính TenHienThi
                        cboVoucher.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải voucher: " + ex.Message);
            }
        }

        // Sự kiện khi chọn Voucher
        private void CboVoucher_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboVoucher == null) return;

            // Ép kiểu về VoucherItem
            var selectedItem = cboVoucher.SelectedItem as VoucherItem;

            if (selectedItem != null)
            {
                _selectedVoucher = selectedItem.Data; // Lấy dữ liệu thật
                CalculateTotal();
            }
        }

        // Hàm tính toán lại tiền
        private void CalculateTotal()
        {
            // Lấy tổng tiền gốc từ ViewModel
            decimal subTotal = _vm.TongTienCart;
            decimal discountAmount = 0;

            if (_selectedVoucher != null)
            {
                if (_selectedVoucher.LoaiVoucher == 1) // Giảm tiền mặt
                {
                    discountAmount = (decimal)_selectedVoucher.GiaTri;
                }
                else if (_selectedVoucher.LoaiVoucher == 2) // Giảm %
                {
                    discountAmount = subTotal * (decimal)(_selectedVoucher.GiaTri / 100.0);
                }
            }

            // Không cho giảm quá số tiền gốc
            if (discountAmount > subTotal) discountAmount = subTotal;

            // Cập nhật giao diện (Kiểm tra null an toàn)
            if (txtGiamGia != null)
                txtGiamGia.Text = $"-{discountAmount:N0} VNĐ";

            _finalTotal = subTotal - discountAmount;

            if (txtThanhTien != null)
                txtThanhTien.Text = $"{_finalTotal:N0} VNĐ";
        }

        // Nút Gửi Đơn / Thanh Toán
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.GioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống!");
                return;
            }

            // Xử lý Voucher: Đánh dấu đã dùng
            if (_selectedVoucher != null)
            {
                using (var db = new MenuContext())
                {
                    var v = db.VoucherCuaKhachs.Find(_selectedVoucher.Id);
                    if (v != null)
                    {
                        v.DaSuDung = true;
                        v.NgaySuDung = DateTime.Now;
                        db.SaveChanges();
                    }
                }
                MessageBox.Show($"Áp dụng voucher thành công! Tổng thanh toán: {_finalTotal:N0}đ");
            }

            // Đóng cửa sổ và trả về Result = true
            this.DialogResult = true;
            this.Close();
        }

        // --- Các nút Tăng/Giảm/Xóa ---
        private void BtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn.DataContext as CartItem;
            if (item != null)
            {
                _vm.GiamSoLuongMon(item);
                CalculateTotal(); // Tính lại tiền ngay
            }
        }

        private void BtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn.DataContext as CartItem;
            if (item != null)
            {
                _vm.TangSoLuongMon(item);
                CalculateTotal(); // Tính lại tiền ngay
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var itemToDelete = btn.DataContext as CartItem;

            if (itemToDelete != null)
            {
                var result = MessageBox.Show($"Bạn có chắc muốn bỏ món '{itemToDelete.TenHienThi}' không?",
                                             "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _vm.XoaMonKhoiGio(itemToDelete);
                    CalculateTotal(); // Tính lại tiền ngay sau khi xóa
                }
            }
        }
    }

    // [CLASS MỚI] Dùng class này để hiển thị trên ComboBox đẹp hơn
    public class VoucherItem
    {
        public int Id { get; set; }
        public string TenHienThi { get; set; }
        public VoucherCuaKhach Data { get; set; }

        // Hàm này giúp ComboBox hiển thị tên thay vì tên Class
        public override string ToString()
        {
            return TenHienThi;
        }
    }
}