# Jollicow - Hệ thống đặt món tại bàn

## Tổng quan

Jollicow là ứng dụng web cho phép khách hàng đặt món trực tiếp tại bàn thông qua QR code, với hệ thống thanh toán tích hợp và theo dõi đơn hàng real-time.

## Cấu trúc dự án

```
Jollicow/
├── Controllers/           # MVC Controllers
│   ├── CartController.cs
│   ├── MenuController.cs
│   └── PaymentController.cs
├── Models/               # Data Models
│   ├── AuthModels.cs
│   ├── CartModel.cs
│   ├── CategoriesModel.cs
│   ├── DishesModels.cs
│   ├── PaymentModels/
│   └── ViewModels/
├── Services/             # Business Logic
│   ├── AppService/
│   ├── FirebaseService.cs
│   └── TokenService.cs
├── Views/                # Razor Views
│   ├── Cart/
│   ├── Menu/
│   ├── Payment/
│   └── Shared/
├── wwwroot/              # Static Files
│   ├── css/
│   ├── js/
│   └── lib/
└── Configuration/        # App Configuration
```

## Sitemap

### Trang chính

- **Menu** (`/Menu`) - Hiển thị danh sách món ăn theo danh mục
- **Cart** (`/Cart/CartDetail`) - Giỏ hàng và thanh toán
- **Payment** (`/Payment/*`) - Các phương thức thanh toán
  - Cash (`/Payment/Cash`) - Thanh toán tiền mặt
  - VietQR (`/Payment/VietQR`) - Thanh toán QR
  - Confirmation (`/Payment/Confirmation`) - Xác nhận đơn hàng
  - TrackBill (`/Payment/TrackBill`) - Theo dõi đơn hàng

### Components

- **PaymentMethod** - Dropdown chọn phương thức thanh toán
- **Promotion** - Hiển thị voucher/khuyến mãi
- **DishCard** - Card hiển thị món ăn

## User Flow

### 1. Khởi tạo phiên

```
QR Code → Decode acsc token → Lấy id_table, restaurant_id → Redirect to Menu
```

### 2. Đặt món

```
Menu → Chọn món → Thêm vào giỏ → Cart → Chọn phương thức thanh toán
```

### 3. Thanh toán

```
Cart → Payment Method → Cash/VietQR → Confirmation → TrackBill
```

### 4. Theo dõi đơn hàng

```
TrackBill → Polling API → Hiển thị trạng thái → Bill details → End Table
```

## Kỹ thuật sử dụng

### Backend (.NET 6)

- **ASP.NET Core MVC** - Framework chính
- **Entity Framework Core** - ORM (nếu có database)
- **Dependency Injection** - IoC container
- **Async/Await** - Xử lý bất đồng bộ
- **HttpClient** - Gọi API bên ngoài
- **JSON Serialization** - Xử lý dữ liệu API

### Frontend

- **Bootstrap 5** - CSS Framework
- **Bootstrap Icons** - Icon library
- **Vanilla JavaScript** - Không dùng framework
- **Fetch API** - Gọi API từ client
- **localStorage** - Lưu trữ dữ liệu tạm
- **Custom Events** - Communication giữa components

### API Integration

- **Cart API** - Quản lý giỏ hàng
- **Payment API** - Xử lý thanh toán
- **Bill API** - Theo dõi đơn hàng
- **Promotion API** - Khuyến mãi

### Real-time Features

- **Polling** - Cập nhật trạng thái đơn hàng
- **Event-driven** - Cập nhật navbar real-time
- **Smart caching** - localStorage + API fallback

### Security

- **Token-based** - acsc parameter encoding
- **CORS** - Cross-origin requests
- **Input validation** - Client + server side

### Performance

- **Lazy loading** - Components load khi cần
- **Debouncing** - Tránh spam API calls
- **Smart polling** - Chỉ cập nhật khi cần thiết
- **Resource optimization** - CSS/JS minification

## Cài đặt và chạy

```bash
# Clone repository
git clone [repository-url]

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run development server
dotnet run
```

## Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Development/Production
- API endpoints trong `appsettings.json`

## Deployment

- **Railway** - Hosting platform
- **Static files** - CSS, JS, Images
- **Database** - External API (không có local DB)
