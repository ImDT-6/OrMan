using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrMan.Models  // <-- Lưu ý namespace này
{
    public class CustomerFeedback
    {
        public string TableName { get; set; }     // Tên bàn (VD: Bàn 5)
        public DateTime CreatedDate { get; set; } // Thời gian tạo
        public int StarRating { get; set; }       // Số sao (1-5)
        public string Content { get; set; }       // Nội dung nhận xét

        // Thuộc tính phụ để hiển thị sao dạng icon trên giao diện
        // Ví dụ: 3 sao sẽ hiện "★★★☆☆"
        public string StarDisplay => new string('★', StarRating) + new string('☆', 5 - StarRating);
    }
}