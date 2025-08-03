# Rate Limiting Middleware - Hướng dẫn Test

## 🚀 Tổng quan

RateLimitingMiddleware đã được tích hợp vào Jollicow để bảo vệ API khỏi spam requests và DDoS attacks.

### Cấu hình hiện tại:

- **10 requests/giây** - Giới hạn burst
- **60 requests/phút** - Giới hạn tổng thể
- **Window size**: 60 giây
- **Client identifier**: IP + Table ID

## 📋 Cách Test

### 1. Test bằng Browser (HTML Tool)

```bash
# Mở file test-rate-limit.html trong browser
# Hoặc truy cập: file:///path/to/test-rate-limit.html
```

**Các test case:**

- **Single Request**: Kiểm tra hoạt động bình thường
- **Rate Limit Test**: Gửi 15 requests liên tiếp
- **Burst Test**: Gửi 20 requests cùng lúc
- **Endpoint Test**: Test các endpoint khác nhau

### 2. Test bằng PowerShell

```powershell
# Chạy script test
.\test-rate-limit.ps1

# Hoặc với parameters
.\test-rate-limit.ps1 -BaseUrl "https://localhost:7001" -RequestCount 20 -DelayMs 30
```

### 3. Test bằng cURL

```bash
# Test single request
curl -X GET "https://localhost:7001/Menu/Menu?id_table=B02&restaurant_id=CHA1001"

# Test rate limiting (bash script)
for i in {1..15}; do
  curl -X GET "https://localhost:7001/Menu/Menu?id_table=B02&restaurant_id=CHA1001"
  sleep 0.05
done
```

### 4. Test bằng JavaScript (Console)

```javascript
// Test burst requests
async function testBurst() {
  const promises = [];
  for (let i = 1; i <= 15; i++) {
    promises.push(fetch("/Menu/Menu?id_table=B02&restaurant_id=CHA1001"));
  }
  const responses = await Promise.all(promises);

  responses.forEach((response, index) => {
    if (response.status === 429) {
      console.log(`Request ${index + 1}: RATE LIMITED`);
    } else {
      console.log(`Request ${index + 1}: SUCCESS`);
    }
  });
}

testBurst();
```

## 🧪 Test Scenarios

### Scenario 1: Normal Usage

```bash
# Gửi 1-5 requests bình thường
# Expected: Tất cả thành công (200 OK)
```

### Scenario 2: Rate Limit Trigger

```bash
# Gửi 15+ requests trong 1 giây
# Expected: Một số requests bị rate limited (429)
```

### Scenario 3: Burst Protection

```bash
# Gửi 20+ requests cùng lúc
# Expected: Nhiều requests bị rate limited
```

### Scenario 4: Different Tables

```bash
# Test với table IDs khác nhau
curl "https://localhost:7001/Menu/Menu?id_table=B01&restaurant_id=CHA1001"
curl "https://localhost:7001/Menu/Menu?id_table=B02&restaurant_id=CHA1001"
# Expected: Rate limit riêng biệt cho từng table
```

### Scenario 5: Recovery Test

```bash
# 1. Trigger rate limit
# 2. Đợi 60 giây
# 3. Gửi request mới
# Expected: Request mới thành công
```

## 📊 Expected Results

### Response khi bị Rate Limited:

```json
{
  "error": "Rate limit exceeded",
  "message": "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
  "retryAfter": 45
}
```

### HTTP Status Codes:

- **200**: Request thành công
- **429**: Rate limit exceeded
- **500**: Server error (khác)

## 🔍 Monitoring

### Log Messages:

```
[INFO] Minute rate limit exceeded for 127.0.0.1_B02: 61/60
[INFO] Second rate limit exceeded for 127.0.0.1_B02: 11/10
[WARN] Rate limit exceeded for client: 127.0.0.1_B02
```

### Performance Metrics:

- Request count per client
- Rate limit triggers
- Response times
- Error rates

## 🛠️ Configuration

### Thay đổi cấu hình trong `RateLimitingMiddleware.cs`:

```csharp
// Rate limit settings
private const int MaxRequestsPerMinute = 60; // Thay đổi số requests/phút
private const int MaxRequestsPerSecond = 10;  // Thay đổi số requests/giây
private const int WindowSizeInSeconds = 60;   // Thay đổi window size
```

### Thêm vào `appsettings.json`:

```json
{
  "RateLimiting": {
    "MaxRequestsPerMinute": 60,
    "MaxRequestsPerSecond": 10,
    "WindowSizeInSeconds": 60
  }
}
```

## 🚨 Troubleshooting

### Vấn đề thường gặp:

1. **Rate limit không hoạt động**

   - Kiểm tra middleware đã được đăng ký trong Program.cs
   - Kiểm tra logs để xem middleware có được gọi không

2. **Rate limit quá nghiêm ngặt**

   - Giảm `MaxRequestsPerSecond` hoặc `MaxRequestsPerMinute`
   - Tăng `WindowSizeInSeconds`

3. **Rate limit không đủ bảo vệ**

   - Tăng `MaxRequestsPerSecond`
   - Thêm IP-based rate limiting

4. **Performance issues**
   - Chuyển từ in-memory sang Redis
   - Tối ưu cleanup logic

## 📈 Advanced Testing

### Load Testing với Apache Bench:

```bash
# Test 100 requests, 10 concurrent
ab -n 100 -c 10 https://localhost:7001/Menu/Menu?id_table=B02&restaurant_id=CHA1001
```

### Stress Testing:

```bash
# Test với nhiều clients
for i in {1..10}; do
  curl "https://localhost:7001/Menu/Menu?id_table=B0$i&restaurant_id=CHA1001" &
done
```

## ✅ Success Criteria

Rate limiting được coi là hoạt động tốt khi:

1. ✅ Single requests thành công (200 OK)
2. ✅ Burst requests bị rate limited (429)
3. ✅ Different tables có rate limit riêng biệt
4. ✅ Rate limit recovery sau window time
5. ✅ Logs hiển thị rate limit events
6. ✅ Performance không bị ảnh hưởng đáng kể

## 🎯 Next Steps

1. **Production Deployment**: Chuyển sang Redis storage
2. **Monitoring**: Thêm metrics collection
3. **Configuration**: Externalize settings
4. **Advanced Features**: IP whitelist, endpoint-specific limits
