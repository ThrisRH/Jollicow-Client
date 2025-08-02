namespace Jollicow.Models
{
    public class ToppingOptions
    {
        public string id_option { get; set; } = String.Empty;
        public string name { get; set; } = String.Empty;
        public string price { get; set; } = String.Empty;
    }

    public class ToppingModels
    {
        public string id_topping { get; set; } = String.Empty;
        public string id_dishes { get; set; } = String.Empty;
        public string name_details { get; set; } = String.Empty;
        public bool mustChoice { get; set; } = false;
        public List<ToppingOptions> options { get; set; } = new List<ToppingOptions>();

        public ToppingType Type => GetCateFromName(name_details);

        // Property để lấy lựa chọn mặc định (lựa chọn đầu tiên nếu mustChoice = true)
        public ToppingOptions? DefaultOption => mustChoice && options.Count > 0 ? options.FirstOrDefault() : null;

        // Property để kiểm tra xem có phải single choice không
        public bool IsSingleChoice => mustChoice;

        private ToppingType GetCateFromName(string name_details)
        {
            return name_details.ToLowerInvariant() switch
            {
                "kích thước" => ToppingType.Size,
                "độ cay" => ToppingType.Spice,
                _ => ToppingType.Other,
            };
        }
    }

    // Chuẩn hóa data
    public enum ToppingType
    {
        Size,
        Spice,
        Other,
    }
}