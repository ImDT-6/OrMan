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

        // --- Helper to get Resource String ---
        private string GetRes(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
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

                    // [UPDATED] Get placeholder text from Resource
                    string placeholder = GetRes("Str_SelectVoucher_Placeholder");
                    discountVouchers.Insert(0, new VoucherItem { Id = 0, TenHienThi = placeholder, Data = null });

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
                // [UPDATED] Localized Error Message
                string errTemplate = GetRes("Str_Err_LoadVoucher");
                MessageBox.Show(string.Format(errTemplate, ex.Message));
            }
        }

        private string FormatVoucherName(VoucherCuaKhach v, int count)
        {
            string ten = v.TenPhanThuong;

            // [UPDATED] Localized Discount Description
            if (v.LoaiVoucher == 1)
            {
                // "Giảm {0:N0}đ" or "Off {0:N0}đ"
                string format = GetRes("Str_Discount_Money");
                ten += $" ({string.Format(format, v.GiaTri)})";
            }
            else if (v.LoaiVoucher == 2)
            {
                // "Giảm {0}%" or "Off {0}%"
                string format = GetRes("Str_Discount_Percent");
                ten += $" ({string.Format(format, v.GiaTri)})";
            }

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

                    // [UPDATED] Localized Gift Note " (Quà tặng)" or " (Gift)"
                    string giftNote = GetRes("Str_Gift_Note");
                    _vm.AddToCart(monAn, 1, 0, giftNote);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // [UPDATED] Localized Error
                string errTemplate = GetRes("Str_Err_AddGift");
                MessageBox.Show(string.Format(errTemplate, ex.Message));
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

            // [UPDATED] Check for Gift Note using Localized string or generic check
            // Note: Since the note is saved in CartItem, we need to check both EN and VI strings if user switched lang mid-session
            // OR better: check if Price is 0 (assuming gifts are 0 cost)
            // But adhering to your logic:
            string giftNoteKey = "Str_Gift_Note";
            // NOTE: To be safe with language switching, checking price is safer, but let's stick to Note string for now.
            // Ideally, the 'Note' in CartItem should store a Code, not display text. 
            // For now, we fetch the CURRENT language's Gift Note.
            string currentGiftNote = GetRes(giftNoteKey);

            decimal giftValue = _vm.GioHang
                .Where(x => x.GhiChu != null && (x.GhiChu.Contains("(Quà tặng)") || x.GhiChu.Contains("(Gift)"))) // Check both to be safe
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

            // [UPDATED] Localized Currency Suffix
            string currencySuffix = GetRes("Str_Currency_Suffix"); // " VNĐ" or " VND"

            if (txtGiamGia != null)
                txtGiamGia.Text = $"-{totalDiscountDisplay:N0}{currencySuffix}";

            _finalTotal = totalCart - totalDiscountDisplay;
            if (txtThanhTien != null)
                txtThanhTien.Text = $"{_finalTotal:N0}{currencySuffix}";
        }

        // --- 4. GỬI ĐƠN ---
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.GioHang.Count == 0)
            {
                // [UPDATED] Localized Message
                MessageBox.Show(GetRes("Str_Msg_CartEmpty"));
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
                bool isGift = item.GhiChu != null && (item.GhiChu.Contains("(Quà tặng)") || item.GhiChu.Contains("(Gift)"));
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
                // [UPDATED] Localized Confirmation
                string msgTemplate = GetRes("Str_Msg_ConfirmRemoveItem");
                string title = GetRes("Str_Title_Confirm");

                var result = MessageBox.Show(string.Format(msgTemplate, itemToDelete.TenHienThi),
                                             title, MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    // [QUAN TRỌNG] Nếu xóa món quà, trả lại voucher
                    if (itemToDelete.GhiChu != null && (itemToDelete.GhiChu.Contains("(Quà tặng)") || itemToDelete.GhiChu.Contains("(Gift)")))
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