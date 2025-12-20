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
        private VoucherCuaKhach _selectedDiscountVoucher = null;
        private decimal _finalTotal = 0;

        // Danh sách ID các voucher quà tặng ĐANG CHỜ DÙNG trong phiên này
        private List<int> _pendingGiftVoucherIds = new List<int>();

        public GioHangWindow(UserViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = _vm;
            Loaded += GioHangWindow_Loaded;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void GioHangWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVouchers();
            CalculateTotal();
        }

        // --- 1. LOGIC TẢI VÀ PHÂN LOẠI VOUCHER ---
        private void LoadVouchers()
        {
            try
            {
                if (_vm.CurrentCustomer == null || _vm.CurrentCustomer.KhachHangID <= 0) return;

                using (var db = new MenuContext())
                {
                    var rawList = db.VoucherCuaKhachs
                                    .Where(v => v.KhachHangID == _vm.CurrentCustomer.KhachHangID && v.DaSuDung == false)
                                    .ToList();

                    // A. Voucher GIẢM TIỀN (Loại 1 & 2)
                    var discountVouchers = rawList
                        .Where(x => x.LoaiVoucher == 1 || x.LoaiVoucher == 2)
                        .GroupBy(x => new { x.LoaiVoucher, x.GiaTri, x.TenPhanThuong })
                        .Select(g => new VoucherItem
                        {
                            Id = g.First().Id,
                            Data = g.First(),
                            TenHienThi = FormatVoucherName(g.First(), g.Count())
                        })
                        .ToList();

                    discountVouchers.Insert(0, new VoucherItem { Id = 0, TenHienThi = "--- Chọn mã giảm giá ---", Data = null });

                    if (cboVoucher != null)
                    {
                        cboVoucher.ItemsSource = discountVouchers;
                        cboVoucher.SelectedIndex = 0;
                    }

                    // B. Voucher TẶNG MÓN (Loại 3)
                    // Chỉ hiện những voucher CHƯA nằm trong danh sách pending
                    var giftVouchers = rawList
                        .Where(x => x.LoaiVoucher == 3 && !_pendingGiftVoucherIds.Contains(x.Id))
                        .GroupBy(x => x.TenPhanThuong)
                        .Select(g => new GiftItem
                        {
                            Id = g.First().Id,
                            TenHienThi = g.First().TenPhanThuong,
                            SoLuongConLai = g.Count(),
                            Data = g.First()
                        })
                        .ToList();

                    if (icGifts != null)
                    {
                        icGifts.ItemsSource = giftVouchers;
                        icGifts.Visibility = giftVouchers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải voucher: " + ex.Message);
            }
        }

        private string FormatVoucherName(VoucherCuaKhach v, int count)
        {
            string ten = v.TenPhanThuong;
            if (v.LoaiVoucher == 1) ten += $" (Giảm {v.GiaTri:N0}đ)";
            else if (v.LoaiVoucher == 2) ten += $" (Giảm {v.GiaTri}%)";

            if (count > 1) ten += $" (x{count})";
            return ten;
        }

        // --- 2. XỬ LÝ NHẬN QUÀ TẶNG ---
        private void BtnNhanQua_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var gift = btn.Tag as GiftItem;

            if (gift != null && gift.Data != null)
            {
                bool success = AddGiftToCart(gift.Data);
                if (success)
                {
                    _pendingGiftVoucherIds.Add(gift.Data.Id); // Đưa vào danh sách đen tạm thời
                    LoadVouchers();
                    CalculateTotal();
                }
            }
        }

        private bool AddGiftToCart(VoucherCuaKhach voucher)
        {
            try
            {
                using (var db = new MenuContext())
                {
                    var monAn = db.MonAns.FirstOrDefault(m => m.TenMon == voucher.TenPhanThuong);

                    if (monAn == null)
                        monAn = db.MonAns.FirstOrDefault(m => m.TenMon.Contains(voucher.TenPhanThuong));

                    if (monAn == null)
                    {
                        string maMonAo = "GIFT_" + DateTime.Now.Ticks.ToString().Substring(10);

                        var newMon = new MonPhu(
                            maMonAo, voucher.TenPhanThuong, 0, "Phần", "Quà Tặng"
                        );
                        newMon.HinhAnhUrl = "";
                        newMon.IsSoldOut = false;

                        db.MonAns.Add(newMon);
                        db.SaveChanges();
                        monAn = newMon;
                    }

                    _vm.AddToCart(monAn, 1, 0, " (Quà tặng)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm quà: " + ex.Message);
                return false;
            }
        }

        // [MỚI] Hàm trả lại Voucher khi xóa món quà
        private void RestoreGiftVoucher(string tenMonQua)
        {
            if (_pendingGiftVoucherIds.Count == 0) return;

            using (var db = new MenuContext())
            {
                // Lấy thông tin các voucher đang bị treo (pending)
                var pendingVouchers = db.VoucherCuaKhachs
                                        .Where(v => _pendingGiftVoucherIds.Contains(v.Id))
                                        .ToList();

                // Tìm voucher nào khớp tên với món quà vừa xóa
                var voucherToRestore = pendingVouchers.FirstOrDefault(v => v.TenPhanThuong == tenMonQua);

                // Nếu tìm chính xác không thấy, thử tìm gần đúng (do lúc thêm mình tìm Contains)
                if (voucherToRestore == null)
                {
                    voucherToRestore = pendingVouchers.FirstOrDefault(v => tenMonQua.Contains(v.TenPhanThuong));
                }

                if (voucherToRestore != null)
                {
                    _pendingGiftVoucherIds.Remove(voucherToRestore.Id); // Xóa khỏi danh sách đen
                    LoadVouchers(); // Load lại để hiện nút "Thêm"
                }
            }
        }

        private void CboVoucher_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboVoucher == null) return;
            var selectedItem = cboVoucher.SelectedItem as VoucherItem;

            if (selectedItem != null)
            {
                _selectedDiscountVoucher = selectedItem.Data;
                CalculateTotal();
            }
        }

        // --- 3. TÍNH TOÁN ---
        private void CalculateTotal()
        {
            decimal totalCart = _vm.TongTienCart;

            decimal giftValue = _vm.GioHang
                .Where(x => x.GhiChu != null && x.GhiChu.Contains("(Quà tặng)"))
                .Sum(x => x.ThanhTien);

            decimal billableAmount = totalCart - giftValue;
            decimal voucherDiscount = 0;

            if (_selectedDiscountVoucher != null)
            {
                if (_selectedDiscountVoucher.LoaiVoucher == 1)
                    voucherDiscount = (decimal)_selectedDiscountVoucher.GiaTri;
                else if (_selectedDiscountVoucher.LoaiVoucher == 2)
                    voucherDiscount = billableAmount * (decimal)(_selectedDiscountVoucher.GiaTri / 100.0);
            }

            decimal totalDiscountDisplay = voucherDiscount + giftValue;
            if (totalDiscountDisplay > totalCart) totalDiscountDisplay = totalCart;

            if (txtGiamGia != null)
                txtGiamGia.Text = $"-{totalDiscountDisplay:N0} VNĐ";

            _finalTotal = totalCart - totalDiscountDisplay;
            if (txtThanhTien != null)
                txtThanhTien.Text = $"{_finalTotal:N0} VNĐ";
        }

        // --- 4. GỬI ĐƠN ---
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.GioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống!");
                return;
            }

            decimal tongGoc = _vm.TongTienCart;
            decimal tienGiam = tongGoc - _finalTotal;
            _vm.GiamGiaTamTinh = tienGiam;

            using (var db = new MenuContext())
            {
                if (_selectedDiscountVoucher != null)
                {
                    var v = db.VoucherCuaKhachs.Find(_selectedDiscountVoucher.Id);
                    if (v != null) { v.DaSuDung = true; v.NgaySuDung = DateTime.Now; }
                }

                foreach (int pendingId in _pendingGiftVoucherIds)
                {
                    var vGift = db.VoucherCuaKhachs.Find(pendingId);
                    if (vGift != null) { vGift.DaSuDung = true; vGift.NgaySuDung = DateTime.Now; }
                }

                db.SaveChanges();
            }

            this.DialogResult = true;
            this.Close();
        }

        // --- Event Button Tăng/Giảm/Xóa ---
        private void BtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn.DataContext as CartItem;
            if (item != null)
            {
                // Lưu lại thông tin trước khi giảm (phòng trường hợp món bị xóa mất)
                bool isGift = item.GhiChu != null && item.GhiChu.Contains("(Quà tặng)");
                string tenMon = item.MonAn.TenMon;

                _vm.GiamSoLuongMon(item);

                // Kiểm tra xem món có bị xóa khỏi giỏ chưa
                if (isGift && !_vm.GioHang.Contains(item))
                {
                    RestoreGiftVoucher(tenMon);
                }

                CalculateTotal();
            }
        }

        private void BtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn.DataContext as CartItem;
            if (item != null)
            {
                _vm.TangSoLuongMon(item);
                CalculateTotal();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var itemToDelete = btn.DataContext as CartItem;
            if (itemToDelete != null)
            {
                var result = MessageBox.Show($"Bỏ món '{itemToDelete.TenHienThi}'?", "Xác nhận", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    // [QUAN TRỌNG] Nếu xóa món quà, trả lại voucher
                    if (itemToDelete.GhiChu != null && itemToDelete.GhiChu.Contains("(Quà tặng)"))
                    {
                        RestoreGiftVoucher(itemToDelete.MonAn.TenMon);
                    }

                    _vm.XoaMonKhoiGio(itemToDelete);
                    CalculateTotal();
                }
            }
        }
    }

    public class VoucherItem
    {
        public int Id { get; set; }
        public string TenHienThi { get; set; }
        public VoucherCuaKhach Data { get; set; }
        public override string ToString() => TenHienThi;
    }

    public class GiftItem
    {
        public int Id { get; set; }
        public string TenHienThi { get; set; }
        public int SoLuongConLai { get; set; }
        public VoucherCuaKhach Data { get; set; }
    }
}